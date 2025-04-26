using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.DatabaseService.Services.SchemaEvolution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Controllers
{
    [ApiController]
    [Route("api/schema-evolution")]
    public class SchemaEvolutionController : ControllerBase
    {
        private readonly ISchemaEvolutionService _schemaEvolutionService;
        private readonly ILogger<SchemaEvolutionController> _logger;

        public SchemaEvolutionController(
            ISchemaEvolutionService schemaEvolutionService,
            ILogger<SchemaEvolutionController> logger)
        {
            _schemaEvolutionService = schemaEvolutionService;
            _logger = logger;
        }

        [HttpPost("compare")]
        public async Task<ActionResult<List<SchemaChange>>> CompareSchemas([FromBody] SchemaCompareRequest request)
        {
            try
            {
                var changes = await _schemaEvolutionService.CompareSchemas(request.OldSchema, request.NewSchema);
                return Ok(changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing schemas");
                return StatusCode(500, "An error occurred while comparing schemas");
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<SchemaValidationResult>> ValidateSchemaCompatibility([FromBody] SchemaCompareRequest request)
        {
            try
            {
                var result = await _schemaEvolutionService.ValidateSchemaCompatibility(request.OldSchema, request.NewSchema);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating schema compatibility");
                return StatusCode(500, "An error occurred while validating schema compatibility");
            }
        }

        [HttpPost("migration-plan")]
        public async Task<ActionResult<SchemaMigrationPlan>> GenerateMigrationPlan([FromBody] MigrationPlanRequest request)
        {
            try
            {
                // Validate the database type
                if (!Enum.IsDefined(typeof(DatabaseType), request.DatabaseType))
                {
                    return BadRequest($"Invalid database type: {request.DatabaseType}. Valid types are: PostgreSQL, SQLServer, MySQL");
                }

                var plan = await _schemaEvolutionService.GenerateMigrationPlan(
                    request.OldSchema,
                    request.NewSchema,
                    request.DatabaseType);

                return Ok(plan);
            }
            catch (NotImplementedException ex)
            {
                _logger.LogError(ex, "Database type not supported");
                return BadRequest($"Database type not supported: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating migration plan");
                return StatusCode(500, "An error occurred while generating migration plan");
            }
        }

        [HttpPost("execute")]
        public async Task<ActionResult<SchemaMigrationResult>> ExecuteMigrationPlan([FromBody] ExecuteMigrationRequest request)
        {
            try
            {
                var result = await _schemaEvolutionService.ExecuteMigrationPlan(request.SourceId, request.MigrationPlan);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration plan");
                return StatusCode(500, "An error occurred while executing migration plan");
            }
        }

        [HttpGet("history/{sourceId}")]
        public async Task<ActionResult<List<DataSchema>>> GetSchemaHistory(string sourceId)
        {
            try
            {
                var history = await _schemaEvolutionService.GetSchemaHistory(sourceId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schema history for source {sourceId}", sourceId);
                return StatusCode(500, $"An error occurred while getting schema history for source {sourceId}");
            }
        }

        [HttpGet("version/{sourceId}/{versionNumber}")]
        public async Task<ActionResult<DataSchema>> GetSchemaVersion(string sourceId, string versionNumber)
        {
            try
            {
                var schema = await _schemaEvolutionService.GetSchemaVersion(sourceId, versionNumber);

                if (schema == null)
                {
                    return NotFound($"Schema version {versionNumber} not found for source {sourceId}");
                }

                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schema version {versionNumber} for source {sourceId}", versionNumber, sourceId);
                return StatusCode(500, $"An error occurred while getting schema version {versionNumber} for source {sourceId}");
            }
        }
    }

    public class SchemaCompareRequest
    {
        public DataSchema OldSchema { get; set; }
        public DataSchema NewSchema { get; set; }
    }

    public class MigrationPlanRequest
    {
        public DataSchema OldSchema { get; set; }
        public DataSchema NewSchema { get; set; }
        public DatabaseType DatabaseType { get; set; }
    }

    public class ExecuteMigrationRequest
    {
        public string SourceId { get; set; }
        public SchemaMigrationPlan MigrationPlan { get; set; }
    }
}
