using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Encryption;
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
    public class PostBillingControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TokenAccount123Test1AtTestDotComExpire20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.E3RHjKx9p0a-64RN2YPtlEMysGM45QBO9eATLBhtP4tUQNZnkraUr56hAWA-FuGmhiuMptnKNk_dU3VnbyL6SbHrMWUbquxWjyoqsd7stFs1K_nW6XIzsTjh8Bg6hB5hmsSV-M5_hPS24JwJaCdMQeWrh6cIEp2Sjft7I1V4HQrgzrkMh15sDFAw3i1_ZZasQsDYKyYbO9Jp7lx42ognPrz_KuvPzLjEXvBBNTFsVXUE-ur5adLNMvt-uXzcJ1rcwhjHWItUf5YvgRQbbBnd9f-LsJIhfkDgCJcvZmGDZrtlCKaU1UjHv5c3faZED-cjL59MbibofhPjv87MK8hhdg";
        private const string TokenAccount123Test1AtTestDotComExpire20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.JBmiZBgKVSUtB4_NhD1kiUhBTnH2ufGSzcoCwC3-Gtx0QDvkFjy2KbxIU9asscenSdzziTOZN6IfFx6KgZ3_a3YB7vdCgfSINQwrAK0_6Owa-BQuNAIsKk-pNoIhJ-OcckV-zrp5wWai3Ak5Qzg3aZ1NKZQKZt5ICZmsFZcWu_4pzS-xsGPcj5gSr3Iybt61iBnetrkrEbjtVZg-3xzKr0nmMMqe-qqeknozIFy2YWAObmTkrN4sZ3AB_jzqyFPXN-nMw3a0NxIdJyetbESAOcNnPLymBKZEZmX2psKuXwJxxekvgK9egkfv2EjKYF9atpH5XwC0Pd4EWvraLAL2eg";
        private readonly WebApplicationFactory<Startup> _factory;

        public PostBillingControllerTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TokenAccount123Test1AtTestDotComExpire20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task POST_Billing_information_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "accounts/test1@test.com/agreements")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_billing_information_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Post, "accounts/test1@test.com/agreements");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_billing_information_should_update_right_value_based_on_body_information()
        {
            var agreementInformation = new AgreementInformation
            {
                PlanId = 1,
                Total = 15,
                DiscountId = 2
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(agreementInformation), Encoding.UTF8, "application/json");

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var createAgreementMock = new Mock<IBillingRepository>();
            createAgreementMock.Setup(x => x.CreateAgreement(It.IsAny<string>(), It.IsAny<AgreementInformation>())).ReturnsAsync(true);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(createAgreementMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Post, "accounts/test1@test.com/agreements")
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
