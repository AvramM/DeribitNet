using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DeribitNet.Utils
{
    public class TypedTaskInfo<T> : TaskInfo
    {
        public Converter.JsonConverter<T> converter;
        public TaskCompletionSource<T> tcs;

        public override object Convert(JToken obj)
        {
            return converter.Convert(obj);
        }

        public override void Resolve(object value)
        {
            tcs.SetResult((T)value);
        }

        public override void Reject(Exception e)
        {
            tcs.SetException(e);
        }
    }
}
