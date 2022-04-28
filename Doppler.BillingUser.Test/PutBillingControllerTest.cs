using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using Newtonsoft.Json;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PutBillingControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TokenAccount123Test1AtTestDotComExpire20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.E3RHjKx9p0a-64RN2YPtlEMysGM45QBO9eATLBhtP4tUQNZnkraUr56hAWA-FuGmhiuMptnKNk_dU3VnbyL6SbHrMWUbquxWjyoqsd7stFs1K_nW6XIzsTjh8Bg6hB5hmsSV-M5_hPS24JwJaCdMQeWrh6cIEp2Sjft7I1V4HQrgzrkMh15sDFAw3i1_ZZasQsDYKyYbO9Jp7lx42ognPrz_KuvPzLjEXvBBNTFsVXUE-ur5adLNMvt-uXzcJ1rcwhjHWItUf5YvgRQbbBnd9f-LsJIhfkDgCJcvZmGDZrtlCKaU1UjHv5c3faZED-cjL59MbibofhPjv87MK8hhdg";
        private const string TokenAccount123Test1AtTestDotComExpire20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.JBmiZBgKVSUtB4_NhD1kiUhBTnH2ufGSzcoCwC3-Gtx0QDvkFjy2KbxIU9asscenSdzziTOZN6IfFx6KgZ3_a3YB7vdCgfSINQwrAK0_6Owa-BQuNAIsKk-pNoIhJ-OcckV-zrp5wWai3Ak5Qzg3aZ1NKZQKZt5ICZmsFZcWu_4pzS-xsGPcj5gSr3Iybt61iBnetrkrEbjtVZg-3xzKr0nmMMqe-qqeknozIFy2YWAObmTkrN4sZ3AB_jzqyFPXN-nMw3a0NxIdJyetbESAOcNnPLymBKZEZmX2psKuXwJxxekvgK9egkfv2EjKYF9atpH5XwC0Pd4EWvraLAL2eg";
        private readonly WebApplicationFactory<Startup> _factory;

        public PutBillingControllerTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TokenAccount123Test1AtTestDotComExpire20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task PUT_Billing_information_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@test.com/billing-information")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_billing_information_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@test.com/billing-information");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_billing_information_should_update_right_value_based_on_body_information()
        {
            // Arrange
            const int expectedRows = 1;

            var contactInformation = new BillingInformation
            {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test ProvinceAh pe",
                ZipCode = "ZipCode",
                Country = "Test Country"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(contactInformation), Encoding.UTF8, "application/json");

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingInformation("test1@test.com"))
                .ReturnsAsync(contactInformation);

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@test.com/billing-information")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TokenAccount123Test1AtTestDotComExpire20330518}"
                    }
                },
                Content = requestContent
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PUT_billing_information_with_different_countrye_should_update_right_value_based_on_body_information_and_also_update_payment_method()
        {
            // Arrange
            const int expectedRows = 1;

            var contactInformation = new BillingInformation
            {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test ProvinceAh pe",
                ZipCode = "ZipCode",
                Country = "Test Country"
            };

            var currentBillingInformation = new BillingInformation
            {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test ProvinceAh pe",
                ZipCode = "ZipCode",
                Country = "co"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(contactInformation), Encoding.UTF8, "application/json");

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingInformation("test1@test.com"))
                .ReturnsAsync(currentBillingInformation);
            billingRepositoryMock.Setup(x => x.GetCurrentPaymentMethod("test1@test.com"))
                .ReturnsAsync(new PaymentMethod { PaymentMethodName = PaymentMethodEnum.TRANSF.ToString() });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es",
                IdUser = 1
            });

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@test.com/billing-information")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TokenAccount123Test1AtTestDotComExpire20330518}"
                    }
                },
                Content = requestContent
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
