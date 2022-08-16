using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Utils
{
    public static class SourceTypeHelper
    {
        public static SourceType SourceTypeEnumMapper(UserTypePlanInformation planInformation)
        {
            return planInformation.IdUserType == UserType.INDIVIDUAL
                ? SourceType.BuyCreditsId
                : SourceType.UpgradeId;
        }
    }
}
