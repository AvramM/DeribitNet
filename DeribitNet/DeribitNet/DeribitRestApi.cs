using System;
using System.Net.Http;
using System.Threading.Tasks;
using DeribitNet.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeribitNet
{
    public class DeribitRestApi
    {
        private readonly HttpClient _client;
        private readonly DeribitConfiguration _config;

        public DeribitRestApi(DeribitConfiguration config)
        {
            _config = config;
            _client = new HttpClient();
        }

        public Task<string> AjaxGetRaw(string query)
        {
            return _client.GetStringAsync(_config.GetDeribitAjaxRestApiEndpoint() + query);
        }

        public async Task<T> AjaxGet<T>(string query, Func<JToken, T> converter)
        {
            var result = await AjaxGetRaw(query);
            var response = (JToken)JsonConvert.DeserializeObject(result);
            return converter(response);
        }

        public Task<string> ApiV1GetRaw(string query)
        {
            return _client.GetStringAsync(_config.GetDeribitV1RestApiEndpoint() + query);
        }

        public async Task<T> ApiV1Get<T>(string query, Func<JToken, T> converter)
        {
            var result = await ApiV1GetRaw(query);
            var response = JsonConvert.DeserializeObject<RestResponse>(result);
            if (!response.success)
            {
                throw new Exception($"Invalid response error: {response.error}, message: {response.message}");
            }
            return converter(response.result);
        }

        public Task<FtuTwcResponse> FtuTwc(string instrument, string resolution, long fromSeconds, long toSeconds)
        {
            return AjaxGet($"q=ftu_twc&symbol={instrument}&resolution={resolution}&from={fromSeconds}&to={toSeconds}", token =>
            {
                var result = token.ToObject<FtuTwcResponse>();
                if (result.s != "ok")
                {
                    throw new Exception("Error during getting chart candies");
                }
                return result;
            });
        }
    }
}
