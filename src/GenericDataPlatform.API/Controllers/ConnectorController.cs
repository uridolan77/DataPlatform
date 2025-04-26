using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.API.Controllers
{
    [ApiController]
    [Route("api/connectors")]
    public class ConnectorController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectorController> _logger;
        
        private readonly string _ingestionServiceEndpoint;
        
        public ConnectorController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ConnectorController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            
            _ingestionServiceEndpoint = configuration["ServiceEndpoints:IngestionService"];
        }
        
        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseConnectors()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_ingestionServiceEndpoint}/api/connectors/database");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { Error = $"Service returned status code {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database connectors");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpGet("file-system")]
        public async Task<IActionResult> GetFileSystemConnectors()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_ingestionServiceEndpoint}/api/connectors/file-system");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { Error = $"Service returned status code {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file system connectors");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpGet("streaming")]
        public async Task<IActionResult> GetStreamingConnectors()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_ingestionServiceEndpoint}/api/connectors/streaming");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { Error = $"Service returned status code {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streaming connectors");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpGet("rest")]
        public async Task<IActionResult> GetRestConnectors()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_ingestionServiceEndpoint}/api/connectors/rest");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { Error = $"Service returned status code {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting REST connectors");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateConnection([FromBody] DataSourceDefinition source)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(source);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync($"{_ingestionServiceEndpoint}/api/connectors/validate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(responseContent));
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(responseContent));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating connection");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpPost("infer-schema")]
        public async Task<IActionResult> InferSchema([FromBody] DataSourceDefinition source)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(source);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync($"{_ingestionServiceEndpoint}/api/connectors/infer-schema", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<DataSchema>(responseContent));
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(responseContent));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inferring schema");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpPost("fetch-data")]
        public async Task<IActionResult> FetchData([FromBody] FetchDataRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync($"{_ingestionServiceEndpoint}/api/connectors/fetch-data", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<IEnumerable<DataRecord>>(responseContent));
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(responseContent));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
    
    public class FetchDataRequest
    {
        public DataSourceDefinition Source { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
