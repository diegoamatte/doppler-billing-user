using FluentValidation;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Validators
{
    public class BillingInformationValidator : AbstractValidator<BillingInformation>
    {
        public BillingInformationValidator()
        {
            RuleFor(x => x.Firstname)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Lastname)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Address)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.City)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Province)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Country)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.ZipCode)
                .MaximumLength(10);

            RuleFor(x => x.Phone)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
