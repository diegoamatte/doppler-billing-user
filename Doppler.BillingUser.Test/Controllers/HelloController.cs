using Doppler.BillingUser.DopplerSecurity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Test.Controllers
{
    [Authorize]
    [ApiController]
    public class HelloController
    {
        [AllowAnonymous]
        [HttpGet("/hello/anonymous")]
        public string GetForAnonymous() => "Hello anonymous!";

        [HttpGet("/hello/valid-token")]
        public string GetForValidToken() => "Hello! you have a valid token!";

        [Authorize(Policies.OnlySuperser)]
        [HttpGet("/hello/superuser")]
        public string GetForSuperUserToken() => "Hello! you have a valid SuperUser token!";

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpGet("/accounts/{accountId:int:min(0)}/hello")]
        public string GetForAccountById(int accountId) => $"Hello! \"you\" that have access to the account with ID '{accountId}'";

        [Authorize(Policies.OwnResourceOrSuperUser)]
        [HttpGet("/accounts/{accountname}/hello")]
        public string GetForAccountByName(string accountname) => $"Hello! \"you\" that have access to the account with accountname '{accountname}'";
    }
}
