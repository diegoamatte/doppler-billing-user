using Doppler.AccountPlans.Utils;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class SourceTypeMapperTest
    {
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_a_free_user_buy_credits()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.BUY_CREDITS_ID, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_a_free_user_buy_monthly_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.UPGRADE_ID, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_a_free_user_buy_subscribers_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.UPGRADE_ID, result);
        }

        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_prepaid_user_buy_credits()
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
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL
                }
            };

            // Act
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.BUY_CREDITS_ID, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_prepaid_user_buy_monthly_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            // the new plan always be greater than previous, that because is an upselling
            Assert.Equal((int)SourceTypeEnum.UPSELLING_ID, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_prepaid_user_buy_subscribers_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert

            Assert.Equal((int)SourceTypeEnum.UPSELLING_ID, result);
        }

        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_user_already_has_monthly_contact_plan_buy_a_higher_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.UPSELLING_ID, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_user_already_has_subscribers_plan_buy_a_higher_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.UPSELLING_ID, result);
        }
        [Fact]
        public void SourceTypeEnumMapper_method_should_return_correct_value_for_source_type_when_user_already_has_subscribers_plan_buy_a_monthly_contact_plan()
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
            var result = SourceTypeHelper.SourceTypeEnumMapper(user);

            // Assert
            Assert.Equal((int)SourceTypeEnum.UPSELLING_ID, result);
        }
    }
}
