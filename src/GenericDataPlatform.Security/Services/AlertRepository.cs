using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Alerting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Repository for storing and retrieving alerts and alert rules
    /// </summary>
    public class AlertRepository : IAlertRepository
    {
        private readonly SecurityOptions _options;
        private readonly ILogger<AlertRepository> _logger;
        private readonly string _dataDirectory;

        public AlertRepository(IOptions<SecurityOptions> options, ILogger<AlertRepository> logger)
        {
            _options = options.Value;
            _logger = logger;
            
            // Create data directory if it doesn't exist
            _dataDirectory = _options.DataDirectory ?? Path.Combine(AppContext.BaseDirectory, "SecurityData");
            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(Path.Combine(_dataDirectory, "Alerts"));
            Directory.CreateDirectory(Path.Combine(_dataDirectory, "Alerts", "Rules"));
            Directory.CreateDirectory(Path.Combine(_dataDirectory, "Alerts", "Instances"));
        }

        /// <summary>
        /// Saves an alert rule
        /// </summary>
        public async Task SaveAlertRuleAsync(AlertRule alertRule)
        {
            try
            {
                // Ensure ID is set
                alertRule.Id ??= Guid.NewGuid().ToString();
                
                // Save rule
                var filePath = Path.Combine(_dataDirectory, "Alerts", "Rules", $"{alertRule.Id}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(alertRule, options));
                
                _logger.LogInformation("Saved alert rule {RuleId} of type {MetricName}", alertRule.Id, alertRule.MetricName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving alert rule {RuleId}", alertRule.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes an alert rule
        /// </summary>
        public async Task DeleteAlertRuleAsync(string ruleId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "Alerts", "Rules", $"{ruleId}.json");
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted alert rule {RuleId}", ruleId);
                }
                else
                {
                    _logger.LogWarning("Alert rule {RuleId} not found", ruleId);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert rule {RuleId}", ruleId);
                throw;
            }
        }

        /// <summary>
        /// Gets an alert rule by ID
        /// </summary>
        public async Task<AlertRule> GetAlertRuleAsync(string ruleId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "Alerts", "Rules", $"{ruleId}.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Alert rule {RuleId} not found", ruleId);
                    return null;
                }
                
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<AlertRule>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert rule {RuleId}", ruleId);
                return null;
            }
        }

        /// <summary>
        /// Gets all alert rules
        /// </summary>
        public async Task<List<AlertRule>> GetAlertRulesAsync()
        {
            try
            {
                var rules = new List<AlertRule>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Alerts", "Rules"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var rule = JsonSerializer.Deserialize<AlertRule>(json);
                        
                        if (rule != null)
                        {
                            rules.Add(rule);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading alert rule file {FilePath}", file.FullName);
                    }
                }
                
                return rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert rules");
                return new List<AlertRule>();
            }
        }

        /// <summary>
        /// Gets alert rules by metric
        /// </summary>
        public async Task<List<AlertRule>> GetAlertRulesByMetricAsync(string metricName)
        {
            try
            {
                var rules = await GetAlertRulesAsync();
                return rules.Where(r => r.MetricName == metricName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert rules for metric {MetricName}", metricName);
                return new List<AlertRule>();
            }
        }

        /// <summary>
        /// Saves an alert
        /// </summary>
        public async Task SaveAlertAsync(Alert alert)
        {
            try
            {
                // Ensure ID is set
                alert.Id ??= Guid.NewGuid().ToString();
                
                // Save alert
                var filePath = Path.Combine(_dataDirectory, "Alerts", "Instances", $"{alert.Id}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(alert, options));
                
                _logger.LogInformation("Saved alert {AlertId} of type {MetricName}", alert.Id, alert.MetricName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving alert {AlertId}", alert.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets an alert by ID
        /// </summary>
        public async Task<Alert> GetAlertAsync(string alertId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "Alerts", "Instances", $"{alertId}.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Alert {AlertId} not found", alertId);
                    return null;
                }
                
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<Alert>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert {AlertId}", alertId);
                return null;
            }
        }

        /// <summary>
        /// Gets alerts by status
        /// </summary>
        public async Task<List<Alert>> GetAlertsByStatusAsync(AlertStatus status, int limit = 100)
        {
            try
            {
                var alerts = new List<Alert>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Alerts", "Instances"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    if (alerts.Count >= limit)
                    {
                        break;
                    }
                    
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var alert = JsonSerializer.Deserialize<Alert>(json);
                        
                        if (alert != null && alert.Status == status)
                        {
                            alerts.Add(alert);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading alert file {FilePath}", file.FullName);
                    }
                }
                
                // Sort by timestamp
                return alerts.OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts by status {Status}", status);
                return new List<Alert>();
            }
        }

        /// <summary>
        /// Gets alerts by severity
        /// </summary>
        public async Task<List<Alert>> GetAlertsBySeverityAsync(AlertSeverity severity, int limit = 100)
        {
            try
            {
                var alerts = new List<Alert>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Alerts", "Instances"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    if (alerts.Count >= limit)
                    {
                        break;
                    }
                    
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var alert = JsonSerializer.Deserialize<Alert>(json);
                        
                        if (alert != null && alert.Severity == severity)
                        {
                            alerts.Add(alert);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading alert file {FilePath}", file.FullName);
                    }
                }
                
                // Sort by timestamp
                return alerts.OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts by severity {Severity}", severity);
                return new List<Alert>();
            }
        }

        /// <summary>
        /// Gets active alerts by rule
        /// </summary>
        public async Task<List<Alert>> GetActiveAlertsByRuleAsync(string ruleId)
        {
            try
            {
                var alerts = new List<Alert>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Alerts", "Instances"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var alert = JsonSerializer.Deserialize<Alert>(json);
                        
                        if (alert != null && alert.RuleId == ruleId && alert.Status == AlertStatus.Active)
                        {
                            alerts.Add(alert);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading alert file {FilePath}", file.FullName);
                    }
                }
                
                // Sort by timestamp
                return alerts.OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active alerts by rule {RuleId}", ruleId);
                return new List<Alert>();
            }
        }
    }

    /// <summary>
    /// Interface for alert repository
    /// </summary>
    public interface IAlertRepository
    {
        Task SaveAlertRuleAsync(AlertRule alertRule);
        Task DeleteAlertRuleAsync(string ruleId);
        Task<AlertRule> GetAlertRuleAsync(string ruleId);
        Task<List<AlertRule>> GetAlertRulesAsync();
        Task<List<AlertRule>> GetAlertRulesByMetricAsync(string metricName);
        Task SaveAlertAsync(Alert alert);
        Task<Alert> GetAlertAsync(string alertId);
        Task<List<Alert>> GetAlertsByStatusAsync(AlertStatus status, int limit = 100);
        Task<List<Alert>> GetAlertsBySeverityAsync(AlertSeverity severity, int limit = 100);
        Task<List<Alert>> GetActiveAlertsByRuleAsync(string ruleId);
    }
}
