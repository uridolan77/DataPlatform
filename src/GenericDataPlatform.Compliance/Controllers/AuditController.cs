using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.Auditing;
using GenericDataPlatform.Compliance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Compliance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Auditor")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Records an audit event
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecordEvent([FromBody] AuditEvent auditEvent)
        {
            try
            {
                // Set user ID if not provided
                if (string.IsNullOrEmpty(auditEvent.UserId))
                {
                    auditEvent.UserId = User.Identity?.Name;
                }
                
                // Set IP address if not provided
                if (string.IsNullOrEmpty(auditEvent.IpAddress))
                {
                    auditEvent.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                }
                
                // Set user agent if not provided
                if (string.IsNullOrEmpty(auditEvent.UserAgent))
                {
                    auditEvent.UserAgent = Request.Headers["User-Agent"].ToString();
                }
                
                var eventId = await _auditService.RecordEventAsync(auditEvent);
                return Ok(new { Id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording audit event");
                return StatusCode(500, "Error recording audit event");
            }
        }

        /// <summary>
        /// Gets audit events by resource ID
        /// </summary>
        [HttpGet("resource/{resourceId}")]
        public async Task<IActionResult> GetEventsByResource(string resourceId, [FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null, [FromQuery] int limit = 100)
        {
            try
            {
                var events = await _auditService.GetEventsByResourceAsync(resourceId, startTime, endTime, limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit events for resource {ResourceId}", resourceId);
                return StatusCode(500, "Error getting audit events");
            }
        }

        /// <summary>
        /// Gets audit events by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetEventsByUser(string userId, [FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null, [FromQuery] int limit = 100)
        {
            try
            {
                var events = await _auditService.GetEventsByUserAsync(userId, startTime, endTime, limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit events for user {UserId}", userId);
                return StatusCode(500, "Error getting audit events");
            }
        }

        /// <summary>
        /// Gets audit events by event type
        /// </summary>
        [HttpGet("type/{eventType}")]
        public async Task<IActionResult> GetEventsByType(string eventType, [FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null, [FromQuery] int limit = 100)
        {
            try
            {
                var events = await _auditService.GetEventsByTypeAsync(eventType, startTime, endTime, limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit events of type {EventType}", eventType);
                return StatusCode(500, "Error getting audit events");
            }
        }

        /// <summary>
        /// Searches audit events with filters
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchEvents([FromQuery] AuditEventFilter filter, [FromQuery] int limit = 100)
        {
            try
            {
                var events = await _auditService.SearchEventsAsync(filter, limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching audit events");
                return StatusCode(500, "Error searching audit events");
            }
        }
    }
}
