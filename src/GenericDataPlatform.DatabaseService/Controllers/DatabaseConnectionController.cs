using System;
using System.Threading.Tasks;
using GenericDataPlatform.DatabaseService.Repositories;
using GenericDataPlatform.DatabaseService.Services.SchemaEvolution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.DatabaseService.Controllers
{
    [ApiController]
    [Route("api/database-connections")]
    public class DatabaseConnectionController : ControllerBase
    {
        private readonly DbRepositoryFactory _repositoryFactory;
        private readonly DatabaseOptions _databaseOptions;
        private readonly ILogger<DatabaseConnectionController> _logger;
        
        public DatabaseConnectionController(
            DbRepositoryFactory repositoryFactory,
            IOptions<DatabaseOptions> databaseOptions,
            ILogger<DatabaseConnectionController> logger)
        {
            _repositoryFactory = repositoryFactory;
            _databaseOptions = databaseOptions.Value;
            _logger = logger;
        }
        
        [HttpGet("test/{databaseType}")]
        public async Task<ActionResult> TestConnection(string databaseType)
        {
            try
            {
                // Parse the database type
                if (!Enum.TryParse<DatabaseType>(databaseType, true, out var dbType))
                {
                    return BadRequest($"Invalid database type: {databaseType}. Valid types are: PostgreSQL, SQLServer, MySQL");
                }
                
                // Get the appropriate repository
                var repository = _repositoryFactory.CreateRepository(dbType);
                
                // Test the connection by executing a simple query
                var result = await repository.QueryAsync("SELECT 1 AS TestResult");
                
                return Ok(new { Success = true, Message = $"Successfully connected to {dbType} database" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to {databaseType}", databaseType);
                return StatusCode(500, new { Success = false, Message = $"Error connecting to database: {ex.Message}" });
            }
        }
        
        [HttpGet("current")]
        public ActionResult GetCurrentDatabaseType()
        {
            return Ok(new { DatabaseType = _databaseOptions.DefaultDatabaseType.ToString() });
        }
        
        [HttpPost("set-default")]
        public ActionResult SetDefaultDatabaseType([FromBody] SetDefaultDatabaseTypeRequest request)
        {
            try
            {
                // Parse the database type
                if (!Enum.TryParse<DatabaseType>(request.DatabaseType, true, out var dbType))
                {
                    return BadRequest($"Invalid database type: {request.DatabaseType}. Valid types are: PostgreSQL, SQLServer, MySQL");
                }
                
                // In a real implementation, this would update the configuration
                // For this example, we'll just return a message
                return Ok(new { 
                    Success = true, 
                    Message = $"Default database type would be set to {dbType}. " +
                              "Note: In this implementation, the setting is not persisted and requires a restart to take effect." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default database type to {databaseType}", request.DatabaseType);
                return StatusCode(500, new { Success = false, Message = $"Error setting default database type: {ex.Message}" });
            }
        }
    }
    
    public class SetDefaultDatabaseTypeRequest
    {
        public string DatabaseType { get; set; }
    }
}
