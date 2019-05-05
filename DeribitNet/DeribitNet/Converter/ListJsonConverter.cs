using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DeribitNet.Converter
{
    public class ListJsonConverter<T> : JsonConverter<List<T>>
    {
        public List<T> Convert(JToken obj)
        {
            var list = (JArray)obj;
            var result = new List<T>(list.Count);
            foreach (var v in list)
            {
                result.Add(v.ToObject<T>());
            }
            return result;
        }
    }
}
