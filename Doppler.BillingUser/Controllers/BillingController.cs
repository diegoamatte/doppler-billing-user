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
using Doppler.BillingUser.Extensions;
using Doppler.BillingUser.Mappers;
using Doppler.BillingUser.Mappers.BillingCredit;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Mappers.PaymentStatus;
using System.Globalization;

namespace Doppler.BillingUser.Controllers
{
    [Authorize]
    [ApiController]
    public partial class BillingController
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
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IPaymentStatusMapper _paymentStatusMapper;
        private readonly IPaymentAmountHelper _paymentAmountService;
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore
        };
        private static readonly List<UserType> AllowedPlanTypesForBilling = new List<UserType>
        {
            UserType.INDIVIDUAL,
            UserType.MONTHLY,
            UserType.SUBSCRIBERS
        };
        private static readonly List<PaymentMethodTypes> AllowedPaymentMethodsForBilling = new List<PaymentMethodTypes>
        {
            PaymentMethodTypes.CC,
            PaymentMethodTypes.TRANSF,
            PaymentMethodTypes.MP
        };

        private static readonly List<Country> AllowedCountriesForTransfer = new List<Country>
        {
            Country.Colombia,
            Country.Mexico,
            Country.Argentina
        };

        private static readonly List<UserType> AllowedUpdatePlanTypesForBilling = new List<UserType>
        {
            UserTypeEnum.MONTHLY,
            UserTypeEnum.SUBSCRIBERS,
            UserTypeEnum.INDIVIDUAL
        };

        [LoggerMessage(0, LogLevel.Debug, "Get current payment method.")]
        partial void LogDebugGetCurrentPaymentMethod();

        [LoggerMessage(1, LogLevel.Debug, "Get current plan.")]
        partial void LogDebugGetCurrentPlan();

        [LoggerMessage(2, LogLevel.Debug, "Update current payment method.")]
        partial void LogDebugUpdateCurrentPaymentMethod();

        [LoggerMessage(3, LogLevel.Error, "Failed at updating payment method for user {accountname}")]
        partial void LogErrorFailedUpdatingPaymentMethodForUser(string accountname);

        [LoggerMessage(4, LogLevel.Error, "Failed at updating payment method for user {accountname} with exception {message}" )]
        partial void LogErrorFailedUpdatingPaymentMethodWithMessage(string accountname, string message);

        [LoggerMessage(5, LogLevel.Error, "Failed at updating lead from zoho {accountname} with exception {message}")]
        partial void LogErrorFailedUpdatingLeadFromZohoWithMessage(string accountname, string message);

        [LoggerMessage(6, LogLevel.Error, "Failed at creating new agreement for user {accountname}, {message}")]
        partial void LogErrorFailedCreatingNewAgreementForUserWithMessage(string accountname, string message);

        [LoggerMessage(7, LogLevel.Error, "The paymentMethod '{paymentMethod}' does not have a mapper.")]
        partial void LogErrorPaymentMethodDontHaveMapper(PaymentMethodTypes paymentMethod);


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
            IEmailTemplatesService emailTemplatesService,
            IMercadoPagoService mercadopagoService,
            IPaymentStatusMapper paymentStatusMapper,
            IPaymentAmountHelper paymentAmountService)
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
            _mercadoPagoService = mercadopagoService;
            _paymentStatusMapper = paymentStatusMapper;
            _paymentAmountService = paymentAmountService;
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpGet("/accounts/{accountName}/billing-information")]
        public async Task<IActionResult> GetBillingInformation(string accountName)
        {
            var billingInformation = await _billingRepository.GetBillingInformation(accountName);

            return billingInformation == null ? new NotFoundResult() : new OkObjectResult(billingInformation);
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpPut("/accounts/{accountname}/billing-information")]
        public async Task<IActionResult> UpdateBillingInformation(string accountname, [FromBody] BillingInformation billingInformation)
        {
            var results = await _billingInformationValidator.ValidateAsync(billingInformation);
            if (!results.IsValid)
            {
                return new BadRequestObjectResult(results.ToString("-"));
            }

            var currentBillingInformation = await _billingRepository.GetBillingInformation(accountname);

            await _billingRepository.UpdateBillingInformation(accountname, billingInformation);

            if (currentBillingInformation != null && currentBillingInformation.Country.ToLower(CultureInfo.InvariantCulture) != billingInformation.Country.ToLower(CultureInfo.InvariantCulture))
            {
                var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

                if (currentPaymentMethod != null & currentPaymentMethod.PaymentMethodName == PaymentMethodTypes.TRANSF.ToString())
                {
                    var userInformation = await _userRepository.GetUserInformation(accountname);
                    await _billingRepository.SetEmptyPaymentMethod(userInformation.IdUser);
                }
            }

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpGet("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> GetInvoiceRecipients(string accountname)
        {
            var result = await _billingRepository.GetInvoiceRecipients(accountname);

            return result == null ? new NotFoundResult() : new OkObjectResult(result);
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpPut("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> UpdateInvoiceRecipients(string accountname, [FromBody] InvoiceRecipients invoiceRecipients)
        {
            await _billingRepository.UpdateInvoiceRecipients(accountname, invoiceRecipients.Recipients, invoiceRecipients.PlanId);

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpGet("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> GetCurrentPaymentMethod(string accountname)
        {
            LogDebugGetCurrentPaymentMethod();

            var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

            return currentPaymentMethod == null ? new NotFoundResult() : new OkObjectResult(currentPaymentMethod);
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpPut("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> UpdateCurrentPaymentMethod(string accountname, [FromBody] PaymentMethod paymentMethod)
        {
            try
            {
                LogDebugUpdateCurrentPaymentMethod();

                var userInformation = await _userRepository.GetUserInformation(accountname);
                var isSuccess = await _billingRepository.UpdateCurrentPaymentMethod(userInformation, paymentMethod);

                if (!isSuccess)
                {
                    LogErrorFailedUpdatingPaymentMethodForUser(accountname);
                    await _slackService.SendNotification($"Failed at updating payment method for user {accountname}");
                    return new BadRequestObjectResult("Failed at updating payment");
                }

                return new OkObjectResult("Successfully");
            }
            catch (DopplerApplicationException e)
            {
                LogErrorFailedUpdatingPaymentMethodWithMessage(accountname, e.Message);
                await _slackService.SendNotification($"Failed at updating payment method for user {accountname} with exception {e.Message}");
                return new BadRequestObjectResult(e.Message);
            }
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpGet("/accounts/{accountname}/plans/current")]
        public async Task<IActionResult> GetCurrentPlan(string accountname)
        {
            LogDebugGetCurrentPlan();

            var currentPlan = await _billingRepository.GetCurrentPlan(accountname);

            return currentPlan == null ? new NotFoundResult() : new OkObjectResult(currentPlan);
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpPost("/accounts/{accountname}/agreements")]
        public async Task<IActionResult> CreateAgreement([FromRoute] string accountname, [FromBody] AgreementInformation agreementInformation)
        {
            try
            {
                var results = await _agreementInformationValidator.ValidateAsync(agreementInformation);
                if (!results.IsValid)
                {
                    var messageError = $"Validation error {results.ToString("-")}";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                    return new BadRequestObjectResult(results.ToString("-"));
                }

                var user = await _userRepository.GetUserBillingInformation(accountname);
                if (user == null)
                {
                    var messageError = $"Invalid User";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                    return new NotFoundObjectResult("Invalid user");
                }

                if (!AllowedPaymentMethodsForBilling.Any(p => p == user.PaymentMethod))
                {
                    var messageError = $"Invalid payment method {user.PaymentMethod}";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                    return new BadRequestObjectResult("Invalid payment method");
                }

                if (user.PaymentMethod == PaymentMethodTypes.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == user.IdBillingCountry))
                {
                    var messageErrorTransference = $"payment method {user.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageErrorTransference);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageErrorTransference}");
                    return new BadRequestObjectResult("Invalid payment method");
                }

                var currentPlan = await _userRepository.GetUserCurrentTypePlan(user.IdUser);
                if (currentPlan != null && !AllowedUpdatePlanTypesForBilling.Any(p => p == currentPlan.IdUserType))
                {
                    var messageError = $"Invalid user type (only free users or upgrade between 'Montly' and 'Contacts' plans) {currentPlan.IdUserType}";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                    return new BadRequestObjectResult("Invalid user type (only free users or upgrade between 'Montly' plans)");
                }

                var newPlan = await _userRepository.GetUserNewTypePlan(agreementInformation.PlanId);
                if (newPlan == null)
                {
                    var messageError = $"Invalid selected plan {agreementInformation.PlanId}";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                    return new BadRequestObjectResult("Invalid selected plan");
                }

                if (!AllowedPlanTypesForBilling.Any(p => p == newPlan.IdUserType))
                {
                    var messageError = $"invalid selected plan type {newPlan.IdUserType}";
                    LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                    await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                    return new BadRequestObjectResult("Invalid selected plan type");
                }

                //TODO: Check the current error
                //var isValidTotal = await _accountPlansService.IsValidTotal(accountname, agreementInformation);
                //if (!isValidTotal)
                //{
                //    var messageError = $"Failed at creating new agreement for user {accountname}, Total of agreement is not valid";
                //    _logger.LogError(messageError);
                //    await _slackService.SendNotification(messageError);
                //    return new BadRequestObjectResult("Total of agreement is not valid");
                //}

                Promotion promotion = null;
                if (!string.IsNullOrEmpty(agreementInformation.Promocode))
                {
                    promotion = await _accountPlansService.GetValidPromotionByCode(agreementInformation.Promocode, agreementInformation.PlanId);
                }

                var invoiceId = 0;
                var authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;
                CreditCardPayment payment = null;

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    (user.PaymentMethod == PaymentMethodTypes.CC || user.PaymentMethod == PaymentMethodTypes.MP))
                {
                    encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                    if (encryptedCreditCard == null)
                    {
                        var messageError = $"missing credit card information";
                        LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, messageError);
                        await _slackService.SendNotification($"Failed at creating new agreement for user {accountname}, {messageError}");
                        return new ObjectResult("User credit card missing")
                        {
                            StatusCode = 500
                        };
                    }

                    payment = await CreateCreditCardPayment(agreementInformation.Total.Value, user.IdUser, accountname, user.PaymentMethod);

                    var accountEntyMapper = GetAccountingEntryMapper(user.PaymentMethod);
                    var invoiceEntry = await accountEntyMapper.MapToInvoiceAccountingEntry(agreementInformation.Total.Value, user, newPlan, payment);
                    AccountingEntry paymentEntry = null;
                    authorizationNumber = payment.AuthorizationNumber;

                    if (payment.Status == PaymentStatus.Approved)
                    {
                        paymentEntry = await accountEntyMapper.MapToPaymentAccountingEntry(invoiceEntry, encryptedCreditCard);
                    }

                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(invoiceEntry, paymentEntry);
                }

                var billingCreditId = 0;
                var partialBalance = 0;

                if (currentPlan == null)
                {
                    var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                    var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, BillingCreditType.UpgradeRequest);
                    billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                    user.IdCurrentBillingCredit = billingCreditId;
                    user.OriginInbound = agreementInformation.OriginInbound;
                    user.UpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);
                    user.UTCFirstPayment = !user.UpgradePending ? DateTime.UtcNow : null;
                    user.UTCUpgrade = user.UTCFirstPayment;

                    if (newPlan.IdUserType == UserType.SUBSCRIBERS && newPlan.SubscribersQty.HasValue && !user.UpgradePending)
                    {
                        user.MaxSubscribers = newPlan.SubscribersQty.Value;
                    }

                    await _userRepository.UpdateUserBillingCredit(user);

                    partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);

                    if (!user.UpgradePending)
                    {
                        if (newPlan.IdUserType != UserType.SUBSCRIBERS)
                        {
                            await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);
                        }
                        else
                        {
                            await _billingRepository.UpdateUserSubscriberLimitsAsync(user.IdUser);
                        }

                        var userInformation = await _userRepository.GetUserInformation(accountname);
                        var activatedStandByAmount = await _billingRepository.ActivateStandBySubscribers(user.IdUser);
                        if (activatedStandByAmount > 0)
                        {
                            var lang = userInformation.Language ?? "en";
                            await _emailTemplatesService.SendActivatedStandByEmail(lang, userInformation.FirstName, activatedStandByAmount, user.Email);
                        }
                    }

                    if (promotion != null)
                    {
                        await _promotionRepository.IncrementUsedTimes(promotion);
                    }

                    //Send notifications
                    SendNotifications(accountname, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditType.UpgradeRequest, currentPlan, null);
                }
                else
                {
                    if (currentPlan.IdUserType == UserType.MONTHLY && newPlan.IdUserType == UserType.MONTHLY)
                    {
                        if (currentPlan.IdUserTypePlan != newPlan.IdUserTypePlan)
                        {
                            billingCreditId = await ChangeBetweenMonthlyPlans(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                    }
                    else
                    {
                        if (currentPlan.IdUserType == UserType.SUBSCRIBERS && newPlan.IdUserType == UserType.SUBSCRIBERS)
                        {
                            billingCreditId = await ChangeBetweenSubscribersPlans(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                        else if (currentPlan.IdUserType == UserTypeEnum.INDIVIDUAL && newPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
                        {
                            billingCreditId = await BuyCredits(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                    }
                }

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    ((user.PaymentMethod == PaymentMethodTypes.CC) ||
                    (user.PaymentMethod == PaymentMethodTypes.MP) ||
                    (user.PaymentMethod == PaymentMethodTypes.TRANSF && user.IdBillingCountry == (int)Country.Argentina)))
                {
                    var billingCredit = await _billingRepository.GetBillingCredit(billingCreditId);
                    var cardNumber = user.PaymentMethod == PaymentMethodTypes.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number) : "";
                    var holderName = user.PaymentMethod == PaymentMethodTypes.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.HolderName) : "";

                    if (billingCredit != null)
                    {
                        await _sapService.SendBillingToSap(
                            BillingHelper.MapBillingToSapAsync(_sapSettings.Value,
                                cardNumber,
                                holderName,
                                billingCredit,
                                currentPlan,
                                newPlan,
                                authorizationNumber,
                                invoiceId,
                                agreementInformation.Total),
                            accountname);
                    }
                }

                var userType = currentPlan == null ? "Free user" : "Update plan";
                var message = $"Successful at creating a new agreement for: User: {accountname} - Plan: {agreementInformation.PlanId} - {userType}";
                await _slackService.SendNotification(message + (!string.IsNullOrEmpty(agreementInformation.Promocode) ? $" - Promocode {agreementInformation.Promocode}" : string.Empty));

                if (currentPlan == null)
                {
                    if (_zohoSettings.Value.UseZoho)
                    {
                        var zohoDto = new ZohoDTO()
                        {
                            Email = user.Email,
                            Doppler = newPlan.IdUserType.ToDescription(),
                            BillingSystem = user.PaymentMethod.ToString(),
                            OriginInbound = agreementInformation.OriginInbound
                        };

                        if (!user.UpgradePending)
                        {
                            zohoDto.UpgradeDate = DateTime.UtcNow;
                            zohoDto.FirstPaymentDate = DateTime.UtcNow;
                        }

                        if (promotion != null)
                        {
                            zohoDto.PromoCodo = agreementInformation.Promocode;
                            if (promotion.ExtraCredits.HasValue && promotion.ExtraCredits.Value != 0)
                            {
                                zohoDto.DiscountType = ZohoDopplerValues.Credits;
                            }
                            else if (promotion.DiscountPercentage.HasValue && promotion.DiscountPercentage.Value != 0)
                            {
                                zohoDto.DiscountType = ZohoDopplerValues.Discount;
                            }
                        }

                        try
                        {
                            await _zohoService.RefreshTokenAsync();
                            var contact = await _zohoService.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", string.Format(CultureInfo.InvariantCulture, "Email:equals:{0}", zohoDto.Email));
                            if (contact == null)
                            {
                                var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", string.Format(CultureInfo.InvariantCulture, "Email:equals:{0}", zohoDto.Email));
                                if (response != null)
                                {
                                    var lead = response.Data.FirstOrDefault();
                                    BillingHelper.MapForUpgrade(lead, zohoDto);
                                    var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityLead> { Data = new List<ZohoEntityLead> { lead } }, _settings);
                                    await _zohoService.UpdateZohoEntityAsync(body, lead.Id, "Leads");
                                }
                            }
                            else
                            {
                                if (contact.AccountName != null && !string.IsNullOrEmpty(contact.AccountName.Name))
                                {
                                    var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityAccount>>("Accounts", string.Format(CultureInfo.InvariantCulture, "Account_Name:equals:{0}", contact.AccountName.Name));
                                    if (response != null)
                                    {
                                        var account = response.Data.FirstOrDefault();
                                        BillingHelper.MapForUpgrade(account, zohoDto);
                                        var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityAccount> { Data = new List<ZohoEntityAccount> { account } }, _settings);
                                        await _zohoService.UpdateZohoEntityAsync(body, account.Id, "Accounts");
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogErrorFailedUpdatingLeadFromZohoWithMessage(accountname, e.Message);
                            await _slackService.SendNotification($"Failed at updating lead from zoho {accountname} with exception {e.Message}");
                        }
                    }
                }

                return new OkObjectResult("Successfully");
            }
            catch (Exception e)
            {
                LogErrorFailedCreatingNewAgreementForUserWithMessage(accountname, e.Message);
                await _slackService.SendNotification($"Failed at creating new agreement for user {accountname} with exception {e.Message}");
                return new ObjectResult("Failed at creating new agreement")
                {
                    StatusCode = 500,
                    Value = e.Message,
                };
            }
        }

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpPut("/accounts/{accountname}/purchase-intention")]
        public async Task<IActionResult> UpdateLastPurchaseIntentionDate(string accountname)
        {
            var result = await _userRepository.UpdateUserPurchaseIntentionDate(accountname);

            return result.Equals(0)
                ? new BadRequestObjectResult("Failed updating purchase intention. Invalid account.")
                : new OkObjectResult("Successfully");
        }

        private async void SendNotifications(
            string accountname,
            UserTypePlanInformation newPlan,
            UserBillingInformation user,
            int partialBalance,
            Promotion promotion,
            string promocode,
            int discountId,
            CreditCardPayment payment,
            BillingCreditType billingCreditType,
            UserTypePlanInformation currentPlan,
            PlanAmountDetails amountDetails)
        {
            var userInformation = await _userRepository.GetUserInformation(accountname);
            var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(discountId);
            bool isUpgradeApproved;

            switch (billingCreditType)
            {
                case BillingCreditTypeEnum.UpgradeRequest:
                    isUpgradeApproved = (user.PaymentMethod == PaymentMethod.CC || !BillingHelper.IsUpgradePending(user, promotion, payment));

                    if (newPlan.IdUserType == UserType.INDIVIDUAL)
                    {
                        await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved);
                    }
                    else
                    {
                        if (isUpgradeApproved && newPlan.IdUserType == UserType.SUBSCRIBERS)
                        {
                            await _emailTemplatesService.SendNotificationForSuscribersPlan(accountname, userInformation, newPlan);
                        }

                        await _emailTemplatesService.SendNotificationForUpgradePlan(accountname, userInformation, newPlan, user, promotion, promocode, discountId, planDiscountInformation, !isUpgradeApproved);
                    }

                    return;
                case BillingCreditType.UpgradeBetweenMonthlies:
                case BillingCreditType.UpgradeBetweenSubscribers:
                    await _emailTemplatesService.SendNotificationForUpdatePlan(accountname, userInformation, currentPlan, newPlan, user, promotion, promocode, discountId, planDiscountInformation, amountDetails);
                    return;
                case BillingCreditTypeEnum.Credit_Buyed_CC:
                case BillingCreditTypeEnum.Credit_Request:
                    isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion, payment));
                    await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved);
                    return;
                default:
                    return;
            }
        }

        private async Task<CreditCardPayment> CreateCreditCardPayment(decimal total, int userId, string accountname, PaymentMethodTypes paymentMethod)
        {
            var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);

            switch (paymentMethod)
            {
                case PaymentMethodTypes.CC:
                    var authorizationNumber = await _paymentGateway.CreateCreditCardPayment(total, encryptedCreditCard, userId);
                    return new CreditCardPayment { Status = PaymentStatus.Approved, AuthorizationNumber = authorizationNumber };
                case PaymentMethodTypes.MP:
                    var paymentDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyType.UsS, CurrencyType.sARG, total);
                    var mercadoPagoPayment = await _mercadoPagoService.CreatePayment(accountname, userId, paymentDetails.Total, encryptedCreditCard);
                    return new CreditCardPayment { Status = _paymentStatusMapper.MapToPaymentStatus(mercadoPagoPayment.Status), AuthorizationNumber = mercadoPagoPayment.Id.ToString(CultureInfo.InvariantCulture) };
                default:
                    return new CreditCardPayment { Status = PaymentStatus.Approved };
            }
        }

        private IAccountingEntryMapper GetAccountingEntryMapper(PaymentMethodTypes paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethodTypes.CC:
                    return new AccountingEntryForCreditCardMapper();
                case PaymentMethodTypes.MP:
                    return new AccountingEntryForMercadopagoMapper(_paymentAmountService);
                default:
                    LogErrorPaymentMethodDontHaveMapper(paymentMethod);
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private IBillingCreditMapper GetBillingCreditMapper(PaymentMethodTypes paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethodTypes.CC:
                    return new BillingCreditForCreditCardMapper(_billingRepository, _encryptionService);
                case PaymentMethodTypes.MP:
                    return new BillingCreditForMercadopagoMapper(_billingRepository, _encryptionService, _paymentAmountService);
                case PaymentMethodTypes.TRANSF:
                    return new BillingCreditForTransferMapper(_billingRepository);
                default:
                    LogErrorPaymentMethodDontHaveMapper(paymentMethod);
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private async Task<int> ChangeBetweenMonthlyPlans(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            if (currentPlan.EmailQty < newPlan.EmailQty)
            {
                var amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);

                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit);
                if (currentBillingCredit != null)
                {
                    promotion = await _promotionRepository.GetById(currentBillingCredit.IdPromotion ?? 0);
                    if (promotion != null)
                    {
                        var timesAppliedPromocode = await _promotionRepository.GetHowManyTimesApplyedPromocode(promotion.Code, user.Email);
                        if (promotion.Duration == timesAppliedPromocode.CountApplied)
                        {
                            promotion = null;
                        }
                    }
                }

                var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, BillingCreditType.UpgradeBetweenMonthlies);
                billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;

                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                /* Update the user */
                user.IdCurrentBillingCredit = billingCreditId;
                user.OriginInbound = agreementInformation.OriginInbound;
                user.UpgradePending = false;
                user.UTCUpgrade = DateTime.UtcNow;

                await _userRepository.UpdateUserBillingCredit(user);

                var partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                await _billingRepository.CreateMovementBalanceAdjustmentAsync(user.IdUser, 0, UserType.MONTHLY, UserType.MONTHLY);
                var currentMonthlyAddedEmailsWithBilling = await _userRepository.GetCurrentMonthlyAddedEmailsWithBillingAsync(user.IdUser);

                await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan, currentMonthlyAddedEmailsWithBilling);

                if (promotion != null)
                {
                    await _promotionRepository.IncrementUsedTimes(promotion);
                }

                //Send notifications
                SendNotifications(user.Email, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditType.UpgradeBetweenMonthlies, currentPlan, amountDetails);

                return billingCreditId;
            }

            return 0;
        }

        private async Task<int> ChangeBetweenSubscribersPlans(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            if (currentPlan.SubscribersQty < newPlan.SubscribersQty)
            {
                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit);
                var amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);

                if (currentBillingCredit != null)
                {
                    promotion = await _promotionRepository.GetById(currentBillingCredit.IdPromotion ?? 0);
                    if (promotion != null)
                    {
                        var timesAppliedPromocode = await _promotionRepository.GetHowManyTimesApplyedPromocode(promotion.Code, user.Email);
                        if (promotion.Duration == timesAppliedPromocode.CountApplied)
                        {
                            promotion = null;
                        }
                    }
                }

                var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, BillingCreditType.UpgradeBetweenSubscribers);
                billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;

                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);


                /* Update the user */
                user.IdCurrentBillingCredit = billingCreditId;
                user.OriginInbound = agreementInformation.OriginInbound;
                user.UpgradePending = false;
                user.UTCUpgrade = DateTime.UtcNow;
                user.MaxSubscribers = newPlan.SubscribersQty.Value;

                await _userRepository.UpdateUserBillingCredit(user);

                if (promotion != null)
                {
                    await _promotionRepository.IncrementUsedTimes(promotion);
                }

                //Send notifications
                SendNotifications(user.Email, newPlan, user, 0, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditType.UpgradeBetweenSubscribers, currentPlan, amountDetails);

                //Activate StandBy Subscribers
                var userInformation = await _userRepository.GetUserInformation(user.Email);

                await _billingRepository.UpdateUserSubscriberLimitsAsync(user.IdUser);
                var activatedStandByAmount = await _billingRepository.ActivateStandBySubscribers(user.IdUser);
                if (activatedStandByAmount > 0)
                {
                    var lang = userInformation.Language ?? "en";
                    await _emailTemplatesService.SendActivatedStandByEmail(lang, userInformation.FirstName, activatedStandByAmount, user.Email);
                }

                return billingCreditId;
            }

            return 0;
        }

        private async Task<int> BuyCredits(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit);
            var billingCreditType = user.PaymentMethod == PaymentMethodEnum.CC ? BillingCreditTypeEnum.Credit_Buyed_CC : BillingCreditTypeEnum.Credit_Request;
            var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
            var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, billingCreditType);
            billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;

            var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

            user.IdCurrentBillingCredit = billingCreditId;
            user.OriginInbound = agreementInformation.OriginInbound;
            user.UpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);
            user.UTCUpgrade = !user.UpgradePending ? DateTime.UtcNow : null;

            await _userRepository.UpdateUserBillingCredit(user);

            var partialBalance = 0;

            if (!user.UpgradePending)
            {
                partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);
            }

            if (promotion != null)
            {
                await _promotionRepository.IncrementUsedTimes(promotion);
            }

            //Send notifications
            SendNotifications(user.Email, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, billingCreditType, currentPlan, null);


            return billingCreditId;
        }
    }
}
