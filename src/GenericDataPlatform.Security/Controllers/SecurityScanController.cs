using System;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models;
using GenericDataPlatform.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Security.Controllers
{
    [ApiController]
    [Route("api/security/scan")]
    [Authorize(Roles = "Admin,Security")]
    public class SecurityScanController : ControllerBase
    {
        private readonly ISecurityScanner _securityScanner;
        private readonly ILogger<SecurityScanController> _logger;

        public SecurityScanController(ISecurityScanner securityScanner, ILogger<SecurityScanController> logger)
        {
            _securityScanner = securityScanner;
            _logger = logger;
        }

        /// <summary>
        /// Scans a project for security vulnerabilities
        /// </summary>
        [HttpPost("project")]
        public async Task<IActionResult> ScanProject([FromBody] ScanProjectRequest request)
        {
            try
            {
                _logger.LogInformation("Scanning project {ProjectPath}", request.ProjectPath);
                
                var result = await _securityScanner.ScanProjectAsync(request.ProjectPath);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning project {ProjectPath}", request.ProjectPath);
                return StatusCode(500, new { error = "Error scanning project", detail = ex.Message });
            }
        }

        /// <summary>
        /// Scans a solution for security vulnerabilities
        /// </summary>
        [HttpPost("solution")]
        public async Task<IActionResult> ScanSolution([FromBody] ScanSolutionRequest request)
        {
            try
            {
                _logger.LogInformation("Scanning solution {SolutionPath}", request.SolutionPath);
                
                var results = await _securityScanner.ScanSolutionAsync(request.SolutionPath);
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning solution {SolutionPath}", request.SolutionPath);
                return StatusCode(500, new { error = "Error scanning solution", detail = ex.Message });
            }
        }

        /// <summary>
        /// Generates a security report for a project
        /// </summary>
        [HttpGet("report/{projectPath}")]
        public async Task<IActionResult> GenerateReport(string projectPath)
        {
            try
            {
                _logger.LogInformation("Generating security report for project {ProjectPath}", projectPath);
                
                var report = await _securityScanner.GenerateReportAsync(projectPath);
                
                if (report == null)
                {
                    return NotFound(new { error = "No scan results found for project" });
                }
                
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating security report for project {ProjectPath}", projectPath);
                return StatusCode(500, new { error = "Error generating security report", detail = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for scanning a project
    /// </summary>
    public class ScanProjectRequest
    {
        /// <summary>
        /// Path to the project file
        /// </summary>
        public string ProjectPath { get; set; }
    }

    /// <summary>
    /// Request model for scanning a solution
    /// </summary>
    public class ScanSolutionRequest
    {
        /// <summary>
        /// Path to the solution file
        /// </summary>
        public string SolutionPath { get; set; }
    }
}
