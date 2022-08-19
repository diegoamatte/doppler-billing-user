using Doppler.BillingUser.Test.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Moq.Dapper;

namespace Doppler.BillingUser.Test
{
    public class ExternalControllersFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) => feature.Controllers.Add(typeof(HelloController).GetTypeInfo());
    }
    public class AuthorizationTest
        : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _output;

        public AuthorizationTest(WebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            factory = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var partManager = (ApplicationPartManager)services
                        .Last(descriptor => descriptor.ServiceType == typeof(ApplicationPartManager))
                        .ImplementationInstance;

                    partManager.FeatureProviders.Add(new ExternalControllersFeatureProvider());
                });
            });
            _factory = factory;
            _output = output;
        }

        [Theory]
        [InlineData("/hello/anonymous", HttpStatusCode.OK)]
        public async Task GET_helloAnonymous_should_not_require_token(string url, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData("/hello/anonymous", TestUsersData.Token_Empty, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Expire2096_10_02, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Expire2001_09_08, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Broken, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Superuser_Expire2096_10_02, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Superuser_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Superuser_Expire2001_09_08, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_SuperuserFalse_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2096_10_02, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/anonymous", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2001_09_08, HttpStatusCode.OK)]
        public async Task GET_helloAnonymous_should_accept_any_token(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData("/hello/valid-token", HttpStatusCode.Unauthorized)]
        [InlineData("/hello/superuser", HttpStatusCode.Unauthorized)]
        [InlineData("/accounts/test1@example.com/billing-information", HttpStatusCode.Unauthorized)]
        [InlineData("/accounts/test1@example.com/payment-methods/current", HttpStatusCode.Unauthorized)]
        public async Task GET_authenticated_endpoints_should_require_token(string url, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            // Act
            var response = await client.GetAsync(url);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        }

        [Theory]
        [InlineData("/hello/valid-token", TestUsersData.Token_Empty, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Broken, HttpStatusCode.Unauthorized, "invalid_token", "")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Superuser_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Superuser_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/valid-token", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/hello/superuser", TestUsersData.Token_Empty, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/superuser", TestUsersData.Token_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/superuser", TestUsersData.Token_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/hello/superuser", TestUsersData.Token_Broken, HttpStatusCode.Unauthorized, "invalid_token", "")]
        [InlineData("/hello/superuser", TestUsersData.Token_Superuser_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/superuser", TestUsersData.Token_Superuser_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/hello/superuser", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/hello/superuser", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Empty, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Broken, HttpStatusCode.Unauthorized, "invalid_token", "")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Superuser_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Superuser_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/billing-information", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Empty, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Broken, HttpStatusCode.Unauthorized, "invalid_token", "")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Superuser_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Superuser_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2096_10_02, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token has no expiration\"")]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2001_09_08, HttpStatusCode.Unauthorized, "invalid_token", "error_description=\"The token expired at")]
        public async Task GET_authenticated_endpoints_should_require_a_valid_token(string url, string token, HttpStatusCode expectedStatusCode, string error, string extraErrorInfo)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.StartsWith("Bearer", response.Headers.WwwAuthenticate.ToString());
            Assert.Contains($"error=\"{error}\"", response.Headers.WwwAuthenticate.ToString());
            Assert.Contains(extraErrorInfo, response.Headers.WwwAuthenticate.ToString());
        }

        [Theory]
        [InlineData("/hello/valid-token", TestUsersData.Token_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/valid-token", TestUsersData.Token_Superuser_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/valid-token", TestUsersData.Token_SuperuserFalse_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/hello/valid-token", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.OK)]
        public async Task GET_helloValidToken_should_accept_valid_token(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData("/hello/superuser", TestUsersData.Token_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/hello/superuser", TestUsersData.Token_SuperuserFalse_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/hello/superuser", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.Forbidden)]
        public async Task GET_helloSuperUser_should_require_a_valid_token_with_isSU_flag(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData("/hello/superuser", TestUsersData.Token_Superuser_Expire2033_05_18, HttpStatusCode.OK)]
        public async Task GET_helloSuperUser_should_accept_valid_token_with_isSU_flag(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData("/accounts/123/hello", TestUsersData.Token_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/123/hello", TestUsersData.Token_SuperuserFalse_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/456/hello", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/test1@example.com/hello", TestUsersData.Token_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/test1@example.com/hello", TestUsersData.Token_SuperuserFalse_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/test2@example.com/hello", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_SuperuserFalse_Expire2033_05_18, HttpStatusCode.Forbidden)]
        [InlineData("/accounts/test2@example.com/payment-methods/current", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.Forbidden)]
        public async Task GET_account_endpoint_should_require_a_valid_token_with_isSU_flag_or_a_token_for_the_right_account(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData("/accounts/123/hello", TestUsersData.Token_Superuser_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/accounts/123/hello", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/accounts/test1@example.com/hello", TestUsersData.Token_Superuser_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/accounts/test1@example.com/hello", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Superuser_Expire2033_05_18, HttpStatusCode.OK)]
        [InlineData("/accounts/test1@example.com/payment-methods/current", TestUsersData.Token_Account_123_test1AtExampleDotCom_Expire2033_05_18, HttpStatusCode.OK)]
        public async Task GET_account_endpoint_should_accept_valid_token_with_isSU_flag_or_a_token_for_the_right_account(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<PaymentMethod>(null, null, null, null, null))
                .ReturnsAsync(new PaymentMethod
                {
                    CCHolderFullName = "Holder test"
                });

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

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }
    }
}
