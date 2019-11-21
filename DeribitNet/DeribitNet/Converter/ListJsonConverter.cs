using System.Collections.Generic;
using DeribitNet.Converter;
using Newtonsoft.Json.Linq;

namespace XCoinr.Trading.UI.Deribit.Converter
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
