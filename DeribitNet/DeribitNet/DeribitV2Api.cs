using DeribitNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XCoinr.Trading.UI.Deribit.Converter;

namespace DeribitNet
{
    public class DeribitV2Api
    {
        private DeribitWebSocket _deribitWebSocket;
        private readonly object _lockObject;
        private List<Tuple<string, object, object>> _listeners;

        public DeribitV2Api(DeribitWebSocket deribitWebSocket)
        {
            _deribitWebSocket = deribitWebSocket;
            _lockObject = new object();
            _listeners = new List<Tuple<string, object, object>>();
            _deribitWebSocket.Connect().Wait();
        }

        #region Helpers

        private async Task<bool> subscribe<T>(string channelName, Action<T> originalCallback, Action<EventResponse> myCallback)
        {
            var result = await _deribitWebSocket.ManagedSubscribe(channelName, myCallback);
            if (result)
            {
                lock (_lockObject)
                {
                    _listeners.Add(Tuple.Create(channelName, (object)originalCallback, (object)myCallback));
                }
            }
            return result;
        }

        private async Task<bool> unsubscribe<T>(string channelName, Action<T> originalCallback)
        {
            Tuple<string, object, object> entry;
            lock (_lockObject)
            {
                entry = _listeners.FirstOrDefault(x => x.Item1 == channelName && x.Item2 == (object)originalCallback);
            }
            if (entry == null)
            {
                return false;
            }
            if (await _deribitWebSocket.ManagedUnsubscribe(channelName, (Action<EventResponse>)entry.Item3))
            {
                lock (_lockObject)
                {
                    _listeners.Remove(entry);
                }
                return true;
            }
            return false;
        }

        #endregion



        public Task<List<InstrumentInfo>> GetOptions()
        {
            return _deribitWebSocket.Send("public/get_instruments", new { currency = "BTC", kind = "option" }, new ListJsonConverter<InstrumentInfo>());
        }

        public Task<bool> SubscribeRawBook(string instrumentName, Action<RawBookResponse> callback)
        {
            return subscribe("book." + instrumentName + ".raw", callback, response =>
            {
                var rawBookResponse = response.@params.data.ToObject<RawBookResponse>();
                if (string.IsNullOrEmpty(rawBookResponse.instrument_name))
                {
                    rawBookResponse.instrument_name = response.@params.channel.Split('.')[1];
                }
                callback(rawBookResponse);
            });
        }

        public Task<bool> UnsubscribeRawBook(string instrumentName, Action<RawBookResponse> callback)
        {
            return unsubscribe("book." + instrumentName + ".raw", callback);
        }

        public Task<bool> SubscribeQuote(string instrumentName, Action<QuoteResponse> callback)
        {
            return subscribe("quote." + instrumentName, callback, response =>
            {
                callback(response.@params.data.ToObject<QuoteResponse>());
            });
        }

        public Task<bool> UnsubscribeQuote(string instrumentName, Action<QuoteResponse> callback)
        {
            return unsubscribe("quote." + instrumentName, callback);
        }

        public Task<TickerResponse> Ticker(string instrumentName)
        {
            return _deribitWebSocket.Send("public/ticker", new { instrument = instrumentName }, new ObjectJsonConverter<TickerResponse>());
        }

        public Task<bool> SubscribeTicker(string instrumentName, Action<TickerResponse> callback)
        {
            return subscribe("ticker." + instrumentName + ".100ms", callback, response =>
            {
                callback(response.@params.data.ToObject<TickerResponse>());
            });
        }

        public Task<bool> UnsubscribeTicker(string instrumentName, Action<TickerResponse> callback)
        {
            return unsubscribe("ticker." + instrumentName + ".100ms", callback);
        }

        public Task<OrderBookResponse> GetOrderBook(string instrumentName, int depth = 20)
        {
            return _deribitWebSocket.Send("public/get_order_book", new { instrument_name = instrumentName, depth }, new ObjectJsonConverter<OrderBookResponse>());
        }

        public Task<IndexResponse> GetIndex()
        {
            return _deribitWebSocket.Send("public/get_index", new { currency = "BTC" }, new ObjectJsonConverter<IndexResponse>());
        }

        public Task<IndexResponse> GetSummary()
        {
            return _deribitWebSocket.Send("/api/v1/public/index", new { }, new ObjectJsonConverter<IndexResponse>());
        }

        public Task<bool> SubscribeBook(string instrument, int group, int depth, Action<BookResponse> callback)
        {
            return subscribe("book." + instrument + "." + (group == 0 ? "none" : group.ToString()) + "." + depth + ".100ms", callback, response =>
            {
                callback(response.@params.data.ToObject<BookResponse>());
            });
        }

        public Task<bool> UnsubscribeBook(string instrument, int group, int depth, Action<BookResponse> callback)
        {
            return unsubscribe("book." + instrument + "." + (group == 0 ? "none" : group.ToString()) + "." + depth + ".100ms", callback);
        }

        public Task<bool> SubscribeTrades(string instrument, string interval, Action<List<TradesResponse>> callback)
        {
            return subscribe("trades." + instrument + "." + interval, callback, response =>
            {
                var converter = new ListJsonConverter<TradesResponse>();
                callback(converter.Convert(response.@params.data));
            });
        }

        public Task<bool> UnsubscribeTrades(string instrument, string interval, Action<List<TradesResponse>> callback)
        {
            return unsubscribe("trades." + instrument + "." + interval, callback);
        }

        public Task<bool> SubscribeChartFtu(string instrument, string resolution, Action<ChartFtuResponse> callback)
        {
            return subscribe("chart.ftu." + instrument + "." + resolution, callback, response =>
            {
                callback(response.@params.data.ToObject<ChartFtuResponse>());
            });
        }

        public Task<bool> UnsubscribeChartFtu(string instrument, string resolution, Action<ChartFtuResponse> callback)
        {
            return unsubscribe("chart.ftu." + instrument + "." + resolution, callback);
        }

        public Task<List<LastTrade>> GetLastTrades(string instrument, int count)
        {
            return _deribitWebSocket.Send("public/getlasttrades", new { instrument, count }, new ListJsonConverter<LastTrade>());
        }

        public Task<List<LastTrade>> GetLastTrades(string instrument, long startMiliTimestamp, long endMiliTimestamp, int count, string sort)
        {
            return _deribitWebSocket.Send("public/getlasttrades", new { instrument, startTimestamp = startMiliTimestamp, endTimestamp = endMiliTimestamp, count, sort }, new ListJsonConverter<LastTrade>());
        }

        public Task<List<LastTrade>> GetLastTradesByEndTimestamp(string instrument, long endMiliTimestamp, int count, string sort)
        {
            return _deribitWebSocket.Send("public/getlasttrades", new { instrument, endTimestamp = endMiliTimestamp, count, sort }, new ListJsonConverter<LastTrade>());
        }

        public Task<List<LastTrade>> GetLastTradesByEndId(string instrument, long endId, int count, string sort)
        {
            return _deribitWebSocket.Send("public/getlasttrades", new { instrument, endId, count, sort }, new ListJsonConverter<LastTrade>());
        }
    }
}
