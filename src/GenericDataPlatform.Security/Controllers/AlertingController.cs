using System;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Alerting;
using GenericDataPlatform.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Security.Controllers
{
    [ApiController]
    [Route("api/security/alerting")]
    [Authorize(Roles = "Admin,Operations")]
    public class AlertingController : ControllerBase
    {
        private readonly IAlertingService _alertingService;
        private readonly ILogger<AlertingController> _logger;

        public AlertingController(IAlertingService alertingService, ILogger<AlertingController> logger)
        {
            _alertingService = alertingService;
            _logger = logger;
        }

        /// <summary>
        /// Creates an alert rule
        /// </summary>
        [HttpPost("rules")]
        public async Task<IActionResult> CreateAlertRule([FromBody] AlertRule alertRule)
        {
            try
            {
                _logger.LogInformation("Creating alert rule: {Name}", alertRule.Name);
                
                var ruleId = await _alertingService.CreateAlertRuleAsync(alertRule);
                
                return Ok(new { id = ruleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert rule: {Name}", alertRule.Name);
                return StatusCode(500, new { error = "Error creating alert rule", detail = ex.Message });
            }
        }

        /// <summary>
        /// Updates an alert rule
        /// </summary>
        [HttpPut("rules/{ruleId}")]
        public async Task<IActionResult> UpdateAlertRule(string ruleId, [FromBody] AlertRule alertRule)
        {
            try
            {
                if (ruleId != alertRule.Id)
                {
                    return BadRequest(new { error = "Rule ID in URL does not match rule ID in body" });
                }
                
                _logger.LogInformation("Updating alert rule: {Name} with ID {Id}", alertRule.Name, alertRule.Id);
                
                await _alertingService.UpdateAlertRuleAsync(alertRule);
                
                return Ok(new { message = "Alert rule updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alert rule: {Name} with ID {Id}", alertRule.Name, alertRule.Id);
                return StatusCode(500, new { error = "Error updating alert rule", detail = ex.Message });
            }
        }

        /// <summary>
        /// Deletes an alert rule
        /// </summary>
        [HttpDelete("rules/{ruleId}")]
        public async Task<IActionResult> DeleteAlertRule(string ruleId)
        {
            try
            {
                _logger.LogInformation("Deleting alert rule with ID {Id}", ruleId);
                
                await _alertingService.DeleteAlertRuleAsync(ruleId);
                
                return Ok(new { message = "Alert rule deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert rule with ID {Id}", ruleId);
                return StatusCode(500, new { error = "Error deleting alert rule", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets an alert rule by ID
        /// </summary>
        [HttpGet("rules/{ruleId}")]
        public async Task<IActionResult> GetAlertRule(string ruleId)
        {
            try
            {
                _logger.LogInformation("Getting alert rule with ID {Id}", ruleId);
                
                var alertRule = await _alertingService.GetAlertRuleAsync(ruleId);
                
                if (alertRule == null)
                {
                    return NotFound(new { error = "Alert rule not found" });
                }
                
                return Ok(alertRule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert rule with ID {Id}", ruleId);
                return StatusCode(500, new { error = "Error getting alert rule", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets all alert rules
        /// </summary>
        [HttpGet("rules")]
        public async Task<IActionResult> GetAlertRules()
        {
            try
            {
                _logger.LogInformation("Getting all alert rules");
                
                var alertRules = await _alertingService.GetAlertRulesAsync();
                
                return Ok(alertRules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all alert rules");
                return StatusCode(500, new { error = "Error getting alert rules", detail = ex.Message });
            }
        }

        /// <summary>
        /// Triggers an alert
        /// </summary>
        [HttpPost("alerts")]
        public async Task<IActionResult> TriggerAlert([FromBody] Alert alert)
        {
            try
            {
                _logger.LogInformation("Triggering alert: {Name}", alert.Name);
                
                var alertId = await _alertingService.TriggerAlertAsync(alert);
                
                return Ok(new { id = alertId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering alert: {Name}", alert.Name);
                return StatusCode(500, new { error = "Error triggering alert", detail = ex.Message });
            }
        }

        /// <summary>
        /// Resolves an alert
        /// </summary>
        [HttpPost("alerts/{alertId}/resolve")]
        public async Task<IActionResult> ResolveAlert(string alertId, [FromBody] ResolveAlertRequest request)
        {
            try
            {
                _logger.LogInformation("Resolving alert with ID {Id}", alertId);
                
                await _alertingService.ResolveAlertAsync(alertId, request?.Resolution);
                
                return Ok(new { message = "Alert resolved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert with ID {Id}", alertId);
                return StatusCode(500, new { error = "Error resolving alert", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets an alert by ID
        /// </summary>
        [HttpGet("alerts/{alertId}")]
        public async Task<IActionResult> GetAlert(string alertId)
        {
            try
            {
                _logger.LogInformation("Getting alert with ID {Id}", alertId);
                
                var alert = await _alertingService.GetAlertAsync(alertId);
                
                if (alert == null)
                {
                    return NotFound(new { error = "Alert not found" });
                }
                
                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert with ID {Id}", alertId);
                return StatusCode(500, new { error = "Error getting alert", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets alerts by status
        /// </summary>
        [HttpGet("alerts/status/{status}")]
        public async Task<IActionResult> GetAlertsByStatus(AlertStatus status, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting alerts with status {Status}", status);
                
                var alerts = await _alertingService.GetAlertsByStatusAsync(status, limit);
                
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts with status {Status}", status);
                return StatusCode(500, new { error = "Error getting alerts", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets alerts by severity
        /// </summary>
        [HttpGet("alerts/severity/{severity}")]
        public async Task<IActionResult> GetAlertsBySeverity(AlertSeverity severity, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting alerts with severity {Severity}", severity);
                
                var alerts = await _alertingService.GetAlertsBySeverityAsync(severity, limit);
                
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts with severity {Severity}", severity);
                return StatusCode(500, new { error = "Error getting alerts", detail = ex.Message });
            }
        }

        /// <summary>
        /// Evaluates metric data against alert rules
        /// </summary>
        [HttpPost("evaluate")]
        public async Task<IActionResult> EvaluateMetricData([FromBody] EvaluateMetricRequest request)
        {
            try
            {
                _logger.LogInformation("Evaluating metric data: {MetricName} = {Value}", request.MetricName, request.Value);
                
                await _alertingService.EvaluateMetricDataAsync(request.MetricName, request.Value, request.Labels);
                
                return Ok(new { message = "Metric data evaluated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating metric data: {MetricName} = {Value}", request.MetricName, request.Value);
                return StatusCode(500, new { error = "Error evaluating metric data", detail = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for resolving an alert
    /// </summary>
    public class ResolveAlertRequest
    {
        /// <summary>
        /// Resolution of the alert
        /// </summary>
        public string Resolution { get; set; }
    }

    /// <summary>
    /// Request model for evaluating metric data
    /// </summary>
    public class EvaluateMetricRequest
    {
        /// <summary>
        /// Name of the metric
        /// </summary>
        public string MetricName { get; set; }
        
        /// <summary>
        /// Value of the metric
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// Labels of the metric
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> Labels { get; set; }
    }
}
