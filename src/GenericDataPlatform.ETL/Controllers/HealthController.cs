using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        
        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }
        
        [HttpGet]
        public ActionResult<object> GetHealth()
        {
            try
            {
                return Ok(new
                {
                    Status = "Healthy",
                    Service = "ETLService",
                    Version = GetType().Assembly.GetName().Version.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health");
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    Service = "ETLService",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
