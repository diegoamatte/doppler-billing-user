using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class GetCurrentPaymentMethodTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TokenAccount123Test1AtTestDotComExpire20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.E3RHjKx9p0a-64RN2YPtlEMysGM45QBO9eATLBhtP4tUQNZnkraUr56hAWA-FuGmhiuMptnKNk_dU3VnbyL6SbHrMWUbquxWjyoqsd7stFs1K_nW6XIzsTjh8Bg6hB5hmsSV-M5_hPS24JwJaCdMQeWrh6cIEp2Sjft7I1V4HQrgzrkMh15sDFAw3i1_ZZasQsDYKyYbO9Jp7lx42ognPrz_KuvPzLjEXvBBNTFsVXUE-ur5adLNMvt-uXzcJ1rcwhjHWItUf5YvgRQbbBnd9f-LsJIhfkDgCJcvZmGDZrtlCKaU1UjHv5c3faZED-cjL59MbibofhPjv87MK8hhdg";
        private const string TokenAccount123Test1AtTestDotComExpire20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.JBmiZBgKVSUtB4_NhD1kiUhBTnH2ufGSzcoCwC3-Gtx0QDvkFjy2KbxIU9asscenSdzziTOZN6IfFx6KgZ3_a3YB7vdCgfSINQwrAK0_6Owa-BQuNAIsKk-pNoIhJ-OcckV-zrp5wWai3Ak5Qzg3aZ1NKZQKZt5ICZmsFZcWu_4pzS-xsGPcj5gSr3Iybt61iBnetrkrEbjtVZg-3xzKr0nmMMqe-qqeknozIFy2YWAObmTkrN4sZ3AB_jzqyFPXN-nMw3a0NxIdJyetbESAOcNnPLymBKZEZmX2psKuXwJxxekvgK9egkfv2EjKYF9atpH5XwC0Pd4EWvraLAL2eg";
        private readonly WebApplicationFactory<Startup> _factory;

        public GetCurrentPaymentMethodTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TokenAccount123Test1AtTestDotComExpire20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task GET_current_payment_method_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@test.com/payment-methods/current")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GET_current_payment_method_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@test.com/payment-methods/current");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        [Fact]
        public async Task GET_current_payment_method_should_get_right_values_when_username_is_valid()
        {
            // Arrange
            const string expectedContent = "{\"ccHolderFullName\":\"TEST\",\"ccNumber\":\"TEST\",\"ccVerification\":\"****\",\"ccExpMonth\":\"2\",\"ccExpYear\":\"2130\",\"ccType\":\"Visa\",\"paymentMethodName\":\"CC\",\"renewalMonth\":\"6\",\"cuit\":\"39404521\",\"razonSocial\":\"Company\",\"idConsumerType\":\"9\",\"ccIdentificationType\":\"DNI\",\"ccIdentificationNumber\":\"344444\"}";
            var paymentMethod = new PaymentMethod
            {
                PaymentMethodName = "CC",
                CCNumber = "41111111",
                CCExpMonth = "2",
                CCExpYear = "2130",
                CCHolderFullName = "Test holder Name",
                CCIdentificationNumber = "344444",
                CCIdentificationType = "DNI",
                CCType = "Visa",
                CCVerification = "213",
                Cuit = "39404521",
                IdConsumerType = "9",
                RazonSocial = "Company",
                RenewalMonth = "6"
            };

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<PaymentMethod>(null, null, null, null, null))
                .ReturnsAsync(paymentMethod);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@test.com/payment-methods/current")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TokenAccount123Test1AtTestDotComExpire20330518}"
                    }
                }
            };

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expectedContent, responseContent);
        }

    }
}
