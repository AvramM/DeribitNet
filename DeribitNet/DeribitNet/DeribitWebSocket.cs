using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeribitNet.Model;
using DeribitNet.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using XCoinr.Trading.UI.Deribit.Converter;

namespace DeribitNet
{

    public class DeribitWebSocket
    {
        private readonly DeribitConfiguration _config;
        private ClientWebSocket _websocket;
        private int _requestId;
        private readonly ConcurrentDictionary<int, TaskInfo> _tasks;
        private readonly object _messageSendSyncObj;
        private readonly Queue<MessageInfo> _messagesToSend;
        private bool _isRunningSendingMessageQueue;
        private readonly UTF8Encoding _encoding;
        private readonly object _eventsMapLock;
        private readonly Dictionary<string, SubscribtionEntry> _eventsMap;
        public event EventHandler DisconnectEvent;

        public DeribitWebSocket(DeribitConfiguration config)
        {
            _config = config;
            _requestId = 0;
            _tasks = new ConcurrentDictionary<int, TaskInfo>();
            _messageSendSyncObj = new object();
            _messagesToSend = new Queue<MessageInfo>();
            _encoding = new UTF8Encoding();
            _eventsMapLock = new object();
            _eventsMap = new Dictionary<string, SubscribtionEntry>();
        }

        public async Task Connect()
        {
            if (_websocket != null)
            {
                return;
            }

            _websocket = new ClientWebSocket();
            Console.WriteLine("Connecting to v2 websocket");
            var url = _config.GetDeribitV2WebSocketApiEndpoint();
            _websocket.ConnectAsync(new Uri("wss://www.deribit.com/ws/api/v2/"), CancellationToken.None).Wait();
            Console.WriteLine("Connected to v2 websocket");
            Task.Run(receiveMessageQueue);
            Task.Run(reconnectLoop);
            Task.Run(pingLoop);
            foreach (var entry in _eventsMap)
            {
                entry.Value.State = SubscribtionState.Unsubscribed;
                Task.Run(() => ManagedSubscribe(entry.Key, null));
            }
        }

        private async Task reconnectLoop()
        {
            while (true)
            {
                await Task.Delay(10 * 1000);
                if (_websocket == null)
                {
                    return;
                }
                Console.WriteLine($"WebSocket status {_websocket.State}");
                if (_websocket.State != WebSocketState.Open)
                {
                    Disconnect();
                    return;
                }
            }
        }

        public void Disconnect()
        {
            Console.WriteLine($"WebSocket Disconnect");
            if (_websocket != null)
            {
                _websocket.Dispose();
                _websocket = null;
            }
            DisconnectEvent?.Invoke(this, null);
        }

        public Task<T> Send<T>(string method, object @params, Converter.JsonConverter<T> converter)
        {
            Console.WriteLine("Calling " + method);
            var request = new JsonRpcRequest()
            {
                jsonrpc = "2.0",
                id = _requestId++,
                method = method,
                @params = @params
            };
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            var taskInfo = new TypedTaskInfo<T>()
            {
                tcs = tcs,
                id = request.id,
                converter = converter
            };
            _tasks[request.id] = taskInfo;
            var message = JsonConvert.SerializeObject(request);
            byte[] buffer = _encoding.GetBytes(message);
            Console.WriteLine($"Send task {method} {request.id} {message}");
            pushMessage(taskInfo, buffer);
            return tcs.Task;
        }

        private void pushMessage(TaskInfo task, byte[] message)
        {
            lock (_messageSendSyncObj)
            {
                _messagesToSend.Enqueue(new MessageInfo() { task = task, message = message });
                if (!_isRunningSendingMessageQueue)
                {
                    _isRunningSendingMessageQueue = true;
                    Task.Run(sendingMessageQueue);
                }
            }
        }

        public Task<string> Ping()
        {
            return Send("public/ping", new { }, new ObjectJsonConverter<string>());
        }

        private async Task pingLoop()
        {
            while (true)
            {
                await Task.Delay(10 * 1000);
                if (_websocket.State != WebSocketState.Open)
                {
                    return;
                }
                var res = await Ping();
                Console.WriteLine($"Ping - {res}");
            }
        }

        public Task<List<string>> Subscribe(string[] channels)
        {
            return Send("public/subscribe", new { channels }, new ListJsonConverter<string>());
        }

