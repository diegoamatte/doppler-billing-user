using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.EmailSender
{
    public class EmailNotificationsConfiguration
    {
        public string AdminEmail { get; set; }
        public Dictionary<string, string> CreditsApprovedTemplateId { get; set; }
        public Dictionary<string, string> UpgradeAccountTemplateId { get; set; }
        public Dictionary<string, string> SubscribersPlanPromotionTemplateId { get; set; }
        public string CreditsApprovedAdminTemplateId { get; set; }
        public string UpgradeAccountTemplateAdminTemplateId { get; set; }
        public string UrlEmailImagesBase { get; set; }
        public Dictionary<string, string> ActivatedStandByNotificationTemplateId { get; set; }
        public Dictionary<string, string> CheckAndTransferPurchaseNotification { get; set; }
    }
}
