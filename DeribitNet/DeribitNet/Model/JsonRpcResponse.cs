using Newtonsoft.Json.Linq;

namespace DeribitNet.Model
{
    public class JsonRpcResponse
    {
        public int id;
        public JToken result;
        public JToken error; 
        public bool testnet;
        public long usIn;
        public long usOut;
        public long usDiff;
    }
}
