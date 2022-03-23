using System;
using Doppler.BillingUser.DopplerSecurity;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using FluentValidation;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Encryption;
using System.Linq;
using Doppler.BillingUser.ExternalServices.Slack;
using Microsoft.Extensions.Options;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.Utils;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Newtonsoft.Json;
using System.Collections.Generic;
using Doppler.BillingUser.Services;

namespace Doppler.BillingUser.Controllers
{
    [Authorize]
    [ApiController]
    public class BillingController
    {
        private readonly ILogger _logger;
        private readonly IBillingRepository _billingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<BillingInformation> _billingInformationValidator;
        private readonly IAccountPlansService _accountPlansService;
        private readonly IValidator<AgreementInformation> _agreementInformationValidator;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly ISapService _sapService;
        private readonly IEncryptionService _encryptionService;
        private readonly IOptions<SapSettings> _sapSettings;
        private readonly IPromotionRepository _promotionRepository;
        private readonly ISlackService _slackService;
        private readonly IOptions<ZohoSettings> _zohoSettings;
        private readonly IZohoService _zohoService;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private const int CurrencyTypeUsd = 0;
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore
        };
        private static readonly List<UserTypeEnum> AllowedPlanTypesForBilling = new List<UserTypeEnum>
        {
            UserTypeEnum.INDIVIDUAL,
            UserTypeEnum.MONTHLY,
            UserTypeEnum.SUBSCRIBERS
        };
        private static readonly List<PaymentMethodEnum> AllowedPaymentMethodsForBilling = new List<PaymentMethodEnum>
        {
            PaymentMethodEnum.CC,
            PaymentMethodEnum.TRANSF
        };

