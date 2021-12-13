using System.Data.Common;
using System.Net;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class GetCurrentPlanTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.E3RHjKx9p0a-64RN2YPtlEMysGM45QBO9eATLBhtP4tUQNZnkraUr56hAWA-FuGmhiuMptnKNk_dU3VnbyL6SbHrMWUbquxWjyoqsd7stFs1K_nW6XIzsTjh8Bg6hB5hmsSV-M5_hPS24JwJaCdMQeWrh6cIEp2Sjft7I1V4HQrgzrkMh15sDFAw3i1_ZZasQsDYKyYbO9Jp7lx42ognPrz_KuvPzLjEXvBBNTFsVXUE-ur5adLNMvt-uXzcJ1rcwhjHWItUf5YvgRQbbBnd9f-LsJIhfkDgCJcvZmGDZrtlCKaU1UjHv5c3faZED-cjL59MbibofhPjv87MK8hhdg";
        private readonly WebApplicationFactory<Startup> _factory;

        public GetCurrentPlanTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_current_plan_return_not_found_when_account_does_not_have_plan_associated()
        {
            // Arrange
            var mockConnection = new Mock<DbConnection>();
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>()))
                .ReturnsAsync(null as CurrentPlan);
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.GetAsync("accounts/test1@test.com/plans/current");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_current_plan_return_ok_when_account_has_valid_current_plan()
        {
            // Arrange
            var mockConnection = new Mock<DbConnection>();
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>()))
                .ReturnsAsync(new CurrentPlan
                {
                    IdPlan = 1
                });
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.GetAsync("accounts/test1@test.com/plans/current");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_current_plan_return_ok_when_account_has_data_valid_of_current_plan()
        {
            // Arrange
            var expectedContent = "{\"idPlan\":1,\"planSubscription\":0,\"planType\":\"individual\",\"remainingCredits\":0,\"emailQty\":10,\"subscribersQty\":null}";
            var mockConnection = new Mock<DbConnection>();
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>()))
                .ReturnsAsync(new CurrentPlan
                {
                    IdPlan = 1,
                    PlanType = "individual",
                    EmailQty = 10
                });
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.GetAsync("accounts/test1@test.com/plans/current");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expectedContent, responseContent);
        }
    }
}
