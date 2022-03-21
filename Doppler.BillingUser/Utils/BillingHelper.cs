using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Utils
{
    public static class BillingHelper
    {
        public static bool IsUpgradePending(UserBillingInformation user, Promotion promotion)
        {
            if (promotion != null)
            {
                return user.PaymentMethod == PaymentMethodEnum.TRANSF &&
                    ((promotion.DiscountPercentage.HasValue &&
                    promotion.DiscountPercentage.Value < 100) ||
                    !promotion.DiscountPercentage.HasValue);
            }


            return user.PaymentMethod != PaymentMethodEnum.CC;
        }
    }
}
