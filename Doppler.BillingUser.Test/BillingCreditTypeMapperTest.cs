using Doppler.AccountPlans.Utils;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class BillingCreditTypeMapperTest
    {
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_a_free_user_buy_credits()
        {
            // Arrange
            var user = new User()
            {
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Free_to_Individual, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_a_free_user_buy_monthly_plan()
        {
            // Arrange
            var user = new User()
            {
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.MONTHLY
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Free_to_Monthly, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_a_free_user_buy_subscribers_plan()
        {
            // Arrange
            var user = new User()
            {
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.SUBSCRIBERS
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Free_to_Subscribers, result);
        }

        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_prepaid_user_buy_credits()
        {
            // Arrange
            var user = new User()
            {
                PaymentMethod = (int)PaymentMethodEnum.CC,
                CurrentUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL
                },
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Credit_Buyed_CC, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_prepaid_user_buy_monthly_plan()
        {
            // Arrange
            var user = new User()
            {
                CurrentUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL
                },
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.MONTHLY
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Individual_to_Monthly, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_prepaid_user_buy_subscribers_plan()
        {
            // Arrange
            var user = new User()
            {
                CurrentUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL
                },

                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.SUBSCRIBERS
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert

            Assert.Equal((int)BillingCreditTypeEnum.Individual_to_Subscribers, result);
        }

        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_user_already_has_monthly_contact_plan_buy_a_higher_plan()
        {
            // Arrange
            var user = new User()
            {
                CurrentUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.MONTHLY
                },
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.MONTHLY
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Upgrade_Between_Monthlies, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_user_already_has_subscribers_plan_buy_a_higher_plan()
        {
            // Arrange
            var user = new User()
            {
                CurrentUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.SUBSCRIBERS
                },
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.SUBSCRIBERS
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Upgrade_Between_Subscribers, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_billing_credit_type_when_user_already_has_subscribers_plan_buy_a_monthly_contact_plan()
        {
            // Arrange
            var user = new User()
            {
                CurrentUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.SUBSCRIBERS
                },
                NewUserTypePlan = new UserTypePlan()
                {
                    IdUserType = (int)UserTypeEnum.MONTHLY
                }
            };

            // Act
            var result = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)BillingCreditTypeEnum.Subscribers_to_Monthly, result);
        }
    }
}
