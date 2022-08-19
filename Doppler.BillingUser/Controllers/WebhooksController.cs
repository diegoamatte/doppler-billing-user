using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Mappers;
using Doppler.BillingUser.Mappers.PaymentStatus;
using Doppler.BillingUser.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Controllers
{
    [ApiController]
    public partial class WebhooksController : ControllerBase
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IPaymentAmountHelper _paymentAmountService;
        private readonly ILogger<WebhooksController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IPaymentStatusMapper _paymentStatusMapper;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly string _paymentUpdated = "payment.updated";

        [LoggerMessage(0, LogLevel.Error, "Invoice with authorization number: {authorizationNumber} was not found.")]
        partial void LogErrorAuthorizationNotFound(long authorizationNumber);

        [LoggerMessage(1, LogLevel.Error, "The payment associated to the invoiceId {invoiceId} was rejected. Reason: {reason}")]
        partial void LogErrorPaymentRejectedWithReason(int invoiceId, string reason);

        public WebhooksController(
            IPaymentAmountHelper paymentAmountService,
            ILogger<WebhooksController> logger,
            IBillingRepository billingRepository,
            IUserRepository userRepository,
            IMercadoPagoService mercadoPagoService,
            IPaymentStatusMapper paymentStatusMapper,
            IEmailTemplatesService emailTemplatesService)
        {
            _billingRepository = billingRepository;
            _paymentAmountService = paymentAmountService;
            _logger = logger;
            _userRepository = userRepository;
            _mercadoPagoService = mercadoPagoService;
            _paymentStatusMapper = paymentStatusMapper;
            _emailTemplatesService = emailTemplatesService;
        }

        [HttpPost("/accounts/{accountname}/integration/mercadopagonotification")]
        public async Task<IActionResult> UpdateMercadoPagoPaymentStatusAsync([FromRoute] string accountname, [FromBody] MercadoPagoNotification notification)
        {
            if (notification.Action != _paymentUpdated)
            {
                return new OkObjectResult("Successful");
            }

            var user = await _userRepository.GetUserInformation(accountname);
            if (user is null)
            {
                return new NotFoundObjectResult("Account not found");
            }

            var invoice = await _billingRepository.GetInvoice(user.IdUser, notification.Data.Id.ToString(CultureInfo.InvariantCulture));
            if (invoice is null)
            {
                LogErrorAuthorizationNotFound(notification.Data.Id);
                return new NotFoundObjectResult("Invoice not found");
            }

            var payment = await _mercadoPagoService.GetPaymentById(notification.Data.Id, accountname);

            var status = _paymentStatusMapper.MapToPaymentStatus(payment.Status);
            if (status == PaymentStatus.Pending && invoice.Status == PaymentStatus.Approved)
            {
                return new OkObjectResult("Successful");
            }

            if (status == PaymentStatus.DeclinedPaymentTransaction && invoice.Status != PaymentStatus.DeclinedPaymentTransaction)
            {
                if (invoice.Status == PaymentStatus.Approved)
                {
                    LogErrorPaymentRejectedWithReason(invoice.IdAccountingEntry, payment.StatusDetail);
                }

                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, status);
                return new OkObjectResult("Successful");
            }

            if (status == PaymentStatus.Approved && invoice.Status != PaymentStatus.Approved)
            {
                var accountingEntryMapper = new AccountingEntryForMercadopagoMapper(_paymentAmountService);
                var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                var paymentEntry = await accountingEntryMapper.MapToPaymentAccountingEntry(invoice, encryptedCreditCard);
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatus.Approved);
                await _billingRepository.CreatePaymentEntryAsync(invoice.IdAccountingEntry, paymentEntry);

                await _emailTemplatesService.SendNotificationForMercadoPagoPaymentApproved(user.IdUser, user.Email);
            }

            return new OkObjectResult("Successful");
        }

    }
}
