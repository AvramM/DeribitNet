using DeribitNet.Converter;
using Newtonsoft.Json.Linq;

namespace XCoinr.Trading.UI.Deribit.Converter
{
    public class ObjectJsonConverter<T> : JsonConverter<T>
    {
        public T Convert(JToken obj)
        {
            return obj.ToObject<T>();
        }
    }
}
