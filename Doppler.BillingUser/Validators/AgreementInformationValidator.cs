using FluentValidation;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Validators
{
    public class AgreementInformationValidator : AbstractValidator<AgreementInformation>
    {
        public AgreementInformationValidator()
        {
            RuleFor(x => x.PlanId)
                .NotEmpty()
                .GreaterThan(0);
        }
    }
}
