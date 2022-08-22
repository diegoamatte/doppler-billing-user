using Doppler.BillingUser.Model;
using FluentValidation;

namespace Doppler.BillingUser.Validators
{
    public class AgreementInformationValidator : AbstractValidator<AgreementInformation>
    {
        public AgreementInformationValidator()
        {
            RuleFor(x => x.PlanId)
                .NotEmpty()
                .GreaterThan(0);

            RuleFor(x => x.Total)
                .NotEmpty();
        }
    }
}
