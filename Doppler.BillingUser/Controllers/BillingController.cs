using Doppler.BillingUser.DopplerSecurity;
using Doppler.BillingUser.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Controllers
{
    [Authorize]
    [ApiController]
    public class BillingController
    {
        private readonly ILogger _logger;
        private readonly BillingRepository _billingRepository;

        public BillingController(ILogger<BillingController> logger, BillingRepository billingRepository)
        {
            _logger = logger;
            _billingRepository = billingRepository;
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountName}/billing-information")]
        public async Task<IActionResult> GetBillingInformation(string accountName)
        {
            var billingInformation = await _billingRepository.GetBillingInformation(accountName);

            if (billingInformation == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(billingInformation);
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
        [HttpPost("/accounts/{accountname}/agreements")]
        public string CreateAgreement(string accountname)
        {
            return $"Hello! \"you\" that have access to Upgrade with accountname '{accountname}'";
        }
    }
}
