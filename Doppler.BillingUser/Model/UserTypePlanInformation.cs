using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class UserTypePlanInformation
    {
        public int IdUserTypePlan { get; set; }
        public UserTypeEnum IdUserType { get; set; }
        public int? EmailQty { get; set; }
        public double? Fee { get; set; }
        public double? ExtraEmailCost { get; set; }
        public int? SubscribersQty { get; set; }
        public string? Subscribers { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; }
    }
}
