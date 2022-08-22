using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public partial class SapService : ISapService
    {
        private readonly IOptions<SapSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        [LoggerMessage(0, LogLevel.Information, "User data successfully sent to DopplerSap. Iduser: {idUser} - ClientManager: {isClientManager}")]
        partial void LogInfoDataSendToSap(int idUser, bool isClientManager);

        [LoggerMessage(1, LogLevel.Information, "User billing data successfully sent to Sap. User: {email}")]
        partial void LogInfoBillingDataSendToSap(string email);

        [LoggerMessage(2, LogLevel.Error, "{message}")]
        partial void LogErrorExceptionWithMessage(Exception e, string message);

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
                    LogInfoDataSendToSap(sapBusinessPartner.Id, sapBusinessPartner.IsClientManager);
                }
                catch (Exception e)
                {

                    LogErrorExceptionWithMessage(e, "Unexpected error sending data to DopplerSap");
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

                    LogInfoBillingDataSendToSap(email);
                }
                catch (Exception e)
                {
                    LogErrorExceptionWithMessage(e, "Unexpected error sending invoice data to Sap");
                }
            }
        }
    }
}
