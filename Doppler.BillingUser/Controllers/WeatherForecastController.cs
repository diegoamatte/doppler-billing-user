using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doppler.BillingUser.Weather;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Doppler.BillingUser.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly WeatherForecastService _service;

        public WeatherForecastController(WeatherForecastService service)
        {
            _service = service;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get() => _service.GetForecasts();
    }
}
