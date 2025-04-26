using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Alerting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Service for managing and triggering alerts
    /// </summary>
    public class AlertingService : IAlertingService
    {
        private readonly SecurityOptions _options;
        private readonly IAlertRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AlertingService> _logger;

        public AlertingService(
            IOptions<SecurityOptions> options,
            IAlertRepository repository,
            INotificationService notificationService,
            ILogger<AlertingService> logger)
        {
            _options = options.Value;
            _repository = repository;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Creates an alert rule
        /// </summary>
        public async Task<string> CreateAlertRuleAsync(AlertRule alertRule)
        {
            try
            {
                _logger.LogInformation("Creating alert rule: {Name}", alertRule.Name);
                
                // Ensure required fields are set
                alertRule.Id ??= Guid.NewGuid().ToString();
                alertRule.CreatedAt = DateTime.UtcNow;
                alertRule.UpdatedAt = DateTime.UtcNow;
                
                // Save the alert rule
                await _repository.SaveAlertRuleAsync(alertRule);
                
                _logger.LogInformation("Created alert rule: {Name} with ID {Id}", alertRule.Name, alertRule.Id);
                
                return alertRule.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert rule: {Name}", alertRule.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an alert rule
        /// </summary>
        public async Task UpdateAlertRuleAsync(AlertRule alertRule)
        {
            try
            {
                _logger.LogInformation("Updating alert rule: {Name} with ID {Id}", alertRule.Name, alertRule.Id);
                
                // Ensure required fields are set
                alertRule.UpdatedAt = DateTime.UtcNow;
                
                // Save the alert rule
                await _repository.SaveAlertRuleAsync(alertRule);
                
                _logger.LogInformation("Updated alert rule: {Name} with ID {Id}", alertRule.Name, alertRule.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alert rule: {Name} with ID {Id}", alertRule.Name, alertRule.Id);
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
                _logger.LogInformation("Deleting alert rule with ID {Id}", ruleId);
                
                // Delete the alert rule
                await _repository.DeleteAlertRuleAsync(ruleId);
                
                _logger.LogInformation("Deleted alert rule with ID {Id}", ruleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert rule with ID {Id}", ruleId);
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
                _logger.LogInformation("Getting alert rule with ID {Id}", ruleId);
                
                // Get the alert rule
                var alertRule = await _repository.GetAlertRuleAsync(ruleId);
                
                if (alertRule == null)
                {
                    _logger.LogWarning("Alert rule with ID {Id} not found", ruleId);
                }
                
                return alertRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert rule with ID {Id}", ruleId);
                throw;
            }
        }

        /// <summary>
        /// Gets all alert rules
        /// </summary>
        public async Task<List<AlertRule>> GetAlertRulesAsync()
        {
            try
            {
                _logger.LogInformation("Getting all alert rules");
                
                // Get all alert rules
                var alertRules = await _repository.GetAlertRulesAsync();
                
                _logger.LogInformation("Found {Count} alert rules", alertRules.Count);
                
                return alertRules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all alert rules");
                throw;
            }
        }

        /// <summary>
        /// Triggers an alert
        /// </summary>
        public async Task<string> TriggerAlertAsync(Alert alert)
        {
            try
            {
                _logger.LogInformation("Triggering alert: {Name}", alert.Name);
                
                // Ensure required fields are set
                alert.Id ??= Guid.NewGuid().ToString();
                alert.Timestamp = DateTime.UtcNow;
                alert.Status = AlertStatus.Active;
                
                // Save the alert
                await _repository.SaveAlertAsync(alert);
                
                // Send notifications
                await SendAlertNotificationsAsync(alert);
                
                _logger.LogInformation("Triggered alert: {Name} with ID {Id}", alert.Name, alert.Id);
                
                return alert.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering alert: {Name}", alert.Name);
                throw;
            }
        }

        /// <summary>
        /// Resolves an alert
        /// </summary>
        public async Task ResolveAlertAsync(string alertId, string resolution = null)
        {
            try
            {
                _logger.LogInformation("Resolving alert with ID {Id}", alertId);
                
                // Get the alert
                var alert = await _repository.GetAlertAsync(alertId);
                
                if (alert == null)
                {
                    _logger.LogWarning("Alert with ID {Id} not found", alertId);
                    return;
                }
                
                // Update the alert
                alert.Status = AlertStatus.Resolved;
                alert.Resolution = resolution;
                alert.ResolvedAt = DateTime.UtcNow;
                
                // Save the alert
                await _repository.SaveAlertAsync(alert);
                
                _logger.LogInformation("Resolved alert with ID {Id}", alertId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert with ID {Id}", alertId);
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
                _logger.LogInformation("Getting alert with ID {Id}", alertId);
                
                // Get the alert
                var alert = await _repository.GetAlertAsync(alertId);
                
                if (alert == null)
                {
                    _logger.LogWarning("Alert with ID {Id} not found", alertId);
                }
                
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert with ID {Id}", alertId);
                throw;
            }
        }

        /// <summary>
        /// Gets alerts by status
        /// </summary>
        public async Task<List<Alert>> GetAlertsByStatusAsync(AlertStatus status, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting alerts with status {Status}", status);
                
                // Get alerts by status
                var alerts = await _repository.GetAlertsByStatusAsync(status, limit);
                
                _logger.LogInformation("Found {Count} alerts with status {Status}", alerts.Count, status);
                
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts with status {Status}", status);
                throw;
            }
        }

        /// <summary>
        /// Gets alerts by severity
        /// </summary>
        public async Task<List<Alert>> GetAlertsBySeverityAsync(AlertSeverity severity, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting alerts with severity {Severity}", severity);
                
                // Get alerts by severity
                var alerts = await _repository.GetAlertsBySeverityAsync(severity, limit);
                
                _logger.LogInformation("Found {Count} alerts with severity {Severity}", alerts.Count, severity);
                
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts with severity {Severity}", severity);
                throw;
            }
        }

        /// <summary>
        /// Evaluates metric data against alert rules
        /// </summary>
        public async Task EvaluateMetricDataAsync(string metricName, double value, Dictionary<string, string> labels = null)
        {
            try
            {
                _logger.LogInformation("Evaluating metric data: {MetricName} = {Value}", metricName, value);
                
                // Get alert rules for the metric
                var alertRules = await _repository.GetAlertRulesByMetricAsync(metricName);
                
                foreach (var rule in alertRules)
                {
                    // Skip disabled rules
                    if (!rule.Enabled)
                    {
                        continue;
                    }
                    
                    // Check if the rule applies to the labels
                    if (!LabelsMatch(rule.Labels, labels))
                    {
                        continue;
                    }
                    
                    // Evaluate the rule
                    bool triggered = EvaluateRule(rule, value);
                    
                    if (triggered)
                    {
                        // Check if there's already an active alert for this rule
                        var activeAlerts = await _repository.GetActiveAlertsByRuleAsync(rule.Id);
                        
                        if (activeAlerts.Count == 0)
                        {
                            // Trigger a new alert
                            var alert = new Alert
                            {
                                RuleId = rule.Id,
                                Name = rule.Name,
                                Description = rule.Description,
                                Severity = rule.Severity,
                                MetricName = metricName,
                                MetricValue = value,
                                Labels = labels ?? new Dictionary<string, string>()
                            };
                            
                            await TriggerAlertAsync(alert);
                        }
                    }
                    else
                    {
                        // Check if there are active alerts that should be auto-resolved
                        if (rule.AutoResolve)
                        {
                            var activeAlerts = await _repository.GetActiveAlertsByRuleAsync(rule.Id);
                            
                            foreach (var alert in activeAlerts)
                            {
                                await ResolveAlertAsync(alert.Id, "Auto-resolved: metric value returned to normal range");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating metric data: {MetricName} = {Value}", metricName, value);
                throw;
            }
        }

        /// <summary>
        /// Sends notifications for an alert
        /// </summary>
        private async Task SendAlertNotificationsAsync(Alert alert)
        {
            try
            {
                // Get the alert rule
                var rule = await _repository.GetAlertRuleAsync(alert.RuleId);
                
                if (rule == null || rule.Notifications == null || rule.Notifications.Count == 0)
                {
                    return;
                }
                
                // Send notifications
                foreach (var notification in rule.Notifications)
                {
                    try
                    {
                        await _notificationService.SendNotificationAsync(new Notification
                        {
                            Type = notification.Type,
                            Recipients = notification.Recipients,
                            Subject = $"Alert: {alert.Name}",
                            Message = $"Alert: {alert.Name}\n\nDescription: {alert.Description}\n\nSeverity: {alert.Severity}\n\nMetric: {alert.MetricName} = {alert.MetricValue}\n\nTimestamp: {alert.Timestamp}",
                            Properties = new Dictionary<string, object>
                            {
                                { "AlertId", alert.Id },
                                { "AlertName", alert.Name },
                                { "AlertSeverity", alert.Severity.ToString() },
                                { "MetricName", alert.MetricName },
                                { "MetricValue", alert.MetricValue }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending notification for alert {AlertId}", alert.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for alert {AlertId}", alert.Id);
            }
        }

        /// <summary>
        /// Checks if labels match
        /// </summary>
        private bool LabelsMatch(Dictionary<string, string> ruleLabels, Dictionary<string, string> metricLabels)
        {
            if (ruleLabels == null || ruleLabels.Count == 0)
            {
                return true;
            }
            
            if (metricLabels == null)
            {
                return false;
            }
            
            foreach (var label in ruleLabels)
            {
                if (!metricLabels.TryGetValue(label.Key, out var value) || value != label.Value)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Evaluates a rule against a value
        /// </summary>
        private bool EvaluateRule(AlertRule rule, double value)
        {
            switch (rule.Operator)
            {
                case AlertOperator.GreaterThan:
                    return value > rule.Threshold;
                
                case AlertOperator.GreaterThanOrEqual:
                    return value >= rule.Threshold;
                
                case AlertOperator.LessThan:
                    return value < rule.Threshold;
                
                case AlertOperator.LessThanOrEqual:
                    return value <= rule.Threshold;
                
                case AlertOperator.Equal:
                    return Math.Abs(value - rule.Threshold) < 0.0001;
                
                case AlertOperator.NotEqual:
                    return Math.Abs(value - rule.Threshold) >= 0.0001;
                
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Interface for alerting service
    /// </summary>
    public interface IAlertingService
    {
        Task<string> CreateAlertRuleAsync(AlertRule alertRule);
        Task UpdateAlertRuleAsync(AlertRule alertRule);
        Task DeleteAlertRuleAsync(string ruleId);
        Task<AlertRule> GetAlertRuleAsync(string ruleId);
        Task<List<AlertRule>> GetAlertRulesAsync();
        Task<string> TriggerAlertAsync(Alert alert);
        Task ResolveAlertAsync(string alertId, string resolution = null);
        Task<Alert> GetAlertAsync(string alertId);
        Task<List<Alert>> GetAlertsByStatusAsync(AlertStatus status, int limit = 100);
        Task<List<Alert>> GetAlertsBySeverityAsync(AlertSeverity severity, int limit = 100);
        Task EvaluateMetricDataAsync(string metricName, double value, Dictionary<string, string> labels = null);
    }
}