        public Task<List<string>> Unsubscribe(string[] channels)
        {
            return Send("public/unsubscribe", new { channels }, new ListJsonConverter<string>());
        }

        public void AddEventListener(string channel, Action<EventResponse> callback)
        {
            lock (_eventsMapLock)
            {
                if (!_eventsMap.ContainsKey(channel))
                {
                    _eventsMap[channel] = new SubscribtionEntry()
                    {
                        State = SubscribtionState.Unsubscribed,
                        Callbacks = new List<Action<EventResponse>>()
                    };
                }
                _eventsMap[channel].Callbacks.Add(callback);
            }
        }

        public void RemoveEventListener(string channel, Action<EventResponse> callback)
        {
            lock (_eventsMapLock)
            {
                if (_eventsMap.ContainsKey(channel))
                {
                    _eventsMap[channel].Callbacks.Remove(callback);
                }
            }
        }

        public async Task<bool> ManagedSubscribe(string channel, Action<EventResponse> callback)
        {
            SubscribtionEntry entry;
            TaskCompletionSource<bool> defer = null;
            lock (_eventsMapLock)
            {
                if (_eventsMap.ContainsKey(channel))
                {
                    entry = _eventsMap[channel];
                    if (entry.State == SubscribtionState.Subscribed)
                    {
                        Console.WriteLine($"Already subsribed added to callbacks {channel}");
                        if (callback != null)
                        {
                            entry.Callbacks.Add(callback);
                        }
                        return true;
                    }
                    if (entry.State == SubscribtionState.Unsubscribing)
                    {
                        Console.WriteLine($"Unsubscribing return false {channel}");
                        return false;
                    }
                    if (entry.State == SubscribtionState.Unsubscribed)
                    {
                        Console.WriteLine($"Unsubscribed resubscribing {channel}");
                        entry.State = SubscribtionState.Subscribing;
                        defer = new TaskCompletionSource<bool>();
                        entry.CurrentAction = defer.Task;
                    }
                }
                else
                {
                    Console.WriteLine($"Not exists subscribing {channel}");
                    defer = new TaskCompletionSource<bool>();
                    entry = new SubscribtionEntry()
                    {
                        State = SubscribtionState.Subscribing,
                        Callbacks = new List<Action<EventResponse>>(),
                        CurrentAction = defer.Task
                    };
                    _eventsMap[channel] = entry;
                }
            }
            if (defer == null)
            {
                Console.WriteLine($"Empty defer wait for already subscribing {channel}");
                var currentAction = entry.CurrentAction;
                var result = currentAction != null ? await currentAction : false;
                Console.WriteLine($"Empty defer wait for already subscribing res {result} {channel}");
                lock (_eventsMapLock)
                {
                    if (result && entry.State == SubscribtionState.Subscribed)
                    {
                        Console.WriteLine($"Empty defer adding callback {channel}");
                        if (callback != null)
                        {
                            entry.Callbacks.Add(callback);
                        }
                        return true;
                    }
                    return false;
                }
            }
            try
            {
                Console.WriteLine($"Subscribing {channel}");
                var response = await Subscribe(new string[] { channel });
                if (response.Count != 1 || response[0] != channel)
                {
                    Console.WriteLine($"Invalid subscribe result {response} {channel}");
                    defer.SetResult(false);
                }
                else
                {
                    lock (_eventsMapLock)
                    {
                        Console.WriteLine($"Successfully subscribed adding callback {channel}");
                        entry.State = SubscribtionState.Subscribed;
                        if (callback != null)
                        {
                            entry.Callbacks.Add(callback);
                        }
                        entry.CurrentAction = null;
                    }
                    defer.SetResult(true);
                }
            }
            catch (Exception e)
            {
                defer.SetException(e);
            }
            return await defer.Task;
        }

