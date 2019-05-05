﻿namespace DeribitNet.Model
{
    public class TradesResponse
    {
        public long tradeId;
        public string instrument;
        public long timeStamp;
        public long tradeSeq;
        public double quantity;
        public double amount;
        public double price;
        public string direction;
        public int tickDirection;
        public double indexPrice;
    }
}
