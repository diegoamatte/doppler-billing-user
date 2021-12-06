using System.Net.Http;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Model;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public class AccountPlansService : IAccountPlansService
    {
        private readonly IOptions<AccountPlansSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly IUsersApiTokenGetter _usersApiTokenGetter;

        public AccountPlansService(
            IOptions<AccountPlansSettings> options,
            ILogger<AccountPlansService> logger,
            IFlurlClientFactory flurlClientFac,
            IUsersApiTokenGetter usersApiTokenGetter)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.CalculateUrlTemplate);
            _usersApiTokenGetter = usersApiTokenGetter;
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
            var token = await _usersApiTokenGetter.GetTokenAsync();

            var response = await _flurlClient.Request(new UriTemplate(_options.Value.CalculateUrlTemplate)
                    .AddParameter("accountname", accountname)
                    .AddParameter("planId", agreement.PlanId)
                    .AddParameter("discountId", agreement.DiscountId)
                    .AddParameter("promocode", agreement.Promocode)
                    .Resolve())
                .WithHeader("Authorization", $"Bearer {token}")
                .GetAsync();

            return response.ResponseMessage;
        }
    }
}
