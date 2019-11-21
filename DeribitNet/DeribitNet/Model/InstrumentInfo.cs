using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DeribitNet.Model
{
    public partial class InstrumentInfo
    {
        [JsonProperty("tick_size")]
        public double TickSize { get; set; }

        [JsonProperty("strike")]
        public long Strike { get; set; }

        [JsonProperty("settlement_period")]
        public string SettlementPeriod { get; set; }

        [JsonProperty("quote_currency")]
        public string QuoteCurrency { get; set; }

        [JsonProperty("option_type")]
        public string OptionType { get; set; }

        [JsonProperty("min_trade_amount")]
        public double MinTradeAmount { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("instrument_name")]
        public string InstrumentName { get; set; }

        [JsonProperty("expiration_timestamp")]
        [JsonConverter(typeof(DeribitTimestampConverter))]
        public DateTime Expiration { get; set; }

        [JsonProperty("creation_timestamp")]
        public long CreationTimestamp { get; set; }

        [JsonProperty("contract_size")]
        public long ContractSize { get; set; }

        [JsonProperty("base_currency")]
        public string BaseCurrency { get; set; }
    }

    public partial class InstrumentInfo
    {
        public static InstrumentInfo FromJson(string json) => JsonConvert.DeserializeObject<InstrumentInfo>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this InstrumentInfo self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    public class DeribitTimestampConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var t = long.Parse(reader.Value.ToString());
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(t);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
