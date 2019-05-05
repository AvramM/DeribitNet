using DeribitNet.Converter;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace DeribitNet.Utils
{
    public class TypedTaskInfo<T> : TaskInfo
    {
        public JsonConverter<T> converter;
        public TaskCompletionSource<T> tcs;

        public override object Convert(JToken obj)
        {
            return this.converter.Convert(obj);
        }

        public override void Resolve(object value)
        {
            this.tcs.SetResult((T)value);
        }

        public override void Reject(Exception e)
        {
            this.tcs.SetException(e);
        }
    }
}
