using System;
using System.Globalization;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public partial class MercadoPagoService : IMercadoPagoService
    {
        private readonly IOptions<MercadoPagoSettings> _options;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger<MercadoPagoService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private const string TransactionDescription = "Doppler Email Marketing";
        private const string Description = "MERPAGO*DOPPLER";
        private const string Master = "master";

        [LoggerMessage(0, LogLevel.Error, "Error to get payment for user: {accountname} with payment ID: {id}")]
        partial void LogErrorGetPaymentForUserWithPaymentId(string accountname, long id);

        [LoggerMessage(1, LogLevel.Error, "Mercadopago payment Declined with Accountname:{accountname}, ErrorCode:{errorCode}, ErrorMessage: {errorMessage}")]
        partial void LogErrorPaymentDeclinedWithMessage(string accountname, PaymentErrorCode errorCode, string errorMessage);

        [LoggerMessage(2, LogLevel.Error, "Unexpected error")]
        partial void LogErrorException(Exception ex);

        public MercadoPagoService(
            IOptions<MercadoPagoSettings> options,
            IJwtTokenGenerator jwtTokenGenerator,
            IFlurlClientFactory flurlClientFactory,
            ILogger<MercadoPagoService> logger,
            IEncryptionService encryptionService,
            IEmailTemplatesService emailTemplatesService)
        {
            _options = options;
            _jwtTokenGenerator = jwtTokenGenerator;
            _flurlClient = flurlClientFactory.Get(_options.Value.MercadoPagoApiUrlTemplate);
            _logger = logger;
            _encryptionService = encryptionService;
            _emailTemplatesService = emailTemplatesService;
        }

        public async Task<MercadoPagoPayment> GetPaymentById(long id, string accountname)
        {
            try
            {
                var payment = await _flurlClient.Request(new UriTemplate(_options.Value.MercadoPagoApiUrlTemplate)
                    .AddParameter("accountname", accountname)
                    .AddParameter("id", id)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                    .GetJsonAsync<MercadoPagoPayment>();
                return payment;
            }
            catch (Exception)
            {
                LogErrorGetPaymentForUserWithPaymentId(accountname, id);
                throw;
            }
        }

        public async Task<MercadoPagoPayment> CreatePayment(string accountname, int clientId, decimal total, CreditCard creditCard)
        {
            try
            {
                var paymentRequestDto = CreatePaymentRequestDto(total, creditCard);
                var payment = await PostMercadoPagoPayment(accountname, paymentRequestDto);

                if (payment.Status is MercadoPagoPaymentStatus.Rejected or
                    MercadoPagoPaymentStatus.Cancelled or
                    MercadoPagoPaymentStatus.Refunded or
                    MercadoPagoPaymentStatus.ChargedBack)
                {
                    var errorCode = PaymentErrorCode.DeclinedPaymentTransaction;
                    var errorMessage = payment.StatusDetail;

                    LogErrorPaymentDeclinedWithMessage(accountname, errorCode, errorMessage);
                    await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(clientId, errorCode.ToString(), errorMessage, string.Empty, string.Empty, PaymentMethodTypes.MP);

                    throw new DopplerApplicationException(errorCode, errorMessage);
                }

                if (payment.Status == MercadoPagoPaymentStatus.InProcess)
                {
                    var errorCode = payment.Status.ToString();
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "payment is in process, MercadopagoStatus: {0}, MercadopagoStatusDetail:{1}", payment.Status, payment.StatusDetail);
                    await _emailTemplatesService.SendNotificationForMercadoPagoPaymentInProcess(clientId, accountname, errorCode, errorMessage);
                }

                return payment;
            }
            catch (Exception ex) when (ex is not DopplerApplicationException)
            {
                LogErrorException(ex);
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        private async Task<MercadoPagoPayment> PostMercadoPagoPayment(string accountname, PaymentRequestDto paymentRequestDto)
        {
            var payment = await _flurlClient.Request(new UriTemplate(_options.Value.MercadoPagoApiUrlTemplate)
                .AddParameter("accountname", accountname)
                .Resolve())
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PostJsonAsync(paymentRequestDto)
                .ReceiveJson<MercadoPagoPayment>();

            return payment;
        }

        private PaymentRequestDto CreatePaymentRequestDto(decimal total, CreditCard creditCard)
        {
            var paymentRequestDto = new PaymentRequestDto
            {
                TransactionAmount = total,
                Installments = 1,
                TransactionDescription = TransactionDescription,
                Description = Description,
                PaymentMethodId = creditCard.CardType == CardType.Mastercard ? Master : creditCard.CardType.ToString().ToLower(CultureInfo.InvariantCulture),
                Card = new CardDto
                {
                    Cardholder = new PaymentCardholder
                    {
                        Identification = new Identification
                        {
                            Number = creditCard.IdentificationNumber,
                            Type = creditCard.IdentificationType
                        },
                        Name = _encryptionService.DecryptAES256(creditCard.HolderName)
                    },
                    CardNumber = _encryptionService.DecryptAES256(creditCard.Number),
                    SecurityCode = _encryptionService.DecryptAES256(creditCard.Code),
                    ExpirationMonth = creditCard.ExpirationMonth.ToString(CultureInfo.InvariantCulture),
                    ExpirationYear = creditCard.ExpirationYear.ToString(CultureInfo.InvariantCulture)
                }
            };

            return paymentRequestDto;
        }
    }
}
