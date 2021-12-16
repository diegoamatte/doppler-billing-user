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
using Microsoft.Extensions.Options;

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
        private readonly ISapService _sapService;
        private readonly IEncryptionService _encryptionService;
        private readonly IOptions<SapSettings> _sapSettings;

        private const int CurrencyTypeUsd = 0;

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
            IOptions<SapSettings> sapSettings)
        {
            _logger = logger;
            _billingRepository = billingRepository;
            _userRepository = userRepository;
            _billingInformationValidator = billingInformationValidator;
            _agreementInformationValidator = agreementInformationValidator;
            _accountPlansService = accountPlansService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
            _encryptionService = encryptionService;
            _sapSettings = sapSettings;
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
            var results = await _agreementInformationValidator.ValidateAsync(agreementInformation);
            if (!results.IsValid)
            {
                return new BadRequestObjectResult(results.ToString("-"));
            }

            var user = await _userRepository.GetUserBillingInformation(accountname);
            if (user == null)
            {
                return new NotFoundObjectResult("Invalid user");
            }

            if (user.PaymentMethod != PaymentMethodEnum.CC)
            {
                return new BadRequestObjectResult("Invalid payment method");
            }

            var isValidTotal = await _accountPlansService.IsValidTotal(accountname, agreementInformation);

            if (!isValidTotal)
            {
                return new BadRequestObjectResult("Total of agreement is not valid");
            }

            var currentPlan = await _userRepository.GetUserCurrentTypePlan(user.IdUser);
            if (currentPlan != null)
            {
                return new BadRequestObjectResult("Invalid user type (only free users)");
            }

            var newPlan = await _userRepository.GetUserNewTypePlan(agreementInformation.PlanId);
            if (newPlan == null)
            {
                return new BadRequestObjectResult("Invalid selected plan");
            }

            if (newPlan.IdUserType != UserTypeEnum.INDIVIDUAL)
            {
                return new BadRequestObjectResult("Invalid selected plan type");
            }

            Promotion promotion = null;
            if (!string.IsNullOrEmpty(agreementInformation.Promocode))
            {
                promotion = await _accountPlansService.GetValidPromotionByCode(agreementInformation.Promocode, agreementInformation.PlanId);
            }

            int invoiceId = 0;
            string authorizationNumber = string.Empty;
            CreditCard encryptedCreditCard = null;
            if (agreementInformation.Total.GetValueOrDefault() > 0)
            {
                encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                if (encryptedCreditCard == null)
                {
                    return new ObjectResult("User credit card missing")
                    {
                        StatusCode = 500
                    };
                }

                // TODO: Deal with first data exceptions.
                authorizationNumber = await _paymentGateway.CreateCreditCardPayment(agreementInformation.Total.GetValueOrDefault(), encryptedCreditCard, user.IdUser);
                invoiceId = await _billingRepository.CreateAccountingEntriesAsync(agreementInformation, encryptedCreditCard, user.IdUser, authorizationNumber);
            }

            var billingCreditId = await _billingRepository.CreateBillingCreditAsync(agreementInformation, user, newPlan, promotion);

            user.IdCurrentBillingCredit = billingCreditId;
            await _userRepository.UpdateUserBillingCredit(user);

            var partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
            await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);

            if (agreementInformation.Total.GetValueOrDefault() > 0)
            {
                await _sapService.SendBillingToSap(
                    await MapBillingToSapAsync(encryptedCreditCard, currentPlan, newPlan, authorizationNumber, invoiceId, billingCreditId),
                    accountname);
            }

            // TODO: SEND NOTIFICATIONS

            return new OkObjectResult("Successfully");
        }

        private async Task<SapBillingDto> MapBillingToSapAsync(CreditCard creditCard, UserTypePlanInformation currentUserPlan, UserTypePlanInformation newUserPlan, string authorizationNumber, int invoidId, int billingCreditId)
        {
            var billingCredit = await _billingRepository.GetBillingCredit(billingCreditId);
            var cardNumber = _encryptionService.DecryptAES256(creditCard.Number);

            var sapBilling = new SapBillingDto
            {
                Id = billingCredit.IdUser,
                CreditsOrSubscribersQuantity = billingCredit.CreditsQty.GetValueOrDefault(),
                IsCustomPlan = new[] { 0, 9, 17 }.Contains(billingCredit.IdUserTypePlan),
                IsPlanUpgrade = true, // TODO: Check when the other types of purchases are implemented.
                Currency = CurrencyTypeUsd,
                Periodicity = null,
                PeriodMonth = billingCredit.Date.Month,
                PeriodYear = billingCredit.Date.Year,
                PlanFee = billingCredit.PlanFee,
                Discount = billingCredit.DiscountPlanFee,
                ExtraEmailsPeriodMonth = billingCredit.Date.Month,
                ExtraEmailsPeriodYear = billingCredit.Date.Year,
                ExtraEmailsFee = 0,
                IsFirstPurchase = currentUserPlan == null,
                PlanType = billingCredit.IdUserTypePlan,
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
    }
}
