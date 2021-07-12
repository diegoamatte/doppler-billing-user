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
        [HttpGet("/accounts/{accountname}/billing-information")]
        public string GetBillingInformation(string accountname)
        {
            return $"Hello! \"you\" that have access to GetCurrentBillingInformation with accountname '{accountname}'";
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information")]
        public string UpdateBillingInformation(string accountname)
        {
            return $"Hello! \"you\" that have access to UpdateBillingInformation with accountname '{accountname}'";
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/payment-methods/current")]
        public string GetCurrentPaymentMethod(string accountname)
        {
            return $"Hello! \"you\" that have access to GetCurrentPaymentMethod with accountname '{accountname}'";
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPost("/accounts/{accountname}/upgrade")]
        public string Upgrade(string accountname)
        {
            return $"Hello! \"you\" that have access to Upgrade with accountname '{accountname}'";
        }
    }
}
