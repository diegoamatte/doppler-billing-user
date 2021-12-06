using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class AccountPlansServiceTest
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IOptions<AccountPlansSettings>> _accountPlansSettingsMock;

        public AccountPlansServiceTest()
        {
            _accountPlansSettingsMock = new Mock<IOptions<AccountPlansSettings>>();
            _accountPlansSettingsMock.Setup(x => x.Value)
                .Returns(new AccountPlansSettings
                {
                    BaseUrl = "https://localhost:5000"
                });
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [Fact]
        public async Task Get_account_plans_total_return_true_when_total_amount_is_equal_that_current_total_agreement()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };

            var accountname = "test@mail.com";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"Total\":2}")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var service = new AccountPlansService(
                Mock.Of<IUsersApiTokenGetter>(),
                _accountPlansSettingsMock.Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                _httpClientFactoryMock.Object);

            // Act
            var isValid = await service.IsValidTotal(accountname, agreement);
            var uri = $"https://localhost:5000/{accountname}/newplan/{agreement.PlanId}/calculate?discountId={agreement.DiscountId}";

            // Assert
            Assert.True(isValid);
            _httpMessageHandlerMock.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri(uri)),
            ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task Get_account_plans_total_return_false_when_total_amount_is_not_equal_that_current_total_agreement()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };

            var accountname = "test@mail.com";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"Total\":10}")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var service = new AccountPlansService(
                Mock.Of<IUsersApiTokenGetter>(),
                _accountPlansSettingsMock.Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                _httpClientFactoryMock.Object);

            // Act
            var isValid = await service.IsValidTotal(accountname, agreement);
            var uri = $"https://localhost:5000/{accountname}/newplan/{agreement.PlanId}/calculate?discountId={agreement.DiscountId}";

            // Assert
            Assert.False(isValid);
            _httpMessageHandlerMock.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri(uri)),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
