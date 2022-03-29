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
        public Task<bool> SendCheckAndTransferPurchaseNotification(string language, string fistName, string planName, double? amount, string paymentMethod, int? creditsQuantity, string sendTo)
        {
            var templateUser = _emailSettings.Value.CheckAndTransferPurchaseNotification[language];

            //Send email to user
            var userEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateUser,
                    templateModel: new
                    {
                        firstName = fistName,
                        planName = planName,
                        amount = amount,
                        paymentMethod = paymentMethod,
                        creditsQty = creditsQuantity,
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        year = DateTime.Now.Year,
                    },
                    to: new[] { sendTo });

            return userEmail;
        }
        public Task<bool> SendCreditsApprovedAdminNotification(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode)
        {
            var templateAdmin = _emailSettings.Value.CreditsApprovedAdminTemplateId;

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
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
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
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail });

            return adminEmail;
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
                        standByAmount = standByAmount,
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        year = DateTime.Now.Year,
                        isOnlyOneSubscriber = standByAmount == 1,
                    },
                    to: new[] { sendTo });
        }

        public Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation)
        {
            var template = _emailSettings.Value.UpgradeAccountTemplateId[userInformation.Language ?? "en"];

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        planName = newPlan.IdUserType == UserTypeEnum.MONTHLY ? newPlan.EmailQty.ToString() : newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        showMonthDescription = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        discountPlanFee = planDiscountInformation != null ? planDiscountInformation.DiscountPlanFee : 0,
                        isDiscountWith1Month = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 1 : false,
                        isDiscountWith3Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 3 : false,
                        isDiscountWith6Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 6 : false,
                        isDiscountWith12Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 12 : false,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = _emailSettings.Value.UpgradeAccountTemplateAdminTemplateId;

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
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
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
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail });

            return Task.WhenAll(adminEmail, upgradeEmail);
        }

        public Task SendNotificationForCreditsApproved(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode)
        {
            var template = _emailSettings.Value.CreditsApprovedTemplateId[userInformation.Language ?? "en"];

            var creditsEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        availableCreditsQty = partialBalance + newPlan.EmailQty + (promotion != null ? promotion.ExtraCredits ?? 0 : 0),
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = _emailSettings.Value.CreditsApprovedAdminTemplateId;

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
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
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
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail });

            return Task.WhenAll(creditsEmail, adminEmail);
        }
    }
}

