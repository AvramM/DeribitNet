using Newtonsoft.Json.Linq;

namespace DeribitNet.Converter
{
    public class ObjectJsonConverter<T> : JsonConverter<T>
    {
        public T Convert(JToken obj)
        {
            return obj.ToObject<T>();
        }
    }
}
