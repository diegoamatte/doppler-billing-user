using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.EmailSender
{
    public class EmailNotificationsConfiguration
    {
        public string AdminEmail { get; set; }
        public Dictionary<string, string> CreditsApprovedTemplateId { get; set; }
        public string CreditsApprovedAdminTemplateId { get; set; }
        public string UrlEmailImagesBase { get; set; }
    }
}
