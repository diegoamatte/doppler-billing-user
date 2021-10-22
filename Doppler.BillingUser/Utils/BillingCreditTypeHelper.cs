using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.AccountPlans.Utils
{
    public static class BillingCreditTypeHelper
    {
        public static int BillingCreditTypeEnumMapper(User user)
        {
            if (user.CurrentUserTypePlan == null)
            {
                switch (user.NewUserTypePlan.IdUserType)
                {
                    case (int)UserTypeEnum.INDIVIDUAL:
                        return (int)BillingCreditTypeEnum.Free_to_Individual;
                    case (int)UserTypeEnum.MONTHLY:
                        return (int)BillingCreditTypeEnum.Free_to_Monthly;
                    case (int)UserTypeEnum.SUBSCRIBERS:
                        return (int)BillingCreditTypeEnum.Free_to_Subscribers;
                    default:
                        return 0;
                }
            }

            switch (user.CurrentUserTypePlan.IdUserType)
            {
                case (int)UserTypeEnum.INDIVIDUAL:
                    switch (user.NewUserTypePlan.IdUserType)
                    {
                        case (int)UserTypeEnum.INDIVIDUAL:
                            return user.PaymentMethod == (int)PaymentMethodEnum.CC ? (int)BillingCreditTypeEnum.Credit_Buyed_CC : 0; //TODO: check for transfer payments.
                        case (int)UserTypeEnum.MONTHLY:
                            return (int)BillingCreditTypeEnum.Individual_to_Monthly;
                        case (int)UserTypeEnum.SUBSCRIBERS:
                            return (int)BillingCreditTypeEnum.Individual_to_Subscribers;
                        default:
                            return 0;
                    }
                case (int)UserTypeEnum.MONTHLY:
                    if (user.NewUserTypePlan.IdUserType == (int)UserTypeEnum.MONTHLY)
                    {
                        return (int)BillingCreditTypeEnum.Upgrade_Between_Monthlies;
                    }

                    return 0;
                case (int)UserTypeEnum.SUBSCRIBERS:
                    if (user.NewUserTypePlan.IdUserType == (int)UserTypeEnum.SUBSCRIBERS)
                    {
                        return (int)BillingCreditTypeEnum.Upgrade_Between_Subscribers;
                    }

                    if (user.NewUserTypePlan.IdUserType == (int)UserTypeEnum.MONTHLY)
                    {
                        return (int)BillingCreditTypeEnum.Subscribers_to_Monthly;
                    }

                    return 0;
                default:
                    return 0;
            }
        }
    }
}
