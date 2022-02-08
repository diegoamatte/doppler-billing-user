using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Utils
{
    public static class SourceTypeHelper
    {
        public static SourceTypeEnum SourceTypeEnumMapper(UserTypePlanInformation planInformation)
        {
            return planInformation.IdUserType == UserTypeEnum.INDIVIDUAL
                ? SourceTypeEnum.BuyCreditsId
                : SourceTypeEnum.UpgradeId;
        }
    }
}
