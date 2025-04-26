using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.Models;
using GenericDataPlatform.Compliance.Repositories;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Compliance.Auditing
{
    /// <summary>
    /// Service for recording and querying audit events
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _repository;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IAuditRepository repository, ILogger<AuditService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Records an audit event
        /// </summary>
        public async Task<string> RecordEventAsync(AuditEvent auditEvent)
        {
            try
            {
                // Ensure required fields are set
                auditEvent.Id ??= Guid.NewGuid().ToString();
                auditEvent.Timestamp = auditEvent.Timestamp == default ? DateTime.UtcNow : auditEvent.Timestamp;
                
                // Record the event
                await _repository.SaveEventAsync(auditEvent);
                
                _logger.LogInformation(
                    "Recorded audit event: {EventType} by {UserId} on {Resource}",
                    auditEvent.EventType,
                    auditEvent.UserId,
                    auditEvent.ResourceId);
                
                return auditEvent.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error recording audit event: {EventType} by {UserId} on {Resource}",
                    auditEvent.EventType,
                    auditEvent.UserId,
                    auditEvent.ResourceId);
                
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
                return await _repository.GetEventsByResourceAsync(resourceId, startTime, endTime, limit);
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
                return await _repository.GetEventsByUserAsync(userId, startTime, endTime, limit);
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
                return await _repository.GetEventsByTypeAsync(eventType, startTime, endTime, limit);
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
                return await _repository.SearchEventsAsync(filter, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching audit events");
                throw;
            }
        }
    }

    /// <summary>
    /// Interface for audit service
    /// </summary>
    public interface IAuditService
    {
        Task<string> RecordEventAsync(AuditEvent auditEvent);
        Task<IEnumerable<AuditEvent>> GetEventsByResourceAsync(string resourceId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<IEnumerable<AuditEvent>> GetEventsByUserAsync(string userId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<IEnumerable<AuditEvent>> GetEventsByTypeAsync(string eventType, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<IEnumerable<AuditEvent>> SearchEventsAsync(AuditEventFilter filter, int limit = 100);
    }
}