        public BillingController(
            ILogger<BillingController> logger,
            IBillingRepository billingRepository,
            IUserRepository userRepository,
            IValidator<BillingInformation> billingInformationValidator,
            IValidator<AgreementInformation> agreementInformationValidator,
            IAccountPlansService accountPlansService,
            IPaymentGateway paymentGateway,
            ISapService sapService,
            IEncryptionService encryptionService,
            IOptions<SapSettings> sapSettings,
            IPromotionRepository promotionRepository,
            ISlackService slackService,
            IEmailSender emailSender,
            IOptions<EmailNotificationsConfiguration> emailSettings,
            IOptions<ZohoSettings> zohoSettings,
            IZohoService zohoService,
            IEmailTemplatesService emailTemplatesService)
        {
            _logger = logger;
            _billingRepository = billingRepository;
            _userRepository = userRepository;
            _billingInformationValidator = billingInformationValidator;
            _agreementInformationValidator = agreementInformationValidator;
            _accountPlansService = accountPlansService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
            _emailSender = emailSender;
            _emailSettings = emailSettings;
            _encryptionService = encryptionService;
            _sapSettings = sapSettings;
            _promotionRepository = promotionRepository;
            _slackService = slackService;
            _zohoSettings = zohoSettings;
            _zohoService = zohoService;
            _emailTemplatesService = emailTemplatesService;
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountName}/billing-information")]
        public async Task<IActionResult> GetBillingInformation(string accountName)
        {
            var billingInformation = await _billingRepository.GetBillingInformation(accountName);

            if (billingInformation == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(billingInformation);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information")]
        public async Task<IActionResult> UpdateBillingInformation(string accountname, [FromBody] BillingInformation billingInformation)
        {
            var results = await _billingInformationValidator.ValidateAsync(billingInformation);
            if (!results.IsValid)
            {
                return new BadRequestObjectResult(results.ToString("-"));
            }

            await _billingRepository.UpdateBillingInformation(accountname, billingInformation);

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> GetInvoiceRecipients(string accountname)
        {
            var result = await _billingRepository.GetInvoiceRecipients(accountname);

            if (result == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(result);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> UpdateInvoiceRecipients(string accountname, [FromBody] InvoiceRecipients invoiceRecipients)
        {
            await _billingRepository.UpdateInvoiceRecipients(accountname, invoiceRecipients.Recipients, invoiceRecipients.PlanId);

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> GetCurrentPaymentMethod(string accountname)
        {
            _logger.LogDebug("Get current payment method.");

            var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

            if (currentPaymentMethod == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(currentPaymentMethod);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> UpdateCurrentPaymentMethod(string accountname, [FromBody] PaymentMethod paymentMethod)
        {
            _logger.LogDebug("Update current payment method.");

            var isSuccess = await _billingRepository.UpdateCurrentPaymentMethod(accountname, paymentMethod);

            if (!isSuccess)
            {
                var messageError = $"Failed at updating payment method for user {accountname}, Invalid Credit Card";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
                return new BadRequestObjectResult("Invalid Credit Card");
            }

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/plans/current")]
        public async Task<IActionResult> GetCurrentPlan(string accountname)
        {
            _logger.LogDebug("Get current plan.");

            var currentPlan = await _billingRepository.GetCurrentPlan(accountname);

            if (currentPlan == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(currentPlan);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPost("/accounts/{accountname}/agreements")]
        public async Task<IActionResult> CreateAgreement([FromRoute] string accountname, [FromBody] AgreementInformation agreementInformation)
        {
            try
            {
                var results = await _agreementInformationValidator.ValidateAsync(agreementInformation);
                if (!results.IsValid)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Validation error {results.ToString("-")}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult(results.ToString("-"));
                }

                var user = await _userRepository.GetUserBillingInformation(accountname);
                if (user == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid user");
                }

                if (!AllowedPaymentMethodsForBilling.Any(p => p == user.PaymentMethod))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid payment method {user.PaymentMethod}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                if (user.PaymentMethod == PaymentMethodEnum.TRANSF && user.IdBillingCountry != (int)CountryEnum.Colombia)
                {
                    var messageErrorTransference = $"Failed at creating new agreement for user {accountname}, payment method {user.PaymentMethod} it's only supported for Colombia";
                    _logger.LogError(messageErrorTransference);
                    await _slackService.SendNotification(messageErrorTransference);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                var currentPlan = await _userRepository.GetUserCurrentTypePlan(user.IdUser);
                if (currentPlan != null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user type (only free users) {currentPlan.IdUserType}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid user type (only free users)");
                }

                var newPlan = await _userRepository.GetUserNewTypePlan(agreementInformation.PlanId);
                if (newPlan == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected plan {agreementInformation.PlanId}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid selected plan");
                }

                if (!AllowedPlanTypesForBilling.Any(p => p == newPlan.IdUserType))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, invalid selected plan type {newPlan.IdUserType}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid selected plan type");
                }

                var isValidTotal = await _accountPlansService.IsValidTotal(accountname, agreementInformation);

                if (!isValidTotal)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Total of agreement is not valid";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Total of agreement is not valid");
                }

                Promotion promotion = null;
                if (!string.IsNullOrEmpty(agreementInformation.Promocode))
                {
                    promotion = await _accountPlansService.GetValidPromotionByCode(agreementInformation.Promocode, agreementInformation.PlanId);
                }

                int invoiceId = 0;
                string authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;
                if (agreementInformation.Total.GetValueOrDefault() > 0 && user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                    if (encryptedCreditCard == null)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, missing credit card information";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new ObjectResult("User credit card missing")
                        {
                            StatusCode = 500
                        };
                    }

                    // TODO: Deal with first data exceptions.
                    authorizationNumber = await _paymentGateway.CreateCreditCardPayment(agreementInformation.Total.GetValueOrDefault(), encryptedCreditCard, user.IdUser);
                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(agreementInformation, encryptedCreditCard, user.IdUser, newPlan, authorizationNumber);
                }

                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(agreementInformation, user, newPlan, promotion);

                user.IdCurrentBillingCredit = billingCreditId;
                user.OriginInbound = agreementInformation.OriginInbound;
                user.UpgradePending = BillingHelper.IsUpgradePending(user, promotion);

                if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS && newPlan.SubscribersQty.HasValue)
                    user.MaxSubscribers = newPlan.SubscribersQty.Value;

                await _userRepository.UpdateUserBillingCredit(user);

                var partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                User userInformation = await _userRepository.GetUserInformation(accountname);

                if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                {
                    await _billingRepository.UpdateUserSubscriberLimitsAsync(user.IdUser);
                    var activatedStandByAmount = await _billingRepository.ActivateStandBySubscribers(user.IdUser);
                    if (activatedStandByAmount > 0)
                    {
                        var lang = userInformation.Language ?? "en";
                        await SendActivatedStandByEmail(lang, userInformation.FirstName, activatedStandByAmount, user.Email);
                    }
                }
                else
                {
                    await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);
                }

                if (promotion != null)
                    await _promotionRepository.IncrementUsedTimes(promotion);

                if (agreementInformation.Total.GetValueOrDefault() > 0 && user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    await _sapService.SendBillingToSap(
                        await MapBillingToSapAsync(encryptedCreditCard, currentPlan, newPlan, authorizationNumber,
                            invoiceId, billingCreditId),
                        accountname);
                }

                //Send notifications
                SendNotifications(accountname, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId);

                var message = $"Successful at creating a new agreement for: User: {accountname} - Plan: {agreementInformation.PlanId}";
                await _slackService.SendNotification(message + (!string.IsNullOrEmpty(agreementInformation.Promocode) ? $" - Promocode {agreementInformation.Promocode}" : string.Empty));

                if (_zohoSettings.Value.UseZoho)
                {
                    ZohoDTO zohoDto = new ZohoDTO()
                    {
                        Email = user.Email,
                        UpgradeDate = DateTime.UtcNow,
                        FirstPaymentDate = DateTime.UtcNow,
                        Doppler = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL ? "Individual" : "Monthly", // TODO: check for other plan types
                        BillingSystem = PaymentMethodEnum.CC.ToString(),
                        OriginInbound = agreementInformation.OriginInbound
                    };

                    if (promotion != null)
                    {
                        zohoDto.PromoCodo = agreementInformation.Promocode;
                        if (promotion.ExtraCredits.HasValue && promotion.ExtraCredits.Value != 0)
                            zohoDto.DiscountType = ZohoDopplerValues.Credits;
                        else if (promotion.DiscountPercentage.HasValue && promotion.DiscountPercentage.Value != 0)
                            zohoDto.DiscountType = ZohoDopplerValues.Discount;
                    }

                    try
                    {
                        var contact = await _zohoService.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", string.Format("Email:equals:{0}", zohoDto.Email));
                        if (contact == null)
                        {
                            var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", string.Format("Email:equals:{0}", zohoDto.Email));
                            if (response != null)
                            {
                                var lead = response.Data.FirstOrDefault();
                                MapForUpgrade(lead, zohoDto);
                                var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityLead> { Data = new List<ZohoEntityLead> { lead } }, settings);
                                await _zohoService.UpdateZohoEntityAsync(body, lead.Id, "Leads");
                            }
                        }
                        else
                        {
                            if (contact.AccountName != null && !string.IsNullOrEmpty(contact.AccountName.Name))
                            {
                                var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityAccount>>("Accounts", string.Format("Account_Name:equals:{0}", contact.AccountName.Name));
                                if (response != null)
                                {
                                    var account = response.Data.FirstOrDefault();
                                    MapForUpgrade(account, zohoDto);
                                    var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityAccount> { Data = new List<ZohoEntityAccount> { account } }, settings);
                                    await _zohoService.UpdateZohoEntityAsync(body, account.Id, "Accounts");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var messageError = $"Failed at updating lead from zoho {accountname} with exception {e.Message}";
                        _logger.LogError(e, messageError);
                        await _slackService.SendNotification(messageError);
                    }
                }

                return new OkObjectResult("Successfully");
            }
            catch (Exception e)
            {
                var messageError = $"Failed at creating new agreement for user {accountname} with exception {e.Message}";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);
                return new ObjectResult("Failed at creating new agreement")
                {
                    StatusCode = 500
                };
            }
        }

        private async Task<SapBillingDto> MapBillingToSapAsync(CreditCard creditCard, UserTypePlanInformation currentUserPlan, UserTypePlanInformation newUserPlan, string authorizationNumber, int invoidId, int billingCreditId)
        {
            var billingCredit = await _billingRepository.GetBillingCredit(billingCreditId);
            var cardNumber = _encryptionService.DecryptAES256(creditCard.Number);

            var sapBilling = new SapBillingDto
            {
                Id = billingCredit.IdUser,
                CreditsOrSubscribersQuantity = newUserPlan.IdUserType == UserTypeEnum.SUBSCRIBERS ? newUserPlan.SubscribersQty.GetValueOrDefault() : billingCredit.CreditsQty.GetValueOrDefault(),
                IsCustomPlan = (new[] { 0, 9, 17 }).Contains(billingCredit.IdUserTypePlan),
                IsPlanUpgrade = true, // TODO: Check when the other types of purchases are implemented.
                Currency = CurrencyTypeUsd,
                Periodicity = GetPeriodicity(newUserPlan, billingCredit),
                PeriodMonth = billingCredit.Date.Month,
                PeriodYear = billingCredit.Date.Year,
                PlanFee = newUserPlan.IdUserType == UserTypeEnum.SUBSCRIBERS ? billingCredit.PlanFee * (billingCredit.TotalMonthPlan ?? 1) : billingCredit.PlanFee,
                Discount = billingCredit.DiscountPlanFee,
                ExtraEmailsPeriodMonth = billingCredit.Date.Month,
                ExtraEmailsPeriodYear = billingCredit.Date.Year,
                ExtraEmailsFee = 0,
                IsFirstPurchase = currentUserPlan == null,
                PlanType = (int)newUserPlan.IdUserType,
                CardHolder = _encryptionService.DecryptAES256(creditCard.HolderName),
                CardType = billingCredit.CCIdentificationType,
                CardNumber = cardNumber[^4..],
                CardErrorCode = "100",
                CardErrorDetail = "Successfully approved",
                TransactionApproved = true,
                TransferReference = authorizationNumber,
                InvoiceId = invoidId,
                PaymentDate = billingCredit.Date.ToHourOffset(_sapSettings.Value.TimeZoneOffset),
                InvoiceDate = billingCredit.Date.ToHourOffset(_sapSettings.Value.TimeZoneOffset),
                BillingSystemId = billingCredit.IdResponsabileBilling
            };

            return sapBilling;
        }

        private int? GetPeriodicity(UserTypePlanInformation newUserPlan, BillingCredit billingCredit)
        {
            return newUserPlan.IdUserType == UserTypeEnum.INDIVIDUAL ?
                null : billingCredit.TotalMonthPlan == 3 ?
                1 : billingCredit.TotalMonthPlan == 6 ?
                2 : billingCredit.TotalMonthPlan == 12 ? 3 : 0;
        }

        private void MapForUpgrade(ZohoEntityLead lead, ZohoDTO zohoDto)
        {
            lead.Doppler = zohoDto.Doppler;
            if (zohoDto.FirstPaymentDate == DateTime.MinValue)
            {
                lead.DFirstPayment = null;
            }
            else
            {
                lead.DFirstPayment = zohoDto.FirstPaymentDate;
            }
            lead.DDiscountType = zohoDto.DiscountType;
            lead.DBillingSystem = zohoDto.BillingSystem;
            if (zohoDto.UpgradeDate == DateTime.MinValue)
            {
                lead.DUpgradeDate = null;
            }
            else
            {
                lead.DUpgradeDate = zohoDto.UpgradeDate;
            }
            lead.DPromoCode = zohoDto.PromoCodo;
            lead.DDiscountTypeDesc = zohoDto.DiscountTypeDescription;
            lead.Industry = zohoDto.Industry;
        }

        private void MapForUpgrade(ZohoEntityAccount account, ZohoDTO zohoDto)
        {
            account.Doppler = zohoDto.Doppler;
            if (zohoDto.FirstPaymentDate == DateTime.MinValue)
            {
                account.DFirstPayment = null;
            }
            else
            {
                account.DFirstPayment = zohoDto.FirstPaymentDate;
            }
            account.DDiscountType = zohoDto.DiscountType;
            account.DBillingSystem = zohoDto.BillingSystem;
            if (zohoDto.UpgradeDate == DateTime.MinValue)
            {
                account.DUpgradeDate = null;
            }
            else
            {
                account.DUpgradeDate = zohoDto.UpgradeDate;
            }
            account.DPromoCode = zohoDto.PromoCodo;
            account.DDiscountTypeDesc = zohoDto.DiscountTypeDescription;
            account.Industry = zohoDto.Industry;
        }

        private async void SendNotifications(string accountname, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, int discountId)
        {
            User userInformation = await _userRepository.GetUserInformation(accountname);

            if (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion))
            {
                switch (newPlan.IdUserType)
                {
                    case UserTypeEnum.MONTHLY:
                        SendNotificationForUpgradePlan(accountname, userInformation, newPlan, user, promotion, promocode, discountId);
                        break;
                    case UserTypeEnum.SUBSCRIBERS:
                        SendNotificationForSuscribersPlan(accountname, userInformation, newPlan);
                        SendNotificationForUpgradePlan(accountname, userInformation, newPlan, user, promotion, promocode, discountId);
                        break;
                    case UserTypeEnum.INDIVIDUAL:
                        SendNotificationForCreditsApproved(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode);
                        break;
                    default:
                        break;
                }
            }

            if (BillingHelper.IsUpgradePending(user, promotion))
            {
                // TODO: https://makingsense.atlassian.net/browse/DAT-846
                // SENDNOTIFICATION - Doppler2017.AccountPreferencesService.cs Line: 2615
            }
        }

        private async void SendNotificationForCreditsApproved(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode)
        {
            var template = _emailSettings.Value.CreditsApprovedTemplateId[userInformation.Language ?? "en"];

            await _emailSender.SafeSendWithTemplateAsync(
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

            await _emailSender.SafeSendWithTemplateAsync(
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
        }

        private async void SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId)
        {
            var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(discountId);

            var template = _emailSettings.Value.UpgradeAccountTemplateId[userInformation.Language ?? "en"];

            await _emailSender.SafeSendWithTemplateAsync(
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

            await _emailSender.SafeSendWithTemplateAsync(
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
        }

        private async Task SendActivatedStandByEmail(string language, string fistName, int standByAmount, string sendTo)
        {
            var template = _emailSettings.Value.ActivatedStandByNotificationTemplateId[language];

            await _emailSender.SafeSendWithTemplateAsync(
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
    }
}
