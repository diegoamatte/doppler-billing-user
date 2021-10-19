using Doppler.BillingUser.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapService : ISapService
    {
        IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IOptions<SapSettings> _options;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SapService(IJwtTokenGenerator jwtTokenGenerator,
            IOptions<SapSettings> options,
            ILogger<SapService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _options = options;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null)
        {
            try
            {
                var response = await SendToSap(
                    new StringContent(JsonConvert.SerializeObject(sapBusinessPartner), Encoding.UTF8, "application/json"),
                    Convert.ToString(_options.Value.SapCreateBusinessPartnerEndpoint));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(string.Format("User data succesfully sent to DopplerSap. Iduser: {0} - ClientManager: {1}", sapBusinessPartner.Id, sapBusinessPartner.IsClientManager.ToString()));
                }
                else
                {
                    _logger.LogError(string.Format("Sending user data to DopplerSap failed. - status : {0} - message: {1} - Iduser: {2} - ClientManager: {3}", response.StatusCode, response.ReasonPhrase, sapBusinessPartner.Id, sapBusinessPartner.IsClientManager.ToString()));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(string.Format("Unexpected error sending data to DopplerSap, Exception:{0}", e.ToString()));
            }
        }

        private async Task<HttpResponseMessage> SendToSap(StringContent data, string endpoint)
        {
            string accessToken = _jwtTokenGenerator.GenerateSuperUserJwtToken();

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var uri = new Uri(Convert.ToString(_options.Value.SapBaseUrl) + endpoint);

            var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("POST"),
                Content = data
            };

            return await client.SendAsync(httpRequest).ConfigureAwait(false);
        }
    }
}
