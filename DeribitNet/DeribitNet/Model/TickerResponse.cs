namespace DeribitNet.Model
{
    public class TickerResponse
    {
        public long time;
        public TickerStats stats;
        public string state;
        public double settlement_price;
        public double open_interest;
        public double min_price;
        public double max_price;
        public double mark_price;
        public double last_price;
        public string instrument_name;
        public double best_bid_qty;
        public double best_bid_price;
        public double best_bid_amount;
        public double best_ask_qty;
        public double best_ask_price;
        public double best_ask_amount;
    }
}
