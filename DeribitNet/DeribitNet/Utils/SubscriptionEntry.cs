using DeribitNet.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeribitNet.Utils
{
    public class SubscriptionEntry
    {
        public SubscriptionState State { get; set; }
        public List<Action<EventResponse>> Callbacks { get; set; }
        public Task<bool> CurrentAction { get; set; }
    }
}
