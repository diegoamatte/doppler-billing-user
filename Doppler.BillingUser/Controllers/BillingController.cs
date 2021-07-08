using Doppler.BillingUser.DopplerSecurity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doppler.BillingUser.Controllers
{
    [Authorize]
    [ApiController]
    public class BillingController
    {
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/currentbillinginformation")]
        public string GetCurrentBillingInformation(string accountname)
        {
            return $"Hello! \"you\" that have access to GetCurrentBillingInformation with accountname '{accountname}'";
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billinginformation")]
        public string UpdateBillingInformation(string accountname)
        {
            return $"Hello! \"you\" that have access to UpdateBillingInformation with accountname '{accountname}'";
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/current-payment-method")]
        public string GetCurrentPaymentMethod(string accountname)
        {
            return $"Hello! \"you\" that have access to GetCurrentPaymentMethod with accountname '{accountname}'";
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPost("/accounts/{accountname}/billing")]
        public string TryBilling(string accountname)
        {
            return $"Hello! \"you\" that have access to TryBilling with accountname '{accountname}'";
        }
    }
}