        public async Task<bool> ManagedUnsubscribe(string channel, Action<EventResponse> callback)
        {
            SubscribtionEntry entry;
            TaskCompletionSource<bool> defer = null;
            lock (_eventsMapLock)
            {
                if (!_eventsMap.ContainsKey(channel))
                {
                    return false;
                }
                entry = _eventsMap[channel];
                if (!entry.Callbacks.Contains(callback))
                {
                    return false;
                }
                if (entry.State == SubscribtionState.Subscribing)
                {
                    return false;
                }
                if (entry.State == SubscribtionState.Unsubscribed || entry.State == SubscribtionState.Unsubscribing)
                {
                    entry.Callbacks.Remove(callback);
                    return true;
                }
                if (entry.State == SubscribtionState.Subscribed)
                {
                    if (entry.Callbacks.Count > 1)
                    {
                        entry.Callbacks.Remove(callback);
                        return true;
                    }
                    entry.State = SubscribtionState.Unsubscribing;
                    defer = new TaskCompletionSource<bool>();
                    entry.CurrentAction = defer.Task;
                }
            }
            try
            {
                var response = await Unsubscribe(new string[] { channel });
                if (response.Count != 1 || response[0] != channel)
                {
                    defer.SetResult(false);
                }
                else
                {
                    lock (_eventsMapLock)
                    {
                        entry.State = SubscribtionState.Unsubscribed;
                        entry.Callbacks.Remove(callback);
                        entry.CurrentAction = null;
                    }
                    defer.SetResult(true);
                }
            }
            catch (Exception e)
            {
                defer.SetException(e);
            }
            return await defer.Task;
        }

        private async Task sendingMessageQueue()
        {
            while (true)
            {
                MessageInfo messageInfo;
                lock (_messageSendSyncObj)
                {
                    if (_messagesToSend.Count == 0)
                    {
                        _isRunningSendingMessageQueue = false;
                        return;
                    }
                    messageInfo = _messagesToSend.Dequeue();
                }
                try
                {
                    await _websocket.SendAsync(new ArraySegment<byte>(messageInfo.message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception e)
                {
                    messageInfo.task.Reject(e);
                    _tasks.TryRemove(messageInfo.task.id, out TaskInfo task);
                }
            }
        }

        private async Task receiveMessageQueue()
        {
            byte[] buffer = new byte[10240];
            var res = "";
            while (_websocket.State == WebSocketState.Open)
            {
                var result = await _websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    Disconnect();
                    return;
                }
                else
                {
                    res += _encoding.GetString(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        TaskInfo task = null;
                        try
                        {
                            Console.WriteLine($"ReceiveMessageQueue message {res}");
                            var jObject = (JObject)JsonConvert.DeserializeObject(res, ConverterSettings.Settings);
                            if (jObject.ContainsKey("params"))
                            {
                                var eventRes = jObject.ToObject<EventResponse>();
                                //_logger.Debug($"ReceiveMessageQueue event {eventRes.@event}");
                                SubscribtionEntry entry;
                                if (_eventsMap.TryGetValue(eventRes.@params.channel, out entry))
                                {
                                    foreach (var callback in entry.Callbacks)
                                    {
                                        try
                                        {
                                            callback(eventRes);
                                        }
                                        catch (Exception e)
                                        {
                                            //_logger.Debug($"ReceiveMessageQueue Error during calling event callback {eventRes.@params.channel} {e}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var parsedResult = jObject.ToObject<JsonRpcResponse>();
                                Console.WriteLine($"ReceiveMessageQueue task {parsedResult.id}");
                                if (_tasks.TryRemove(parsedResult.id, out task))
                                {
                                    if (parsedResult.error != null)
                                    {
                                        if (parsedResult.error.Type == JTokenType.Object)
                                        {
                                            var error = parsedResult.error.ToObject<JsonRpcError>();
                                            task.Reject(new Exception($"Invalid response for {parsedResult.id}, code: {error.code}, message: {error.message}"));
                                        }
                                        else
                                        {
                                            task.Reject(new Exception($"Invalid response for {parsedResult.id}, code: {parsedResult.error}"));
                                        }
                                    }
                                    else
                                    {
                                        var convertedResult = task.Convert(parsedResult.result);
                                        Console.WriteLine($"ReceiveMessageQueue task resolve {parsedResult.id}");
                                        task.Resolve(convertedResult);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"ReceiveMessageQueue cannot resolve task {parsedResult.id}");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"ReceiveMessageQueue Error during parsing task {e}");
                            if (task != null)
                            {
                                task.Reject(e);
                            }
                        }
                        finally
                        {
                            res = "";
                        }
                    }
                }
            }
        }
    }

    internal static class ConverterSettings
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
