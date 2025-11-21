using System;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MonitoringPOC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly TelemetryClient _telemetryClient;

        public HealthController(ILogger<HealthController> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        [HttpGet("fail")]
        public IActionResult Fail()
        {
            throw new Exception("Errore simulato nel backend");
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            _logger.LogInformation("Health status endpoint chiamato");
            _telemetryClient.TrackTrace("Health status check eseguito");
            
            return Ok(new 
            { 
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                message = "API operativa e funzionante",
                version = "1.0.0"
            });
        }
    }
}