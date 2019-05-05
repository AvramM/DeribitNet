using System.Collections.Generic;

namespace DeribitNet.Model
{
    public class BookResponse
    {
        public string instrument;
        public long change_id;
        public List<double[]> bids;
        public List<double[]> asks;
    }
}
