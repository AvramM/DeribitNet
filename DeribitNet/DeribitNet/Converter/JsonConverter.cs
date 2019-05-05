using Newtonsoft.Json.Linq;

namespace DeribitNet.Converter
{
    public interface JsonConverter<T>
    {
        T Convert(JToken obj);
    }
}
