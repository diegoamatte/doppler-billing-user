using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PostAgreementTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.E3RHjKx9p0a-64RN2YPtlEMysGM45QBO9eATLBhtP4tUQNZnkraUr56hAWA-FuGmhiuMptnKNk_dU3VnbyL6SbHrMWUbquxWjyoqsd7stFs1K_nW6XIzsTjh8Bg6hB5hmsSV-M5_hPS24JwJaCdMQeWrh6cIEp2Sjft7I1V4HQrgzrkMh15sDFAw3i1_ZZasQsDYKyYbO9Jp7lx42ognPrz_KuvPzLjEXvBBNTFsVXUE-ur5adLNMvt-uXzcJ1rcwhjHWItUf5YvgRQbbBnd9f-LsJIhfkDgCJcvZmGDZrtlCKaU1UjHv5c3faZED-cjL59MbibofhPjv87MK8hhdg";
        private readonly WebApplicationFactory<Startup> _factory;

        public PostAgreementTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_agreement_return_ForbidResult_when_total_amount_validation_is_false()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                DiscountId = 2,
                PlanId = 3
            };
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(false);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.PostAsJsonAsync("accounts/test1@test.com/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_agreement_return_Ok_when_total_amount_validation_is_true()
        {
            // Arrange
            var agreement = new
            {
                Total = 10
            };
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.PostAsJsonAsync("accounts/test1@test.com/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
