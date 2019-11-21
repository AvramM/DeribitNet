using System;
using Newtonsoft.Json.Linq;

namespace DeribitNet.Utils
{
    public abstract class TaskInfo
    {
        public int id;
        public abstract object Convert(JToken value);
        public abstract void Resolve(object value);
        public abstract void Reject(Exception e);
    }
}
