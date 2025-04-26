using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Controllers
{
    [ApiController]
    [Route("api/data-sources")]
    public class DataSourceController : ControllerBase
    {
        private readonly ConnectorFactory _connectorFactory;
        private readonly ILogger<DataSourceController> _logger;
        
        // In a real implementation, this would be stored in a database
        private static readonly Dictionary<string, DataSourceDefinition> _dataSources = new();
        
        public DataSourceController(ConnectorFactory connectorFactory, ILogger<DataSourceController> logger)
        {
            _connectorFactory = connectorFactory;
            _logger = logger;
        }
        
        [HttpGet]
        public ActionResult<IEnumerable<DataSourceDefinition>> GetDataSources()
        {
            return Ok(_dataSources.Values);
        }
        
        [HttpGet("{id}")]
        public ActionResult<DataSourceDefinition> GetDataSource(string id)
        {
            if (!_dataSources.TryGetValue(id, out var source))
            {
                return NotFound();
            }
            
            return Ok(source);
        }
        
        [HttpPost]
        public ActionResult<DataSourceDefinition> CreateDataSource(DataSourceDefinition source)
        {
            if (string.IsNullOrEmpty(source.Id))
            {
                source.Id = Guid.NewGuid().ToString();
            }
            
            if (_dataSources.ContainsKey(source.Id))
            {
                return Conflict($"Data source with ID {source.Id} already exists");
            }
            
            _dataSources[source.Id] = source;
            
            return CreatedAtAction(nameof(GetDataSource), new { id = source.Id }, source);
        }
        
        [HttpPut("{id}")]
        public ActionResult<DataSourceDefinition> UpdateDataSource(string id, DataSourceDefinition source)
        {
            if (!_dataSources.ContainsKey(id))
            {
                return NotFound();
            }
            
            source.Id = id;
            _dataSources[id] = source;
            
            return Ok(source);
        }
        
        [HttpDelete("{id}")]
        public ActionResult DeleteDataSource(string id)
        {
            if (!_dataSources.ContainsKey(id))
            {
                return NotFound();
            }
            
            _dataSources.Remove(id);
            
            return NoContent();
        }
        
        [HttpPost("{id}/validate")]
        public async Task<ActionResult> ValidateConnection(string id)
        {
            if (!_dataSources.TryGetValue(id, out var source))
            {
                return NotFound();
            }
            
            try
            {
                var connector = _connectorFactory.CreateConnector(source);
                var isValid = await connector.ValidateConnectionAsync(source);
                
                if (isValid)
                {
                    return Ok(new { Success = true, Message = "Connection is valid" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Connection is invalid" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating connection for data source {id}", id);
                return StatusCode(500, new { Success = false, Message = $"Error validating connection: {ex.Message}" });
            }
        }
        
        [HttpPost("{id}/infer-schema")]
        public async Task<ActionResult<DataSchema>> InferSchema(string id)
        {
            if (!_dataSources.TryGetValue(id, out var source))
            {
                return NotFound();
            }
            
            try
            {
                var connector = _connectorFactory.CreateConnector(source);
                var schema = await connector.InferSchemaAsync(source);
                
                // Update the data source with the inferred schema
                source.Schema = schema;
                _dataSources[id] = source;
                
                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inferring schema for data source {id}", id);
                return StatusCode(500, new { Success = false, Message = $"Error inferring schema: {ex.Message}" });
            }
        }
        
        [HttpPost("{id}/fetch-data")]
        public async Task<ActionResult<IEnumerable<DataRecord>>> FetchData(string id, [FromBody] Dictionary<string, object> parameters = null)
        {
            if (!_dataSources.TryGetValue(id, out var source))
            {
                return NotFound();
            }
            
            try
            {
                var connector = _connectorFactory.CreateConnector(source);
                var records = await connector.FetchDataAsync(source, parameters);
                
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data from data source {id}", id);
                return StatusCode(500, new { Success = false, Message = $"Error fetching data: {ex.Message}" });
            }
        }
    }
}
