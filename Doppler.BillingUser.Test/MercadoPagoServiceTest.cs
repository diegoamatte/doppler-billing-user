using System.Net.Http;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class MercadoPagoServiceTest
    {
        private readonly string _accountname;
        private readonly int _userId;
        private readonly int _paymentId;
        private readonly decimal _total;
        private readonly CreditCard _creditCard;
        private readonly PaymentRequestDto _paymentRequestDto;
        private readonly Mock<IEncryptionService> _encryptionServiceMock;
        private readonly Mock<IOptions<MercadoPagoSettings>> _mercadoPagoSettingsMock;

        public MercadoPagoServiceTest()
        {
            _accountname = "test@example.com";
            _userId = 1;
            _paymentId = 1;
            _total = 100;

            _creditCard = new CreditCard()
            {
                CardType = CardType.Mastercard,
                ExpirationMonth = 12,
                ExpirationYear = 2023,
                HolderName = "EncryptedHolderName",
                Number = "EncryptedNumber",
                Code = "EncryptedCode",
                IdentificationNumber = "11222333",
                IdentificationType = "DNI"
            };

            _paymentRequestDto = new PaymentRequestDto
            {
                TransactionAmount = 100,
                Installments = 1,
                TransactionDescription = "Doppler Email Marketing",
                Description = "MERPAGO*DOPPLER",
                PaymentMethodId = "master",
                Card = new CardDto
                {
                    Cardholder = new PaymentCardholder
                    {
                        Identification = new Identification
                        {
                            Number = "11222333",
                            Type = "DNI"
                        },
                        Name = "HolderName"
                    },
                    CardNumber = "Number",
                    SecurityCode = "Code",
                    ExpirationMonth = "12",
                    ExpirationYear = "2023"
                }
            };

            _encryptionServiceMock = new Mock<IEncryptionService>();
            _encryptionServiceMock.Setup(es => es.DecryptAES256("EncryptedHolderName")).Returns("HolderName");
            _encryptionServiceMock.Setup(es => es.DecryptAES256("EncryptedNumber")).Returns("Number");
            _encryptionServiceMock.Setup(es => es.DecryptAES256("EncryptedCode")).Returns("Code");

            _mercadoPagoSettingsMock = new Mock<IOptions<MercadoPagoSettings>>();
            _mercadoPagoSettingsMock.Setup(x => x.Value)
                .Returns(new MercadoPagoSettings
                {
                    MercadoPagoApiUrlTemplate = "http://localhost:5000/doppler-mercadopago/accounts/{accountname}/payment/{id}"
                });
        }

        [Fact]
        public async void Get_payment_by_id_returns_payment_when_mercadopagoapi_response_is_successful()
        {
            // Arrange
            var expectedUrl = $"http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/{_paymentId}";

            var service = new MercadoPagoService(
                _mercadoPagoSettingsMock.Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Id = 1,
                Status = MercadoPagoPaymentStatus.Approved,
                StatusDetail = "Accredited",
            });

            // Act
            var payment = await service.GetPaymentById(_paymentId, _accountname);

            // Assert
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithVerb(HttpMethod.Get);
            Assert.IsType<MercadoPagoPayment>(payment);
        }

        [Fact]
        public async void Get_payment_by_id_throws_exception_when_mercadopagoapi_response_is_unsuccessful()
        {
            // Arrange
            var service = new MercadoPagoService(
                _mercadoPagoSettingsMock.Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IEmailTemplatesService>());

            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWith(status: 500);

            // Assert
            await Assert.ThrowsAsync<FlurlHttpException>(async () => await service.GetPaymentById(_paymentId, _accountname));
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.InProcess)]
        public async void Create_payment_returns_payment_when_status_is_in_process_or_approved(MercadoPagoPaymentStatus mercadoPagoPaymentStatus)
        {
            // Arrange
            var expectedUrl = "http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/";

            var service = new MercadoPagoService(
                _mercadoPagoSettingsMock.Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                _encryptionServiceMock.Object,
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Status = mercadoPagoPaymentStatus
            });

            // Act
            var paymentCreated = await service.CreatePayment(_accountname, _userId, _total, _creditCard);

            // Assert
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithRequestJson(_paymentRequestDto)
                .WithVerb(HttpMethod.Post);
            Assert.IsType<MercadoPagoPayment>(paymentCreated);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatus.Rejected)]
        [InlineData(MercadoPagoPaymentStatus.Refunded)]
        [InlineData(MercadoPagoPaymentStatus.Cancelled)]
        [InlineData(MercadoPagoPaymentStatus.ChargedBack)]
        public async void Create_payment_throws_doppler_application_exception_with_error_code_DeclinedPaymentTransaction_when_payment_status_is_not_in_process_or_approved(MercadoPagoPaymentStatus mercadoPagoPaymentStatus)
        {
            // Arrange
            var expectedUrl = "http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/";

            var service = new MercadoPagoService(
                _mercadoPagoSettingsMock.Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                _encryptionServiceMock.Object,
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Status = mercadoPagoPaymentStatus
            });

            // Act
            async Task CallFunc()
            {
                await service.CreatePayment(_accountname, _userId, _total, _creditCard);
            }

            // Assert
            var caughtException = await Assert.ThrowsAsync<DopplerApplicationException>(CallFunc);
            Assert.Equal(PaymentErrorCode.DeclinedPaymentTransaction, caughtException.ErrorCode);
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithRequestJson(_paymentRequestDto)
                .WithVerb(HttpMethod.Post);
        }

        [Fact]
        public async void Create_payment_throws_doppler_application_exception_with_error_code_ClientPaymentTransactionError_when_mercadopagoapi_response_is_unsuccessful()
        {
            var expectedUrl = "http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/";

            var service = new MercadoPagoService(
                _mercadoPagoSettingsMock.Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWith(status: 500);

            // Act
            async Task CallFunc()
            {
                await service.CreatePayment(_accountname, _userId, _total, _creditCard);
            }

            // Assert
            var caughtException = await Assert.ThrowsAsync<DopplerApplicationException>(CallFunc);
            Assert.Equal(PaymentErrorCode.ClientPaymentTransactionError, caughtException.ErrorCode);
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithVerb(HttpMethod.Post);
        }
    }
}
