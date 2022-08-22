using System.Threading.Tasks;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Mappers.BillingCredit
{
    public interface IBillingCreditMapper
    {
        Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment, BillingCreditType billingCreditType);
    }
}
