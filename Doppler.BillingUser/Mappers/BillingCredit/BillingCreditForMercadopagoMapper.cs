using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit
{
    public class BillingCreditForMercadopagoMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IEncryptionService _encryptionService;

        private const int CF = 1;

        public BillingCreditForMercadopagoMapper(IBillingRepository billingRepository, ICurrencyRepository currencyRepository, IEncryptionService encryptionService)
        {
            _billingRepository = billingRepository;
            _currencyRepository = currencyRepository;
            _encryptionService = encryptionService;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = currentPaymentMethod.IdCCType,
                CCExpMonth = short.Parse(currentPaymentMethod.CCExpMonth),
                CCExpYear = short.Parse(currentPaymentMethod.CCExpYear),
                CCHolderFullName = currentPaymentMethod.CCHolderFullName,
                CCIdentificationType = currentPaymentMethod.CCType,
                CCIdentificationNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(currentPaymentMethod.CCNumber)),
                CCNumber = currentPaymentMethod.CCNumber,
                CCVerification = currentPaymentMethod.CCVerification,
                IdConsumerType = CF,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                IdPromotion = promotion?.IdPromotion
            };

            DateTime now = DateTime.UtcNow;
            var isUpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);

            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = !isUpgradePending ? now : null,
                ActivationDate = !isUpgradePending ? now : null,
                Approved = !isUpgradePending,
                Payed = !isUpgradePending,
                IdUserTypePlan = newUserTypePlan.IdUserTypePlan,
                PlanFee = newUserTypePlan.Fee,
                CreditsQty = newUserTypePlan.EmailQty ?? null,
                ExtraEmailFee = newUserTypePlan.ExtraEmailCost ?? null,
                ExtraCreditsPromotion = promotion?.ExtraCredits,
                DiscountPlanFeePromotion = promotion?.DiscountPercentage
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

            if (agreementInformation.Total != 0)
            {
                var rate = await _currencyRepository.GetCurrencyRateAsync((int)CurrencyTypeEnum.UsS, (int)CurrencyTypeEnum.sARG, DateTime.UtcNow);
                var amount = await _currencyRepository.ConvertCurrencyAsync((int)CurrencyTypeEnum.UsS, (int)CurrencyTypeEnum.sARG, agreementInformation.Total.Value, DateTime.UtcNow, rate);
                var taxes = CalculateInvoiceTaxes(amount);
                buyCreditAgreement.BillingCredit.Taxes = (double)await _currencyRepository.ConvertCurrencyAsync((int)CurrencyTypeEnum.sARG, (int)CurrencyTypeEnum.UsS, taxes, DateTime.UtcNow, (1 / rate));
            }


            buyCreditAgreement.IdResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago;

            return buyCreditAgreement;
        }

        private static decimal CalculateInvoiceTaxes(decimal amount)
        {
            decimal coefficient = 0.21m;
            return amount * coefficient;
        }
    }
}
