using System.ComponentModel;

namespace Doppler.BillingUser.Enums
{
    public enum UserType
    {
        [Description("Free")]
        FREE = 1,
        [Description("Monthly")]
        MONTHLY = 2,
        [Description("Individual")]
        INDIVIDUAL = 3,
        [Description("Subscribers")]
        SUBSCRIBERS = 4
    }
}
