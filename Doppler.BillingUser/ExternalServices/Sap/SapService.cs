using Doppler.BillingUser.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapService : ISapService
    {
        IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IOptions<SapSettings> _options;
        private readonly ILogger _logger;

        public SapService(IJwtTokenGenerator jwtTokenGenerator,
            IOptions<SapSettings> options,
            ILogger<SapService> logger)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _options = options;
            _logger = logger;
        }

        public void SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null)
        {
            try
            {
                var response = SendToSap(
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

        private HttpResponseMessage SendToSap(StringContent data, string endpoint)
        {
            using var httpClient = new HttpClient();
            string accessToken = _jwtTokenGenerator.GenerateSuperUserJwtToken();

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var url = Convert.ToString(_options.Value.SapBaseUrl) + endpoint;
            return httpClient.PostAsync(url, data).Result;
        }
    }
}
