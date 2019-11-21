namespace DeribitNet
{
    public class DeribitConfiguration
    {
        public string GetDeribitDomain()
        {
            return "www.deribit.com";
        }

        public string GetDeribitV2WebSocketApiEndpoint()
        {
            return "wss://" + GetDeribitDomain() + "/ws/api/v1/";
        }

        public string GetDeribitAjaxRestApiEndpoint()
        {
            return "https://" + GetDeribitDomain() + "/ajax?";
        }

        public string GetDeribitV1RestApiEndpoint()
        {
            return "https://" + GetDeribitDomain() + "/api/v1/";
        }
    }
}
