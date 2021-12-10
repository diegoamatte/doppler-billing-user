using Doppler.BillingUser.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapService : ISapService
    {
        private readonly IOptions<SapSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly ICurrentRequestApiTokenGetter _usersApiTokenGetter;

        public SapService(
            IOptions<SapSettings> options,
            ILogger<SapService> logger,
            IFlurlClientFactory flurlClientFac,
            ICurrentRequestApiTokenGetter currentRequestApiTokenGetter)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.SapCreateBusinessPartnerEndpoint);
            _usersApiTokenGetter = currentRequestApiTokenGetter;
        }

        public async Task SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null)
        {
            try
            {
                await _flurlClient.Request(_options.Value.SapBaseUrl + _options.Value.SapCreateBusinessPartnerEndpoint)
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .PostJsonAsync(sapBusinessPartner);

                _logger.LogInformation($"User data successfully sent to DopplerSap. Iduser: {sapBusinessPartner.Id} - ClientManager: {sapBusinessPartner.IsClientManager}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error sending data to DopplerSap");
            }
        }
    }
}
