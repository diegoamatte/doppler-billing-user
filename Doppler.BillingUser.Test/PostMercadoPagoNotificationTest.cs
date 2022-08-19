using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PostMercadoPagoNotificationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly Mock<IBillingRepository> _billingRepository;
        private readonly Mock<IUserRepository> _userRepository;
        private readonly Mock<IMercadoPagoService> _mercadopagoService;
        private readonly MercadoPagoNotification _notification;
        private readonly HttpClient _client;

        public PostMercadoPagoNotificationTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _billingRepository = new Mock<IBillingRepository>();
            _userRepository = new Mock<IUserRepository>();
            _mercadopagoService = new Mock<IMercadoPagoService>();

            _notification = new MercadoPagoNotification
            {
                Action = "payment.updated",
                Data = new Data { Id = 1 },
            };
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(_mercadopagoService.Object);
                    services.AddSingleton(_userRepository.Object);
                    services.AddSingleton(_billingRepository.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_returns_internal_server_error_when_payment_not_found()
        {
            // Arrange
            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Model.AccountingEntry());
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_returns_notfound_when_user_is_null()
        {
            // Arrange
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Model.AccountingEntry());

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_returns_notfound_when_invoice_is_null()
        {
            // Arrange
            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatus.Rejected, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.Rejected, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.Refunded, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.Refunded, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.ChargedBack, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.ChargedBack, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.Cancelled, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.Cancelled, PaymentStatus.Pending)]
        public async Task Update_mercadopago_payment_status_calls_update_invoice_status_once_when_mp_payment_is_not_successful(MercadoPagoPaymentStatus mpStatus, PaymentStatus invoiceStatus)
        {
            // Arrange
            var invoice = new Model.AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = mpStatus });

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatus.DeclinedPaymentTransaction), Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatus.Approved, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.Approved, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.Authorized, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.Authorized, PaymentStatus.DeclinedPaymentTransaction)]
        public async Task Update_mercadopago_payment_calls_update_invoice_and_create_payment_when_payment_is_approved(MercadoPagoPaymentStatus paymentStatus, PaymentStatus invoiceStatus)
        {
            // Arrange
            var invoice = new AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _userRepository.Setup(ur => ur.GetEncryptedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(new ExternalServices.FirstData.CreditCard());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = paymentStatus });

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatus.Approved), Times.Once);
            _billingRepository.Verify(br => br.CreatePaymentEntryAsync(invoice.IdAccountingEntry, It.IsAny<AccountingEntry>()), Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatus.Approved, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.Pending, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.Pending, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.Pending, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.Authorized, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.InProcess, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.InProcess, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.InProcess, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.InMediation, PaymentStatus.Approved)]
        [InlineData(MercadoPagoPaymentStatus.InMediation, PaymentStatus.Pending)]
        [InlineData(MercadoPagoPaymentStatus.InMediation, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.Rejected, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.Cancelled, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.Refunded, PaymentStatus.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatus.ChargedBack, PaymentStatus.DeclinedPaymentTransaction)]
        public async Task Update_mercadopago_payment_does_not_updates_invoice_status_or_create_payment_when_mp_payment_and_invoice_are_approved(MercadoPagoPaymentStatus paymentStatus, PaymentStatus invoiceStatus)
        {
            var invoice = new AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };
            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = paymentStatus });

            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatus>()), Times.Never);
            _billingRepository.Verify(br => br.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("payment.created")]
        [InlineData("application.deauthorized")]
        [InlineData("application.authorized")]
        [InlineData("subscription_preapproval.created")]
        [InlineData("updated")]
        [InlineData("state_FINISHED")]
        [InlineData("state_CANCELED")]
        [InlineData("state_ERROR")]
        [InlineData("shipment.updated")]

        public async Task Update_mercadopago_payment_does_not_call_any_method_when_notification_action_is_not_payment_updated(string action)
        {
            // Arrange
            _notification.Action = action;
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _userRepository.Verify(ur => ur.GetUserInformation(It.IsAny<string>()), Times.Never);
            _billingRepository.Verify(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _mercadopagoService.Verify(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatus>()), Times.Never);
            _billingRepository.Verify(br => br.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

    }
}
