using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System;
using Flurl.Http.Testing;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Microsoft.Extensions.Options;
using Doppler.BillingUser.ExternalServices.EmailSender;
using System.Collections.Generic;
using System.Threading;
using Doppler.BillingUser.ExternalServices.Zoho;
using Microsoft.Extensions.Logging;
using Doppler.BillingUser.ExternalServices.Zoho.API;

namespace Doppler.BillingUser.Test
{
    public class PostAgreementTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.E3RHjKx9p0a-64RN2YPtlEMysGM45QBO9eATLBhtP4tUQNZnkraUr56hAWA-FuGmhiuMptnKNk_dU3VnbyL6SbHrMWUbquxWjyoqsd7stFs1K_nW6XIzsTjh8Bg6hB5hmsSV-M5_hPS24JwJaCdMQeWrh6cIEp2Sjft7I1V4HQrgzrkMh15sDFAw3i1_ZZasQsDYKyYbO9Jp7lx42ognPrz_KuvPzLjEXvBBNTFsVXUE-ur5adLNMvt-uXzcJ1rcwhjHWItUf5YvgRQbbBnd9f-LsJIhfkDgCJcvZmGDZrtlCKaU1UjHv5c3faZED-cjL59MbibofhPjv87MK8hhdg";
        private const string TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUB0ZXN0LmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.JBmiZBgKVSUtB4_NhD1kiUhBTnH2ufGSzcoCwC3-Gtx0QDvkFjy2KbxIU9asscenSdzziTOZN6IfFx6KgZ3_a3YB7vdCgfSINQwrAK0_6Owa-BQuNAIsKk-pNoIhJ-OcckV-zrp5wWai3Ak5Qzg3aZ1NKZQKZt5ICZmsFZcWu_4pzS-xsGPcj5gSr3Iybt61iBnetrkrEbjtVZg-3xzKr0nmMMqe-qqeknozIFy2YWAObmTkrN4sZ3AB_jzqyFPXN-nMw3a0NxIdJyetbESAOcNnPLymBKZEZmX2psKuXwJxxekvgK9egkfv2EjKYF9atpH5XwC0Pd4EWvraLAL2eg";
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

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(false);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_Ok_when_total_amount_validation_is_true_and_planId_is_defined()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                PlanId = 2
            };

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var authorizatioNumber = "LLLTD222";
            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task POST_agreement_should_return_unauthorized_When_token_is_invalid(string token)
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
        public async Task POST_agreement_should_return_unauthorized_when_authorization_is_empty()
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
        public async Task POST_agreement_should_return_bad_request_when_body_is_empty()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());


            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(new { }));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_ok_when_planId_is_a_valid_prepaid_plan_and_user_exists_and_have_cc_as_payment_method_and_total_is_not_empty_and_has_valid_cc_and_first_datapayment_is_made_and_billing_credit_is_created_and_send_zoho_update()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var authorizatioNumber = "LLLTD222";
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(
                    It.IsAny<AgreementInformation>(),
                    It.IsAny<UserBillingInformation>(),
                    It.IsAny<UserTypePlanInformation>(),
                    It.IsAny<Promotion>()))
                .ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>())).ReturnsAsync(movementCreditId);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var zohoServiceMock = new Mock<IZohoService>();
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", It.IsAny<string>()));
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", It.IsAny<string>()))
                .ReturnsAsync(
                new ZohoResponse<ZohoEntityLead>()
                {
                    Data = new List<ZohoEntityLead>()
                    {
                        new ZohoEntityLead()
                    }
                });

            zohoServiceMock.Setup(x => x.UpdateZohoEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ZohoUpdateResponse()
                {
                    Data = new List<ZohoUpdateResponseItem>()
                }
            );

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(GetZohoServiceSettingsMock().Object);
                    services.AddSingleton(zohoServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_not_found_when_user_not_exists()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(null as UserBillingInformation);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_payment_method_is_not_cc()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>()))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.MP
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_is_not_a_free_user()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_total_is_not_present_in_payload()
        {
            // Arrange
            var planId = 1;

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(new { planId }));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_ok_when_total_is_zero()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 0
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(
                    It.IsAny<AgreementInformation>(),
                    It.IsAny<UserBillingInformation>(),
                    It.IsAny<UserTypePlanInformation>(),
                    It.IsAny<Promotion>()))
                .ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>())).ReturnsAsync(movementCreditId);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_credit_card_not_exists()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(null as CreditCard);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_internal_server_error_when_user_first_data_payment_fails()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>())).ThrowsAsync(new Exception());

            var accountServiceMock = new Mock<IAccountPlansService>();
            accountServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(accountServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_internal_server_error_when_store_payment_records_on_db_fails()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>())).ThrowsAsync(new Exception());

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(0);

            var accountServiceMock = new Mock<IAccountPlansService>();
            accountServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(accountServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_new_plan_type_is_not_individual()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.MONTHLY
            });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_new_plan_type_is_not_valid()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@test.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_ok_when_total_is_zero_and_accounting_records_are_not_created_in_db()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 0
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(
                    It.IsAny<AgreementInformation>(),
                    It.IsAny<UserBillingInformation>(),
                    It.IsAny<UserTypePlanInformation>(),
                    It.IsAny<Promotion>()))
                .ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>())).ReturnsAsync(movementCreditId);

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            billingRepositoryMock.Verify(ms => ms.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async void POST_agreement_should_return_ok_when_promocode_is_null()
        {
            // Arrange
            const string accountName = "test1@test.com";
            var agreement = new
            {
                planId = 1,
                total = 2
            };
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(new CreditCard());
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(
                    It.IsAny<decimal>(),
                    It.IsAny<CreditCard>(),
                    It.IsAny<int>()))
                .ReturnsAsync("authorizatioNumber");

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);
            var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { Total = 2 });

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async void POST_agreement_should_return_internal_server_error_when_promocode_is_invalid()
        {
            // Arrange
            const string accountName = "test1@test.com";
            var agreement = new
            {
                planId = 1,
                total = 2,
                promocode = "promocode-test"
            };
            var billingRepositoryMock = new Mock<IBillingRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);
            var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { Total = 2 });
            httpTest.RespondWith(status: 500);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async void POST_agreement_should_return_ok_when_promocode_is_valid()
        {
            // Arrange
            const string accountName = "test1@test.com";
            var agreement = new
            {
                planId = 1,
                total = 0,
                promocode = "promocode-test"
            };
            var billingRepositoryMock = new Mock<IBillingRepository>();
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode("promocode-test", 1))
                .ReturnsAsync(new Promotion
                {
                    IdPromotion = 1
                });
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IPromotionRepository>());
                    services.AddSingleton(emailSenderMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async void POST_agreement_should_increment_times_to_use_of_promocode_when_agreement_is_accomplished()
        {
            // Arrange
            const string accountName = "test1@test.com";
            var agreement = new
            {
                planId = 1,
                total = 0,
                promocode = "promocode-test"
            };
            var billingRepositoryMock = new Mock<IBillingRepository>();
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode("promocode-test", 1))
                .ReturnsAsync(new Promotion
                {
                    IdPromotion = 1
                });
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var promotionRepositoryMock = new Mock<IPromotionRepository>();
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(promotionRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            promotionRepositoryMock.Verify(x => x.IncrementUsedTimes(It.IsAny<Promotion>()), Times.Once());
        }

        [Fact]
        public async Task POST_agreement_information_should_notify_in_slack_when_first_data_payment_fails()
        {
            // Arrange
            var user = new UserBillingInformation
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var creditCard = new CreditCard
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>()))
                .ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>()))
                .ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(creditCard);

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception());

            var accountServiceMock = new Mock<IAccountPlansService>();
            accountServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(accountServiceMock.Object);
                    services.AddSingleton(GetSlackSettingsMock().Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);
            var httpTest = new HttpTest();

            // Act
            var response = await client.PostAsJsonAsync("accounts/test1@test.com/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            httpTest.ShouldHaveCalled("https://hooks.slack.com/services/test")
                .WithVerb(HttpMethod.Post);
        }

        [Fact]
        public async Task POST_agreement_information_should_notify_to_slack_when_zoho_update_fails()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@test.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var authorizatioNumber = "LLLTD222";
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AgreementInformation>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(
                    It.IsAny<AgreementInformation>(),
                    It.IsAny<UserBillingInformation>(),
                    It.IsAny<UserTypePlanInformation>(),
                    It.IsAny<Promotion>()))
                .ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>())).ReturnsAsync(movementCreditId);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var zohoServiceMock = new Mock<IZohoService>();
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", It.IsAny<string>()));
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", It.IsAny<string>()))
                .ReturnsAsync(
                new ZohoResponse<ZohoEntityLead>()
                {
                    Data = new List<ZohoEntityLead>()
                    {
                        new ZohoEntityLead()
                    }
                });

            zohoServiceMock.Setup(x => x.UpdateZohoEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(GetSlackSettingsMock().Object);
                    services.AddSingleton(GetZohoServiceSettingsMock().Object);
                    services.AddSingleton(zohoServiceMock.Object);
                });
            });

            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT123_TEST1_AT_TEST_DOT_COM_EXPIRE20330518);
            var httpTest = new HttpTest();

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            httpTest.ShouldHaveCalled("https://hooks.slack.com/services/test")
                .WithVerb(HttpMethod.Post);
        }

        private static Mock<IOptions<SlackSettings>> GetSlackSettingsMock()
        {
            var slackSettingsMock = new Mock<IOptions<SlackSettings>>();
            slackSettingsMock.Setup(x => x.Value)
                .Returns(new SlackSettings
                {
                    Url = "https://hooks.slack.com/services/test"
                });

            return slackSettingsMock;
        }

        private static Mock<IOptions<ZohoSettings>> GetZohoServiceSettingsMock()
        {
            var zohoSettingsMock = new Mock<IOptions<ZohoSettings>>();
            zohoSettingsMock.Setup(x => x.Value)
                .Returns(new ZohoSettings
                {
                    UseZoho = true
                });

            return zohoSettingsMock;
        }
    }
}
