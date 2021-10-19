using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.Sap;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class SapServiceTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IOptions<SapSettings>> _sapSettingsMock;

        public SapServiceTest()
        {
            _sapSettingsMock = new Mock<IOptions<SapSettings>>();
            _sapSettingsMock.Setup(x => x.Value)
                .Returns(new SapSettings
                {
                    SapBaseUrl = "https://localhost:5000/",
                    SapCreateBusinessPartnerEndpoint = "businesspartner/createorupdatebusinesspartner"
                });

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [Fact]
        public async Task Send_user_to_sap_should_send_right_values()
        {
            // Arrange
            var businessPartner = new SapBusinessPartner
            {
                Id = 1,
                Email = "test1@test.com"
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("")
                });

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            var service = new SapService(
                Mock.Of<IJwtTokenGenerator>(),
                _sapSettingsMock.Object,
                Mock.Of<ILogger<SapService>>(),
                _httpClientFactoryMock.Object);

            //Act
            await service.SendUserDataToSap(businessPartner, It.IsAny<string>());
            var uri = "https://localhost:5000/businesspartner/createorupdatebusinesspartner";

            // Assert
            _httpMessageHandlerMock.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == new Uri(uri)
                    && JsonConvert.DeserializeObject<SapBusinessPartner>(req.Content.ReadAsStringAsync().Result).Email == "test1@test.com"
                    && JsonConvert.DeserializeObject<SapBusinessPartner>(req.Content.ReadAsStringAsync().Result).Id == 1),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
