using System;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Model;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public class AccountPlansService : IAccountPlansService
    {
        private readonly IOptions<AccountPlansSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly ICurrentRequestApiTokenGetter _usersApiTokenGetter;

        public AccountPlansService(
            IOptions<AccountPlansSettings> options,
            ILogger<AccountPlansService> logger,
            IFlurlClientFactory flurlClientFac,
            ICurrentRequestApiTokenGetter usersApiTokenGetter)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.CalculateUrlTemplate);
            _usersApiTokenGetter = usersApiTokenGetter;
        }

        public async Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation)
        {
            try
            {
                var planAmountDetails = await _flurlClient.Request(new UriTemplate(_options.Value.CalculateUrlTemplate)
                        .AddParameter("accountname", accountname)
                        .AddParameter("planId", agreementInformation.PlanId)
                        .AddParameter("discountId", agreementInformation.DiscountId)
                        .AddParameter("promocode", agreementInformation.Promocode)
                        .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return planAmountDetails.Total == agreementInformation.Total;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get total amount for user {accountname}.");
                throw;
            }
        }

        public async Task<Promotion> GetValidPromotionByCode(string promocode, int planId)
        {
            try
            {
                // TODO: in the future, consider validating a signed token in place of request to the AccountPlanAPI
                var promotion = await _flurlClient.Request(new UriTemplate(_options.Value.GetPromoCodeTemplate)
                    .AddParameter("planId", planId)
                    .AddParameter("promocode", promocode)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<Promotion>();

                return promotion;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get promocode information {promocode}.");
                throw;
            }
        }

        public async Task<PlanAmountDetails> GetCalculateUpgrade(string accountName, AgreementInformation agreementInformation)
        {
            try
            {
                var PlanAmountDetails = await _flurlClient.Request(new UriTemplate(_options.Value.CalculateUrlTemplate)
                    .AddParameter("accountname", accountName)
                    .AddParameter("planId", agreementInformation.PlanId)
                    .AddParameter("discountId", agreementInformation.DiscountId)
                    .AddParameter("promocode", agreementInformation.Promocode)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return PlanAmountDetails;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get total amount for user {accountName}.");
                throw;
            }
        }
    }
}
