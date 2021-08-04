using Doppler.BillingUser.Model;
using Doppler.BillingUser.Validators;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class BillingInformationValidatorTest
    {
        private readonly BillingInformationValidator _validator;

        public BillingInformationValidatorTest()
        {
            _validator = new BillingInformationValidator();
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_return_error_when_firstname_is_invalid(string firstname)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = firstname,
                Lastname = "lastname",
                Address = "address",
                City = "city",
                Country = "country",
                Phone = "23123",
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.False(_validator.Validate(billingInformation).IsValid);
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_return_error_when_lastname_is_invalid(string lastname)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = lastname,
                Address = "address",
                City = "city",
                Country = "country",
                Phone = "23123",
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.False(_validator.Validate(billingInformation).IsValid);
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_return_error_when_address_is_invalid(string address)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = "lastname",
                Address = address,
                City = "city",
                Country = "country",
                Phone = "23123",
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.False(_validator.Validate(billingInformation).IsValid);
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_return_error_when_city_is_invalid(string city)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = "lastname",
                Address = "address",
                City = city,
                Country = "country",
                Phone = "23123",
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.False(_validator.Validate(billingInformation).IsValid);
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_return_error_when_country_is_invalid(string country)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = "lastname",
                Address = "address",
                City = "city",
                Country = country,
                Phone = "23123",
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.False(_validator.Validate(billingInformation).IsValid);
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_return_error_when_phone_is_invalid(string phone)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = "lastname",
                Address = "address",
                City = "city",
                Country = "country",
                Phone = phone,
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.False(_validator.Validate(billingInformation).IsValid);
        }

        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("")]
        [Theory]
        public void Validate_billing_information_should_not_return_error_when_zipcode_is_empty(string zipcode)
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = "lastname",
                Address = "address",
                City = "city",
                Country = "country",
                Phone = "23123",
                Province = "New York",
                ZipCode = zipcode
            };

            Assert.True(_validator.Validate(billingInformation).IsValid);
        }

        [Fact]
        public void Validate_billing_information_should_return_is_valid_when_data_are_correctly()
        {
            var billingInformation = new BillingInformation
            {
                Firstname = "firstname",
                Lastname = "lastname",
                Address = "address",
                City = "city",
                Country = "country",
                Phone = "23123",
                Province = "New York",
                ZipCode = "7688"
            };

            Assert.True(_validator.Validate(billingInformation).IsValid);
        }
    }
}
