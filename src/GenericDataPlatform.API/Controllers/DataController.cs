using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.API.Models;
using GenericDataPlatform.API.Services;
using GenericDataPlatform.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.API.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class DataController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly ILogger<DataController> _logger;
        
        public DataController(IDataService dataService, ILogger<DataController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }
        
        [HttpGet("sources")]
        public async Task<ActionResult<IEnumerable<DataSourceDefinition>>> GetSources()
        {
            try
            {
                var sources = await _dataService.GetDataSourcesAsync();
                return Ok(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data sources");
                return StatusCode(500, "An error occurred while retrieving data sources");
            }
        }
        
        [HttpGet("sources/{id}")]
        public async Task<ActionResult<DataSourceDefinition>> GetSource(string id)
        {
            try
            {
                var source = await _dataService.GetDataSourceAsync(id);
                if (source == null)
                    return NotFound();
                return Ok(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data source {id}", id);
                return StatusCode(500, $"An error occurred while retrieving data source {id}");
            }
        }
        
        [HttpPost("sources")]
        public async Task<ActionResult<DataSourceDefinition>> CreateSource(DataSourceDefinition source)
        {
            try
            {
                var result = await _dataService.CreateDataSourceAsync(source);
                return CreatedAtAction(nameof(GetSource), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data source");
                return StatusCode(500, "An error occurred while creating the data source");
            }
        }
        
        [HttpGet("sources/{sourceId}/schema")]
        public async Task<ActionResult<DataSchema>> GetSchema(string sourceId)
        {
            try
            {
                var schema = await _dataService.GetSchemaAsync(sourceId);
                if (schema == null)
                    return NotFound();
                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema for source {sourceId}", sourceId);
                return StatusCode(500, $"An error occurred while retrieving schema for source {sourceId}");
            }
        }
        
        [HttpGet("records")]
        public async Task<ActionResult<PagedResult<DataRecord>>> GetRecords(
            [FromQuery] string sourceId, 
            [FromQuery] Dictionary<string, string> filters,
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _dataService.GetRecordsAsync(sourceId, filters, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving records for source {sourceId}", sourceId);
                return StatusCode(500, $"An error occurred while retrieving records for source {sourceId}");
            }
        }
        
        [HttpGet("records/{id}")]
        public async Task<ActionResult<DataRecord>> GetRecord(string id)
        {
            try
            {
                var record = await _dataService.GetRecordAsync(id);
                if (record == null)
                    return NotFound();
                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving record {id}", id);
                return StatusCode(500, $"An error occurred while retrieving record {id}");
            }
        }
        
        [HttpPost("query")]
        public async Task<ActionResult<QueryResult>> Query(DataQuery query)
        {
            try
            {
                var result = await _dataService.QueryAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query");
                return StatusCode(500, "An error occurred while executing the query");
            }
        }
    }
}
