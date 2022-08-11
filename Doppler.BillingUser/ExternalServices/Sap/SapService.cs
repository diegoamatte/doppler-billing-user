using Doppler.BillingUser.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapService : ISapService
    {
        private readonly IOptions<SapSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public SapService(
            IOptions<SapSettings> options,
            ILogger<SapService> logger,
            IFlurlClientFactory flurlClientFac,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.SapCreateBusinessPartnerEndpoint);
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null)
        {
            if (!SapHelper.IsMakingSenseAccount(sapBusinessPartner.Email))
            {
                try
                {
                    await _flurlClient.Request(_options.Value.SapBaseUrl + _options.Value.SapCreateBusinessPartnerEndpoint)
                        .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                        .PostJsonAsync(sapBusinessPartner);

                    _logger.LogInformation("User data successfully sent to DopplerSap. Iduser: {IdUser} - ClientManager: {IsClientManager}", sapBusinessPartner.Id, sapBusinessPartner.IsClientManager);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error sending data to DopplerSap");
                }
            }
        }
        public async Task SendBillingToSap(SapBillingDto sapBilling, string email)
        {
            if (!SapHelper.IsMakingSenseAccount(email))
            {
                try
                {
                    await _flurlClient.Request(_options.Value.SapBaseUrl + _options.Value.SapCreateBillingRequestEndpoint)
                        .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                        .PostJsonAsync(new List<SapBillingDto>() { sapBilling });

                    _logger.LogInformation("User billing data successfully sent to Sap. User: {email}", email);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error sending invoice data to Sap");
                }
            }
        }
    }
}
