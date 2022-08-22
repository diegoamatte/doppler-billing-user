using System;
using System.Globalization;
using System.Threading.Tasks;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;

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

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment, BillingCreditType billingCreditType)
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
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType, CultureInfo.InvariantCulture) : null,
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

            var now = DateTime.UtcNow;
            var isUpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);

            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = (billingCreditType is BillingCreditType.UpgradeRequest or BillingCreditType.CreditRequest) ? !isUpgradePending ? now : null : null,
                ActivationDate = (billingCreditType is BillingCreditType.UpgradeRequest or BillingCreditType.CreditRequest) ? !isUpgradePending ? now : null : now,
                Approved = (billingCreditType != BillingCreditType.UpgradeRequest && billingCreditType != BillingCreditType.CreditRequest) || !isUpgradePending,
                Payed = (billingCreditType == BillingCreditType.UpgradeRequest || billingCreditType == BillingCreditType.CreditRequest) && !isUpgradePending,
                IdUserTypePlan = newUserTypePlan.IdUserTypePlan,
                PlanFee = newUserTypePlan.Fee,
                CreditsQty = newUserTypePlan.EmailQty ?? null,
                ExtraEmailFee = newUserTypePlan.ExtraEmailCost ?? null,
                ExtraCreditsPromotion = promotion?.ExtraCredits,
                DiscountPlanFeePromotion = promotion?.DiscountPercentage,
                IdBillingCreditType = (int)billingCreditType
            };

            if (newUserTypePlan.IdUserType == UserType.SUBSCRIBERS)
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

            if (user.PaymentMethod == PaymentMethodTypes.TRANSF &&
                (user.IdBillingCountry == (int)Country.Mexico || user.IdBillingCountry == (int)Country.Argentina))
            {
                var iva = (user.IdBillingCountry == (int)Country.Mexico) ? MexicoIva : ArgentinaIva;
                buyCreditAgreement.BillingCredit.Taxes = Convert.ToDouble(agreementInformation.Total * iva / 100, CultureInfo.InvariantCulture);
            }

            return buyCreditAgreement;
        }

        private static int CalculateBillingSystemByTransfer(int idBillingCountry)
        {
            return idBillingCountry switch
            {
                (int)Country.Colombia => (int)ResponsabileBilling.BorisMarketing,
                (int)Country.Mexico => (int)ResponsabileBilling.RC,
                (int)Country.Argentina => (int)ResponsabileBilling.GBBISIDE,
                _ => (int)ResponsabileBilling.GBBISIDE,
            };
        }
    }
}
