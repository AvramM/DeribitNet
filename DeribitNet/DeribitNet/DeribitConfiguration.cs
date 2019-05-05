using System;
using System.Collections.Generic;
using System.Text;

namespace DeribitNet
{
    public class DeribitConfiguration
    {
        public string DeribitDomain => "deribit.com";
        public string DeribitV2WebSocketApiEndpoint => "wss://" + DeribitDomain + "/ws/api/v2/";
        public string DeribitAjaxRestApiEndpoint => "https://" + DeribitDomain + "/ajax?";
        public string DeribitV2RestApiEndpoint => "https://" + DeribitDomain + "/api/v2/";
    }
}
