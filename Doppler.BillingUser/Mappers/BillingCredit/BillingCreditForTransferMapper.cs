using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit
{
    public class BillingCreditForTransferMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;

        private const int MexicoIva = 16;
        private const int ArgentinaIva = 21;

        public BillingCreditForTransferMapper(IBillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = null,
                CCExpMonth = null,
                CCExpYear = null,
                CCHolderFullName = null,
                CCIdentificationType = null,
                CCIdentificationNumber = null,
                CCNumber = null,
                CCVerification = null,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                CFDIUse = user.CFDIUse,
                PaymentWay = user.PaymentWay,
                PaymentType = user.PaymentType,
                BankName = user.BankName,
                BankAccount = user.BankAccount,
                IdPromotion = promotion?.IdPromotion
            };

            DateTime now = DateTime.UtcNow;
            var isUpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);

            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = (billingCreditType == BillingCreditTypeEnum.UpgradeRequest || billingCreditType == BillingCreditTypeEnum.Credit_Request) ? !isUpgradePending ? now : null : null,
                ActivationDate = (billingCreditType == BillingCreditTypeEnum.UpgradeRequest || billingCreditType == BillingCreditTypeEnum.Credit_Request) ? !isUpgradePending ? now : null : now,
                Approved = (billingCreditType != BillingCreditTypeEnum.UpgradeRequest && billingCreditType != BillingCreditTypeEnum.Credit_Request) || !isUpgradePending,
                Payed = (billingCreditType == BillingCreditTypeEnum.UpgradeRequest || billingCreditType == BillingCreditTypeEnum.Credit_Request) && !isUpgradePending,
                IdUserTypePlan = newUserTypePlan.IdUserTypePlan,
                PlanFee = newUserTypePlan.Fee,
                CreditsQty = newUserTypePlan.EmailQty ?? null,
                ExtraEmailFee = newUserTypePlan.ExtraEmailCost ?? null,
                ExtraCreditsPromotion = promotion?.ExtraCredits,
                DiscountPlanFeePromotion = promotion?.DiscountPercentage,
                IdBillingCreditType = (int)billingCreditType
            };

            if (newUserTypePlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
            {
                var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(agreementInformation.DiscountId);

                buyCreditAgreement.BillingCredit.IdDiscountPlan = agreementInformation.DiscountId != 0 ? agreementInformation.DiscountId : null;
                buyCreditAgreement.BillingCredit.TotalMonthPlan = planDiscountInformation?.MonthPlan;
                buyCreditAgreement.BillingCredit.CurrentMonthPlan =
                    (buyCreditAgreement.BillingCredit.TotalMonthPlan.HasValue
                    && buyCreditAgreement.BillingCredit.TotalMonthPlan.Value > 1
                    && buyCreditAgreement.BillingCredit.Date.Day > 20)
                    ? 0 : 1;
                buyCreditAgreement.BillingCredit.SubscribersQty = newUserTypePlan.SubscribersQty;
            }

            //Calculate the BillingSystem
            buyCreditAgreement.IdResponsabileBilling = CalculateBillingSystemByTransfer(user.IdBillingCountry);

            if (user.PaymentMethod == PaymentMethodEnum.TRANSF &&
                (user.IdBillingCountry == (int)CountryEnum.Mexico || user.IdBillingCountry == (int)CountryEnum.Argentina))
            {
                int iva = (user.IdBillingCountry == (int)CountryEnum.Mexico) ? MexicoIva : ArgentinaIva;
                buyCreditAgreement.BillingCredit.Taxes = Convert.ToDouble(agreementInformation.Total * iva / 100);
            }

            return buyCreditAgreement;
        }

        private int CalculateBillingSystemByTransfer(int idBillingCountry)
        {
            return idBillingCountry switch
            {
                (int)CountryEnum.Colombia => (int)ResponsabileBillingEnum.BorisMarketing,
                (int)CountryEnum.Mexico => (int)ResponsabileBillingEnum.RC,
                (int)CountryEnum.Argentina => (int)ResponsabileBillingEnum.GBBISIDE,
                _ => (int)ResponsabileBillingEnum.GBBISIDE,
            };
        }
    }
}
