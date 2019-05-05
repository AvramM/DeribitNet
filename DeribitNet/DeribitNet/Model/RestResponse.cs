using Newtonsoft.Json.Linq;

namespace DeribitNet.Model
{
    public class RestResponse
    {
        public long usOut;
        public long usIn;
        public long usDiff;
        public bool testnet;
        public bool success;
        public JToken result;
        public string message;
        public int error;
    }
}
