using System;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Lineage;
using GenericDataPlatform.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Security.Controllers
{
    [ApiController]
    [Route("api/security/lineage")]
    [Authorize(Roles = "Admin,DataEngineer,DataScientist")]
    public class DataLineageController : ControllerBase
    {
        private readonly IDataLineageService _lineageService;
        private readonly ILogger<DataLineageController> _logger;

        public DataLineageController(IDataLineageService lineageService, ILogger<DataLineageController> logger)
        {
            _lineageService = lineageService;
            _logger = logger;
        }

        /// <summary>
        /// Records a data lineage event
        /// </summary>
        [HttpPost("event")]
        public async Task<IActionResult> RecordLineageEvent([FromBody] LineageEvent lineageEvent)
        {
            try
            {
                _logger.LogInformation("Recording lineage event: {EventType} for {SourceId} to {TargetId}",
                    lineageEvent.EventType, lineageEvent.SourceId, lineageEvent.TargetId);
                
                var eventId = await _lineageService.RecordLineageEventAsync(lineageEvent);
                
                return Ok(new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording lineage event");
                return StatusCode(500, new { error = "Error recording lineage event", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets lineage events for a data entity
        /// </summary>
        [HttpGet("events/{entityId}")]
        public async Task<IActionResult> GetLineageEvents(string entityId, [FromQuery] LineageDirection direction = LineageDirection.Both, [FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting lineage events for entity {EntityId} with direction {Direction}", entityId, direction);
                
                var events = await _lineageService.GetLineageEventsAsync(entityId, direction, startTime, endTime, limit);
                
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage events for entity {EntityId}", entityId);
                return StatusCode(500, new { error = "Error getting lineage events", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets the lineage graph for a data entity
        /// </summary>
        [HttpGet("graph/{entityId}")]
        public async Task<IActionResult> GetLineageGraph(string entityId, [FromQuery] LineageDirection direction = LineageDirection.Both, [FromQuery] int depth = 3)
        {
            try
            {
                _logger.LogInformation("Getting lineage graph for entity {EntityId} with direction {Direction} and depth {Depth}",
                    entityId, direction, depth);
                
                var graph = await _lineageService.GetLineageGraphAsync(entityId, direction, depth);
                
                return Ok(graph);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage graph for entity {EntityId}", entityId);
                return StatusCode(500, new { error = "Error getting lineage graph", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets the impact analysis for a data entity
        /// </summary>
        [HttpGet("impact/{entityId}")]
        public async Task<IActionResult> GetImpactAnalysis(string entityId, [FromQuery] int depth = 3)
        {
            try
            {
                _logger.LogInformation("Getting impact analysis for entity {EntityId} with depth {Depth}", entityId, depth);
                
                var analysis = await _lineageService.GetImpactAnalysisAsync(entityId, depth);
                
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impact analysis for entity {EntityId}", entityId);
                return StatusCode(500, new { error = "Error getting impact analysis", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets data entities by type
        /// </summary>
        [HttpGet("entities/type/{entityType}")]
        public async Task<IActionResult> GetDataEntitiesByType(string entityType, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting data entities of type {EntityType}", entityType);
                
                var entities = await _lineageService.GetDataEntitiesByTypeAsync(entityType, limit);
                
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entities of type {EntityType}", entityType);
                return StatusCode(500, new { error = "Error getting data entities", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for data entities
        /// </summary>
        [HttpGet("entities/search")]
        public async Task<IActionResult> SearchDataEntities([FromQuery] string searchTerm, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for data entities with term {SearchTerm}", searchTerm);
                
                var entities = await _lineageService.SearchDataEntitiesAsync(searchTerm, limit);
                
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data entities with term {SearchTerm}", searchTerm);
                return StatusCode(500, new { error = "Error searching for data entities", detail = ex.Message });
            }
        }
    }
}
