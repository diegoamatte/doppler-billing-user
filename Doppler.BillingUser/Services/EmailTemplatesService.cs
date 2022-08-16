using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public class EmailTemplatesService : IEmailTemplatesService
    {
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly IEmailSender _emailSender;
        public EmailTemplatesService(IOptions<EmailNotificationsConfiguration> emailSettings, IEmailSender emailSender)
        {
            _emailSettings = emailSettings;
            _emailSender = emailSender;
        }

        public Task<bool> SendNotificationForSuscribersPlan(string accountname, User userInformation, UserTypePlanInformation newPlan)
        {
            var template = _emailSettings.Value.SubscribersPlanPromotionTemplateId[userInformation.Language ?? "en"];

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        planName = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });
        }

        public Task<bool> SendActivatedStandByEmail(string language, string fistName, int standByAmount, string sendTo)
        {
            var template = _emailSettings.Value.ActivatedStandByNotificationTemplateId[language];

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        firstName = fistName,
                        standByAmount,
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        year = DateTime.Now.Year,
                        isOnlyOneSubscriber = standByAmount == 1,
                    },
                    to: new[] { sendTo });
        }

        public Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, bool isUpgradePending)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAccountTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeRequestTemplateId[userInformation.Language ?? "en"];

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isMonthlyPlan = newPlan.IdUserType == UserType.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        planName = newPlan.IdUserType == UserType.MONTHLY ? newPlan.EmailQty.ToString() : newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodTypes.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodTypes.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodTypes.TRANSF,
                        showMonthDescription = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        discountPlanFee = planDiscountInformation != null ? planDiscountInformation.DiscountPlanFee : 0,
                        isDiscountWith1Month = planDiscountInformation != null && planDiscountInformation.MonthPlan == 1,
                        isDiscountWith3Months = planDiscountInformation != null && planDiscountInformation.MonthPlan == 3,
                        isDiscountWith6Months = planDiscountInformation != null && planDiscountInformation.MonthPlan == 6,
                        isDiscountWith12Months = planDiscountInformation != null && planDiscountInformation.MonthPlan == 12,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = !isUpgradePending ?
                _emailSettings.Value.UpgradeAccountTemplateAdminTemplateId :
                _emailSettings.Value.UpgradeRequestAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerType.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerType.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerType.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        billingEmails = userInformation.BillingEmails,
                        isIndividualPlan = newPlan.IdUserType == UserType.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserType.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodTypes.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodTypes.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodTypes.TRANSF,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(adminEmail, upgradeEmail);
        }

        public Task SendNotificationForCredits(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, bool isUpgradePending)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.CreditsApprovedTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.CheckAndTransferPurchaseNotification[userInformation.Language ?? "en"];

            var creditsEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isIndividualPlan = newPlan.IdUserType == UserType.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserType.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodTypes.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodTypes.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodTypes.TRANSF,
                        availableCreditsQty = partialBalance + newPlan.EmailQty + (promotion != null ? promotion.ExtraCredits ?? 0 : 0),
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = !isUpgradePending ?
                _emailSettings.Value.CreditsApprovedAdminTemplateId :
                _emailSettings.Value.CreditsPendingAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerType.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerType.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerType.RI,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        billingEmails = userInformation.BillingEmails,
                        //userMessage = user.ExclusiveMessage, //TODO: set when the property is set in BilligCredit
                        isIndividualPlan = newPlan.IdUserType == UserType.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserType.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodTypes.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodTypes.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodTypes.TRANSF,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail });

            return Task.WhenAll(creditsEmail, adminEmail);
        }

        public Task SendNotificationForPaymentFailedTransaction(int userId, string errorCode, string errorMessage, string transactionCTR, string bankMessage, PaymentMethodTypes paymentMethod)
        {
            var template = paymentMethod == PaymentMethodTypes.CC ?
                _emailSettings.Value.FailedCreditCardFreeUserPurchaseNotificationAdminTemplateId :
                _emailSettings.Value.FailedMercadoPagoFreeUserPurchaseNotificationAdminTemplateId;

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId,
                        errorCode,
                        errorMessage,
                        transactionCTR,
                        bankMessage,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.CommercialEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);
        }

        public Task SendNotificationForMercadoPagoPaymentApproved(int userId, string accountname)
        {
            var template = _emailSettings.Value.MercadoPagoPaymentApprovedAdminTemplateId;

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId,
                        email = accountname,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail });
        }

        public Task SendNotificationForMercadoPagoPaymentInProcess(int userId, string accountname, string errorCode, string errorMessage)
        {
            var template = _emailSettings.Value.MercadoPagoPaymentInProcessAdminTemplateId;

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId,
                        email = accountname,
                        errorCode,
                        errorMessage,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.CommercialEmail, _emailSettings.Value.BillingEmail });
        }

        public Task SendNotificationForUpdatePlan(
            string accountname,
            User userInformation,
            UserTypePlanInformation currentPlan,
            UserTypePlanInformation newPlan,
            UserBillingInformation user,
            Promotion promotion,
            string promocode,
            int discountId,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails)
        {
            var template = _emailSettings.Value.UpdatePlanTemplateId[userInformation.Language ?? "en"];

            var updatePlanEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isMonthlyPlan = newPlan.IdUserType == UserType.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        planName = newPlan.IdUserType == UserType.MONTHLY ? newPlan.EmailQty.ToString() : newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodTypes.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodTypes.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodTypes.TRANSF,
                        showMonthDescription = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        discountPlanFee = planDiscountInformation != null ? planDiscountInformation.DiscountPlanFee : 0,
                        isDiscountWith1Month = planDiscountInformation != null && planDiscountInformation.MonthPlan == 1,
                        isDiscountWith3Months = planDiscountInformation != null && planDiscountInformation.MonthPlan == 3,
                        isDiscountWith6Months = planDiscountInformation != null && planDiscountInformation.MonthPlan == 6,
                        isDiscountWith12Months = planDiscountInformation != null && planDiscountInformation.MonthPlan == 12,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = _emailSettings.Value.UpdatePlanAdminTemplateId;

            var updatePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerType.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerType.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerType.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        billingEmails = userInformation.BillingEmails,
                        isIndividualPlan = newPlan.IdUserType == UserType.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserType.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserType.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodTypes.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodTypes.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodTypes.TRANSF,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        currentIsIndividualPlan = currentPlan.IdUserType == UserType.INDIVIDUAL,
                        currentIsMonthlyPlan = currentPlan.IdUserType == UserType.MONTHLY,
                        currentIsSubscribersPlan = currentPlan.IdUserType == UserType.SUBSCRIBERS,
                        currentCreditsQty = currentPlan.EmailQty,
                        currentSubscribersQty = currentPlan.Subscribers,
                        currentAmount = currentPlan.Fee,
                        currentIsPaymentMethodCC = currentPlan.PaymentMethod == PaymentMethodTypes.CC,
                        currentIsPaymentMethodMP = currentPlan.PaymentMethod == PaymentMethodTypes.MP,
                        currentIsPaymentMethodTransf = currentPlan.PaymentMethod == PaymentMethodTypes.TRANSF,
                        hasDiscountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0,
                        discountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0 ? amountDetails.DiscountPaymentAlreadyPaid : 0,
                        hasDiscountPlanFeeAdmin = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0,
                        discountPlanFeeAdminAmount = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0 ? amountDetails.DiscountPlanFeeAdmin.Amount : 0,
                        discountPlanFeeAdminPercentage = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.DiscountPercentage > 0 ? amountDetails.DiscountPlanFeeAdmin.DiscountPercentage : 0,
                        hasDiscountPrepayment = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0,
                        discountPrepaymentAmount = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0 ? amountDetails.DiscountPrepayment.Amount : 0,
                        discountPrepaymentPercentage = amountDetails != null && amountDetails.DiscountPrepayment.DiscountPercentage > 0 ? amountDetails.DiscountPrepayment.DiscountPercentage : 0,
                        hasDiscountPromocode = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0,
                        discountPromocodeAmount = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0 ? amountDetails.DiscountPromocode.Amount : 0,
                        discountPromocodePercentage = amountDetails != null && amountDetails.DiscountPromocode.DiscountPercentage > 0 ? amountDetails.DiscountPromocode.DiscountPercentage : 0,
                        total = amountDetails != null ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(updatePlanAdminEmail, updatePlanEmail);
        }

    }
}

