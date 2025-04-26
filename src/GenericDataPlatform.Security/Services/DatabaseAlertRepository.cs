using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Security.Models.Alerting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// SQL Server implementation of the alert repository
    /// </summary>
    public class DatabaseAlertRepository : IAlertRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseAlertRepository> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly JsonSerializerOptions _jsonOptions;

        public DatabaseAlertRepository(
            IOptions<SecurityOptions> options,
            ILogger<DatabaseAlertRepository> logger,
            IAsyncPolicy resiliencePolicy)
        {
            _connectionString = options.Value.ConnectionStrings?.SqlServer 
                ?? throw new ArgumentNullException(nameof(options.Value.ConnectionStrings.SqlServer), 
                    "SQL Server connection string is required for DatabaseAlertRepository");
            _logger = logger;
            _resiliencePolicy = resiliencePolicy;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Ensure database tables exist
            EnsureTablesExistAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureTablesExistAsync()
        {
            try
            {
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Check if AlertRules table exists
                    var tableExists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertRules'");

                    if (tableExists == 0)
                    {
                        _logger.LogInformation("Creating alert tables");
                        
                        // Read SQL script from embedded resource
                        var assembly = typeof(DatabaseAlertRepository).Assembly;
                        var resourceName = "GenericDataPlatform.Security.Database.Scripts.CreateAlertTables.sql";
                        
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
                        }
                        
                        using var reader = new System.IO.StreamReader(stream);
                        var sql = await reader.ReadToEndAsync();
                        
                        // Execute script
                        await connection.ExecuteAsync(sql);
                        
                        _logger.LogInformation("Alert tables created successfully");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring alert tables exist");
                throw;
            }
        }

        /// <summary>
        /// Saves an alert rule
        /// </summary>
        public async Task SaveAlertRuleAsync(AlertRule alertRule)
        {
            try
            {
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    alertRule.Id ??= Guid.NewGuid().ToString();
                    
                    // Set updated timestamp
                    alertRule.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        MERGE INTO AlertRules AS target
                        USING (SELECT @Id AS Id) AS source
                        ON target.Id = source.Id
                        WHEN MATCHED THEN
                            UPDATE SET 
                                Name = @Name,
                                Description = @Description,
                                Severity = @Severity,
                                MetricName = @MetricName,
                                Labels = @Labels,
                                Operator = @Operator,
                                Threshold = @Threshold,
                                Enabled = @Enabled,
                                AutoResolve = @AutoResolve,
                                Notifications = @Notifications,
                                UpdatedAt = @UpdatedAt
                        WHEN NOT MATCHED THEN
                            INSERT (Id, Name, Description, Severity, MetricName, Labels, Operator, Threshold, Enabled, AutoResolve, Notifications, CreatedAt, UpdatedAt)
                            VALUES (@Id, @Name, @Description, @Severity, @MetricName, @Labels, @Operator, @Threshold, @Enabled, @AutoResolve, @Notifications, @CreatedAt, @UpdatedAt);";

                    await connection.ExecuteAsync(sql, new
                    {
                        alertRule.Id,
                        alertRule.Name,
                        alertRule.Description,
                        Severity = (int)alertRule.Severity,
                        alertRule.MetricName,
                        Labels = JsonSerializer.Serialize(alertRule.Labels, _jsonOptions),
                        Operator = (int)alertRule.Operator,
                        alertRule.Threshold,
                        Enabled = alertRule.Enabled ? 1 : 0,
                        AutoResolve = alertRule.AutoResolve ? 1 : 0,
                        Notifications = JsonSerializer.Serialize(alertRule.Notifications, _jsonOptions),
                        alertRule.CreatedAt,
                        alertRule.UpdatedAt
                    });
                    
                    _logger.LogInformation("Saved alert rule {RuleId} of type {MetricName}", alertRule.Id, alertRule.MetricName);
                });
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
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // First delete all alerts associated with this rule
                    await connection.ExecuteAsync("DELETE FROM Alerts WHERE RuleId = @RuleId", new { RuleId = ruleId });
                    
                    // Then delete the rule
                    await connection.ExecuteAsync("DELETE FROM AlertRules WHERE Id = @Id", new { Id = ruleId });
                    
                    _logger.LogInformation("Deleted alert rule {RuleId}", ruleId);
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM AlertRules WHERE Id = @Id";
                    var rule = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = ruleId });
                    
                    if (rule == null)
                    {
                        _logger.LogWarning("Alert rule {RuleId} not found", ruleId);
                        return null;
                    }
                    
                    return MapToAlertRule(rule);
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM AlertRules ORDER BY CreatedAt DESC";
                    var rules = await connection.QueryAsync<dynamic>(sql);
                    
                    return rules.Select(MapToAlertRule).ToList();
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM AlertRules WHERE MetricName = @MetricName ORDER BY CreatedAt DESC";
                    var rules = await connection.QueryAsync<dynamic>(sql, new { MetricName = metricName });
                    
                    return rules.Select(MapToAlertRule).ToList();
                });
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
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    alert.Id ??= Guid.NewGuid().ToString();
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        MERGE INTO Alerts AS target
                        USING (SELECT @Id AS Id) AS source
                        ON target.Id = source.Id
                        WHEN MATCHED THEN
                            UPDATE SET 
                                RuleId = @RuleId,
                                Name = @Name,
                                Description = @Description,
                                Severity = @Severity,
                                Status = @Status,
                                MetricName = @MetricName,
                                MetricValue = @MetricValue,
                                Labels = @Labels,
                                Timestamp = @Timestamp,
                                ResolvedAt = @ResolvedAt,
                                Resolution = @Resolution
                        WHEN NOT MATCHED THEN
                            INSERT (Id, RuleId, Name, Description, Severity, Status, MetricName, MetricValue, Labels, Timestamp, ResolvedAt, Resolution)
                            VALUES (@Id, @RuleId, @Name, @Description, @Severity, @Status, @MetricName, @MetricValue, @Labels, @Timestamp, @ResolvedAt, @Resolution);";

                    await connection.ExecuteAsync(sql, new
                    {
                        alert.Id,
                        alert.RuleId,
                        alert.Name,
                        alert.Description,
                        Severity = (int)alert.Severity,
                        Status = (int)alert.Status,
                        alert.MetricName,
                        alert.MetricValue,
                        Labels = JsonSerializer.Serialize(alert.Labels, _jsonOptions),
                        alert.Timestamp,
                        alert.ResolvedAt,
                        alert.Resolution
                    });
                    
                    _logger.LogInformation("Saved alert {AlertId} of type {MetricName}", alert.Id, alert.MetricName);
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM Alerts WHERE Id = @Id";
                    var alert = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = alertId });
                    
                    if (alert == null)
                    {
                        _logger.LogWarning("Alert {AlertId} not found", alertId);
                        return null;
                    }
                    
                    return MapToAlert(alert);
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM Alerts WHERE Status = @Status ORDER BY Timestamp DESC";
                    var alerts = await connection.QueryAsync<dynamic>(sql, new { Status = (int)status, Limit = limit });
                    
                    return alerts.Select(MapToAlert).ToList();
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM Alerts WHERE Severity = @Severity ORDER BY Timestamp DESC";
                    var alerts = await connection.QueryAsync<dynamic>(sql, new { Severity = (int)severity, Limit = limit });
                    
                    return alerts.Select(MapToAlert).ToList();
                });
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
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM Alerts WHERE RuleId = @RuleId AND Status = @Status ORDER BY Timestamp DESC";
                    var alerts = await connection.QueryAsync<dynamic>(sql, new { RuleId = ruleId, Status = (int)AlertStatus.Active });
                    
                    return alerts.Select(MapToAlert).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active alerts for rule {RuleId}", ruleId);
                return new List<Alert>();
            }
        }

        #region Helper Methods

        private AlertRule MapToAlertRule(dynamic rule)
        {
            var alertRule = new AlertRule
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                Severity = (AlertSeverity)rule.Severity,
                MetricName = rule.MetricName,
                Operator = (AlertOperator)rule.Operator,
                Threshold = rule.Threshold,
                Enabled = Convert.ToBoolean(rule.Enabled),
                AutoResolve = Convert.ToBoolean(rule.AutoResolve),
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };
            
            // Deserialize Labels
            if (!string.IsNullOrEmpty(rule.Labels))
            {
                alertRule.Labels = JsonSerializer.Deserialize<Dictionary<string, string>>(rule.Labels, _jsonOptions);
            }
            
            // Deserialize Notifications
            if (!string.IsNullOrEmpty(rule.Notifications))
            {
                alertRule.Notifications = JsonSerializer.Deserialize<List<AlertNotification>>(rule.Notifications, _jsonOptions);
            }
            
            return alertRule;
        }

        private Alert MapToAlert(dynamic alert)
        {
            var alertObj = new Alert
            {
                Id = alert.Id,
                RuleId = alert.RuleId,
                Name = alert.Name,
                Description = alert.Description,
                Severity = (AlertSeverity)alert.Severity,
                Status = (AlertStatus)alert.Status,
                MetricName = alert.MetricName,
                MetricValue = alert.MetricValue,
                Timestamp = alert.Timestamp,
                ResolvedAt = alert.ResolvedAt,
                Resolution = alert.Resolution
            };
            
            // Deserialize Labels
            if (!string.IsNullOrEmpty(alert.Labels))
            {
                alertObj.Labels = JsonSerializer.Deserialize<Dictionary<string, string>>(alert.Labels, _jsonOptions);
            }
            
            return alertObj;
        }

        #endregion
    }
}
