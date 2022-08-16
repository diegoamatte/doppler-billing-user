using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.EmailSender
{
    public partial class RelayEmailSender : IEmailSender
    {
        private readonly ILogger<RelayEmailSender> _logger;
        private readonly RelayEmailSenderSettings _config;
        private readonly IFlurlClient _flurlClient;

        [LoggerMessage(0, LogLevel.Error, "Error sending email with template")]
        partial void LogErrorSendingEmail(Exception ex);

        public RelayEmailSender(IOptions<RelayEmailSenderSettings> config, ILogger<RelayEmailSender> logger, IFlurlClientFactory flurlClientFac)
        {
            _config = config.Value;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_config.SendTemplateUrlTemplate).WithOAuthBearerToken(_config.ApiKey);
        }

        public async Task<bool> SafeSendWithTemplateAsync(
            string templateId,
            object templateModel,
            IEnumerable<string> toEmail,
            IEnumerable<string> cc = null,
            IEnumerable<string> bcc = null,
            string fromName = null,
            string fromAddress = null,
            string replyTo = null,
            IEnumerable<Attachment> attachments = null,
            CancellationToken cancellationToken = default)
        {
            {
                try
                {
                    await SendWithTemplateAsync(templateId, templateModel, toEmail, cc, bcc, fromName, fromAddress, replyTo, attachments, cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    LogErrorSendingEmail(ex);
                    return false;
                }
            }
        }

        public async Task SendWithTemplateAsync(
            string templateId, object templateModel,
            IEnumerable<string> toEmail,
            IEnumerable<string> cc = null,
            IEnumerable<string> bcc = null,
            string fromName = null,
            string fromAddress = null,
            string replyTo = null,
            IEnumerable<Attachment> attachments = null,
            CancellationToken cancellationToken = default
            )
        {
            var recipients = (
                from emailAddress in toEmail ?? Enumerable.Empty<string>()
                select new { email = emailAddress, type = "to" }).Union(
                from emailAddress in cc ?? Enumerable.Empty<string>() select new { email = emailAddress, type = "cc" }).Union(
                from emailAddress in bcc ?? Enumerable.Empty<string>() select new { email = emailAddress, type = "bcc" }).ToArray();

            await _flurlClient.Request(new UriTemplate(_config.SendTemplateUrlTemplate)
                    .AddParameter("accountId", _config.AccountId)
                    .AddParameter("accountName", _config.AccountName)
                    .AddParameter("username", _config.Username)
                    .AddParameter("templateId", templateId)
                    .Resolve())
                .PostJsonAsync(new
                {
                    from_name = fromName ?? _config.FromName,
                    from_email = fromAddress ?? _config.FromAddress,
                    recipients,
                    attachments = attachments?.Select(x => new
                    {
                        content_type = x.ContentType.ToString(),
                        base64_content = Convert.ToBase64String(x.Content),
                        filename = x.Filename
                    }),
                    model = templateModel,
                    reply_to = new { email = replyTo ?? _config.ReplyToAddress, name = _config.FromName }
                }, cancellationToken);
        }
    }
}
