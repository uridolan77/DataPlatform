using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Compliance.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GenericDataPlatform.Compliance.Repositories
{
    /// <summary>
    /// Repository for storing and retrieving audit events
    /// </summary>
    public class AuditRepository : IAuditRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AuditRepository> _logger;

        public AuditRepository(IOptions<ComplianceOptions> options, ILogger<AuditRepository> logger)
        {
            _connectionString = options.Value.DatabaseConnectionString;
            _logger = logger;
        }

        /// <summary>
        /// Saves an audit event to the database
        /// </summary>
        public async Task SaveEventAsync(AuditEvent auditEvent)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO audit_events (
                        id, event_type, user_id, resource_id, resource_type, 
                        action, status, timestamp, details, ip_address, 
                        user_agent, service_name, correlation_id
                    ) VALUES (
                        @Id, @EventType, @UserId, @ResourceId, @ResourceType, 
                        @Action, @Status, @Timestamp, @Details, @IpAddress, 
                        @UserAgent, @ServiceName, @CorrelationId
                    )";

                var parameters = new
                {
                    auditEvent.Id,
                    auditEvent.EventType,
                    auditEvent.UserId,
                    auditEvent.ResourceId,
                    auditEvent.ResourceType,
                    auditEvent.Action,
                    auditEvent.Status,
                    auditEvent.Timestamp,
                    Details = JsonSerializer.Serialize(auditEvent.Details),
                    auditEvent.IpAddress,
                    auditEvent.UserAgent,
                    auditEvent.ServiceName,
                    auditEvent.CorrelationId
                };

                await connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving audit event {EventId}", auditEvent.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets audit events by resource ID
        /// </summary>
        public async Task<IEnumerable<AuditEvent>> GetEventsByResourceAsync(string resourceId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM audit_events 
                    WHERE resource_id = @ResourceId
                    AND (@StartTime IS NULL OR timestamp >= @StartTime)
                    AND (@EndTime IS NULL OR timestamp <= @EndTime)
                    ORDER BY timestamp DESC
                    LIMIT @Limit";

                var parameters = new
                {
                    ResourceId = resourceId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Limit = limit
                };

                var events = await connection.QueryAsync<AuditEventDto>(sql, parameters);
                return MapToAuditEvents(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit events for resource {ResourceId}", resourceId);
                throw;
            }
        }

        /// <summary>
        /// Gets audit events by user ID
        /// </summary>
        public async Task<IEnumerable<AuditEvent>> GetEventsByUserAsync(string userId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM audit_events 
                    WHERE user_id = @UserId
                    AND (@StartTime IS NULL OR timestamp >= @StartTime)
                    AND (@EndTime IS NULL OR timestamp <= @EndTime)
                    ORDER BY timestamp DESC
                    LIMIT @Limit";

                var parameters = new
                {
                    UserId = userId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Limit = limit
                };

                var events = await connection.QueryAsync<AuditEventDto>(sql, parameters);
                return MapToAuditEvents(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit events for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets audit events by event type
        /// </summary>
        public async Task<IEnumerable<AuditEvent>> GetEventsByTypeAsync(string eventType, DateTime? startTime = null, DateTime? endTime = null, int limit = 100)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM audit_events 
                    WHERE event_type = @EventType
                    AND (@StartTime IS NULL OR timestamp >= @StartTime)
                    AND (@EndTime IS NULL OR timestamp <= @EndTime)
                    ORDER BY timestamp DESC
                    LIMIT @Limit";

                var parameters = new
                {
                    EventType = eventType,
                    StartTime = startTime,
                    EndTime = endTime,
                    Limit = limit
                };

                var events = await connection.QueryAsync<AuditEventDto>(sql, parameters);
                return MapToAuditEvents(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit events of type {EventType}", eventType);
                throw;
            }
        }

        /// <summary>
        /// Searches audit events with filters
        /// </summary>
        public async Task<IEnumerable<AuditEvent>> SearchEventsAsync(AuditEventFilter filter, int limit = 100)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var conditions = new List<string>();
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(filter.UserId))
                {
                    conditions.Add("user_id = @UserId");
                    parameters.Add("UserId", filter.UserId);
                }

                if (!string.IsNullOrEmpty(filter.ResourceId))
                {
                    conditions.Add("resource_id = @ResourceId");
                    parameters.Add("ResourceId", filter.ResourceId);
                }

                if (!string.IsNullOrEmpty(filter.ResourceType))
                {
                    conditions.Add("resource_type = @ResourceType");
                    parameters.Add("ResourceType", filter.ResourceType);
                }

                if (!string.IsNullOrEmpty(filter.EventType))
                {
                    conditions.Add("event_type = @EventType");
                    parameters.Add("EventType", filter.EventType);
                }

                if (!string.IsNullOrEmpty(filter.Action))
                {
                    conditions.Add("action = @Action");
                    parameters.Add("Action", filter.Action);
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    conditions.Add("status = @Status");
                    parameters.Add("Status", filter.Status);
                }

                if (filter.StartTime.HasValue)
                {
                    conditions.Add("timestamp >= @StartTime");
                    parameters.Add("StartTime", filter.StartTime.Value);
                }

                if (filter.EndTime.HasValue)
                {
                    conditions.Add("timestamp <= @EndTime");
                    parameters.Add("EndTime", filter.EndTime.Value);
                }

                if (!string.IsNullOrEmpty(filter.IpAddress))
                {
                    conditions.Add("ip_address = @IpAddress");
                    parameters.Add("IpAddress", filter.IpAddress);
                }

                if (!string.IsNullOrEmpty(filter.ServiceName))
                {
                    conditions.Add("service_name = @ServiceName");
                    parameters.Add("ServiceName", filter.ServiceName);
                }

                if (!string.IsNullOrEmpty(filter.CorrelationId))
                {
                    conditions.Add("correlation_id = @CorrelationId");
                    parameters.Add("CorrelationId", filter.CorrelationId);
                }

                var whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";
                var sql = $@"
                    SELECT * FROM audit_events 
                    {whereClause}
                    ORDER BY timestamp DESC
                    LIMIT @Limit";

                parameters.Add("Limit", limit);

                var events = await connection.QueryAsync<AuditEventDto>(sql, parameters);
                return MapToAuditEvents(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching audit events");
                throw;
            }
        }

        /// <summary>
        /// Maps database DTOs to domain models
        /// </summary>
        private IEnumerable<AuditEvent> MapToAuditEvents(IEnumerable<AuditEventDto> dtos)
        {
            foreach (var dto in dtos)
            {
                var auditEvent = new AuditEvent
                {
                    Id = dto.id,
                    EventType = dto.event_type,
                    UserId = dto.user_id,
                    ResourceId = dto.resource_id,
                    ResourceType = dto.resource_type,
                    Action = dto.action,
                    Status = dto.status,
                    Timestamp = dto.timestamp,
                    IpAddress = dto.ip_address,
                    UserAgent = dto.user_agent,
                    ServiceName = dto.service_name,
                    CorrelationId = dto.correlation_id
                };

                if (!string.IsNullOrEmpty(dto.details))
                {
                    try
                    {
                        auditEvent.Details = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.details);
                    }
                    catch
                    {
                        auditEvent.Details = new Dictionary<string, object>
                        {
                            ["raw_details"] = dto.details
                        };
                    }
                }

                yield return auditEvent;
            }
        }

        /// <summary>
        /// DTO for mapping database records
        /// </summary>
        private class AuditEventDto
        {
            public string id { get; set; }
            public string event_type { get; set; }
            public string user_id { get; set; }
            public string resource_id { get; set; }
            public string resource_type { get; set; }
            public string action { get; set; }
            public string status { get; set; }
            public DateTime timestamp { get; set; }
            public string details { get; set; }
            public string ip_address { get; set; }
            public string user_agent { get; set; }
            public string service_name { get; set; }
            public string correlation_id { get; set; }
        }
    }

    /// <summary>
    /// Interface for audit repository
    /// </summary>
    public interface IAuditRepository
    {
        Task SaveEventAsync(AuditEvent auditEvent);
        Task<IEnumerable<AuditEvent>> GetEventsByResourceAsync(string resourceId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<IEnumerable<AuditEvent>> GetEventsByUserAsync(string userId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<IEnumerable<AuditEvent>> GetEventsByTypeAsync(string eventType, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<IEnumerable<AuditEvent>> SearchEventsAsync(AuditEventFilter filter, int limit = 100);
    }
}
