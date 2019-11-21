using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeribitNet.Model
{
    public partial class OrderBookResponse
    {
        [JsonProperty("underlying_price")]
        public double UnderlyingPrice { get; set; }

        [JsonProperty("underlying_index")]
        public string UnderlyingIndex { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("stats")]
        public Stats Stats { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("settlement_price")]
        public double SettlementPrice { get; set; }

        [JsonProperty("open_interest")]
        public long OpenInterest { get; set; }

        [JsonProperty("min_price")]
        public double MinPrice { get; set; }

        [JsonProperty("max_price")]
        public double MaxPrice { get; set; }

        [JsonProperty("mark_price")]
        public double? MarkPrice { get; set; }

        [JsonProperty("mark_iv")]
        public double MarkIv { get; set; }

        [JsonProperty("last_price")]
        public object LastPrice { get; set; }

        [JsonProperty("interest_rate")]
        public long InterestRate { get; set; }

        [JsonProperty("instrument_name")]
        public string InstrumentName { get; set; }

        [JsonProperty("index_price")]
        public double IndexPrice { get; set; }

        [JsonProperty("greeks")]
        public Greeks Greeks { get; set; }

        [JsonProperty("change_id")]
        public long ChangeId { get; set; }

        [JsonProperty("bids")]
        public object[] Bids { get; set; }

        [JsonProperty("bid_iv")]
        public long BidIv { get; set; }

        [JsonProperty("best_bid_price")]
        public double? BestBidPrice { get; set; }

        [JsonProperty("best_bid_amount")]
        public long BestBidAmount { get; set; }

        [JsonProperty("best_ask_price")]
        public double? BestAskPrice { get; set; }

        [JsonProperty("best_ask_amount")]
        public long BestAskAmount { get; set; }

        [JsonProperty("asks")]
        public double[][] Asks { get; set; }

        [JsonProperty("ask_iv")]
        public double AskIv { get; set; }
    }

    public partial class Greeks
    {
        [JsonProperty("vega")]
        public double? Vega { get; set; }

        [JsonProperty("theta")]
        public double? Theta { get; set; }

        [JsonProperty("rho")]
        public double? Rho { get; set; }

        [JsonProperty("gamma")]
        public double? Gamma { get; set; }

        [JsonProperty("delta")]
        public double? Delta { get; set; }
    }

    public partial class Stats
    {
        [JsonProperty("volume")]
        public int? Volume { get; set; }

        [JsonProperty("low")]
        public double? Low { get; set; }

        [JsonProperty("high")]
        public double? High { get; set; }
    }
}
