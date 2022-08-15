using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.ExternalServices.Slack
{
    public partial class SlackService : ISlackService
    {
        private readonly SlackSettings _slackSettings;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger _logger;

        [LoggerMessage(0, LogLevel.Error, "Unexpected error sending slack notification")]
        partial void LogErrorSendingSlackNotification(Exception ex);

        public SlackService(IOptions<SlackSettings> slackSettings, IFlurlClientFactory flurlClientFac, ILogger<SlackService> logger)
        {
            _slackSettings = slackSettings.Value;
            _flurlClient = flurlClientFac.Get(_slackSettings.Url);
            _logger = logger;
        }

        public async Task SendNotification(string message = null)
        {
            try
            {
                await _flurlClient.Request(_slackSettings.Url).PostJsonAsync(new { text = message });
            }
            catch (Exception e)
            {
                LogErrorSendingSlackNotification(e);
            }
        }
    }
}
