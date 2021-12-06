using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public class AccountPlansService : IAccountPlansService
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IOptions<AccountPlansSettings> _options;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountPlansService(
            IJwtTokenGenerator jwtTokenGenerator,
            IOptions<AccountPlansSettings> options,
            ILogger<AccountPlansService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _options = options;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation)
        {
            var response = await SendRequest(accountname, agreementInformation);

            if (response.IsSuccessStatusCode)
            {
                var planAmountDetails = JsonConvert.DeserializeObject<PlanAmountDetails>(await response.Content.ReadAsStringAsync());

                return planAmountDetails.Total == agreementInformation.Total;
            }

            _logger.LogError($"Error to get total amount {response.Content.ReadAsStringAsync()}.");
            return false;
        }

        private async Task<HttpResponseMessage> SendRequest(string accountname, AgreementInformation agreement)
        {
            var url = $"{_options.Value.BaseUrl}/{accountname}/newplan/{agreement.PlanId}/calculate";
            var param = new Dictionary<string, string> {
            {
                "discountId", agreement.DiscountId.ToString()
            },
            {
                "promocode", agreement.Promocode
            } };
            var uri = new Uri(QueryHelpers.AddQueryString(url, param));
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("GET")
            };

            _logger.LogInformation($"Sending request with url: {uri}");
            var accessToken = _jwtTokenGenerator.GenerateSuperUserJwtToken();
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await client.SendAsync(httpRequest);
        }
    }
}
