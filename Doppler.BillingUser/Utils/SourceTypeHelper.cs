using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.AccountPlans.Utils
{
    public static class SourceTypeHelper
    {
        public static int SourceTypeEnumMapper(User user)
        {
            if (user.CurrentUserTypePlan == null)
            {
                return user.NewUserTypePlan.IdUserType == (int)UserTypeEnum.INDIVIDUAL
                    ? (int)SourceTypeEnum.BUY_CREDITS_ID
                    : (int)SourceTypeEnum.UPGRADE_ID;
            }
            else
            {
                return user.CurrentUserTypePlan.IdUserType == (int)UserTypeEnum.INDIVIDUAL && user.NewUserTypePlan.IdUserType == (int)UserTypeEnum.INDIVIDUAL
                    ? (int)SourceTypeEnum.BUY_CREDITS_ID
                    : (int)SourceTypeEnum.UPSELLING_ID;
            }
        }
    }
}
