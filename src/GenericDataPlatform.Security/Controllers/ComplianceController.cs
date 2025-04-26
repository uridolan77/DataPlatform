using System;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Compliance;
using GenericDataPlatform.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Security.Controllers
{
    [ApiController]
    [Route("api/security/compliance")]
    [Authorize(Roles = "Admin,Compliance")]
    public class ComplianceController : ControllerBase
    {
        private readonly IComplianceService _complianceService;
        private readonly ILogger<ComplianceController> _logger;

        public ComplianceController(IComplianceService complianceService, ILogger<ComplianceController> logger)
        {
            _complianceService = complianceService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a GDPR compliance report
        /// </summary>
        [HttpGet("gdpr")]
        public async Task<IActionResult> GenerateGdprReport()
        {
            try
            {
                _logger.LogInformation("Generating GDPR compliance report");
                
                var report = await _complianceService.GenerateGdprReportAsync();
                
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating GDPR compliance report");
                return StatusCode(500, new { error = "Error generating GDPR compliance report", detail = ex.Message });
            }
        }

        /// <summary>
        /// Generates a HIPAA compliance report
        /// </summary>
        [HttpGet("hipaa")]
        public async Task<IActionResult> GenerateHipaaReport()
        {
            try
            {
                _logger.LogInformation("Generating HIPAA compliance report");
                
                var report = await _complianceService.GenerateHipaaReportAsync();
                
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HIPAA compliance report");
                return StatusCode(500, new { error = "Error generating HIPAA compliance report", detail = ex.Message });
            }
        }

        /// <summary>
        /// Generates a custom compliance report
        /// </summary>
        [HttpPost("custom")]
        public async Task<IActionResult> GenerateCustomReport([FromBody] CustomReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating custom compliance report of type {ReportType}", request.ReportType);
                
                var report = await _complianceService.GenerateCustomReportAsync(request.ReportType, request.Requirements);
                
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom compliance report of type {ReportType}", request.ReportType);
                return StatusCode(500, new { error = "Error generating custom compliance report", detail = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for generating a custom compliance report
    /// </summary>
    public class CustomReportRequest
    {
        /// <summary>
        /// Type of the report
        /// </summary>
        public string ReportType { get; set; }
        
        /// <summary>
        /// Compliance requirements
        /// </summary>
        public System.Collections.Generic.List<ComplianceRequirement> Requirements { get; set; }
    }
}
