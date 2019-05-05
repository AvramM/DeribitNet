using Newtonsoft.Json.Linq;

namespace DeribitNet.Model
{
    public class EventParams
    {
        public string channel;
        public JToken data;
    }
}
