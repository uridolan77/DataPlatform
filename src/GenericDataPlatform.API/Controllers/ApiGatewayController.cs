using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.API.Controllers
{
    [ApiController]
    [Route("api/gateway")]
    public class ApiGatewayController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiGatewayController> _logger;
        
        private readonly Dictionary<string, string> _serviceEndpoints;
        
        public ApiGatewayController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ApiGatewayController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            
            // Initialize service endpoints from configuration
            _serviceEndpoints = new Dictionary<string, string>
            {
                ["ingestion"] = configuration["ServiceEndpoints:IngestionService"],
                ["storage"] = configuration["ServiceEndpoints:StorageService"],
                ["database"] = configuration["ServiceEndpoints:DatabaseService"],
                ["etl"] = configuration["ServiceEndpoints:ETLService"]
            };
        }
        
        [HttpGet("services")]
        public ActionResult<Dictionary<string, string>> GetServices()
        {
            return Ok(_serviceEndpoints);
        }
        
        [HttpGet("{service}/health")]
        public async Task<IActionResult> CheckServiceHealth(string service)
        {
            if (!_serviceEndpoints.TryGetValue(service.ToLowerInvariant(), out var endpoint))
            {
                return NotFound($"Service '{service}' not found");
            }
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{endpoint}/health");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { Status = "Unhealthy", Message = $"Service returned status code {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health of service {Service}", service);
                return StatusCode(500, new { Status = "Unhealthy", Message = ex.Message });
            }
        }
        
        [HttpGet("{service}/{*path}")]
        public async Task<IActionResult> ProxyGet(string service, string path, [FromQuery] Dictionary<string, string> queryParams)
        {
            return await ProxyRequest(HttpMethod.Get, service, path, queryParams, null);
        }
        
        [HttpPost("{service}/{*path}")]
        public async Task<IActionResult> ProxyPost(string service, string path, [FromBody] object body)
        {
            return await ProxyRequest(HttpMethod.Post, service, path, null, body);
        }
        
        [HttpPut("{service}/{*path}")]
        public async Task<IActionResult> ProxyPut(string service, string path, [FromBody] object body)
        {
            return await ProxyRequest(HttpMethod.Put, service, path, null, body);
        }
        
        [HttpDelete("{service}/{*path}")]
        public async Task<IActionResult> ProxyDelete(string service, string path)
        {
            return await ProxyRequest(HttpMethod.Delete, service, path, null, null);
        }
        
        private async Task<IActionResult> ProxyRequest(HttpMethod method, string service, string path, Dictionary<string, string> queryParams, object body)
        {
            if (!_serviceEndpoints.TryGetValue(service.ToLowerInvariant(), out var endpoint))
            {
                return NotFound($"Service '{service}' not found");
            }
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Build the request URI
                var uriBuilder = new UriBuilder($"{endpoint}/{path}");
                
                // Add query parameters if any
                if (queryParams != null && queryParams.Count > 0)
                {
                    var query = new StringBuilder();
                    foreach (var param in queryParams)
                    {
                        if (query.Length > 0)
                        {
                            query.Append('&');
                        }
                        query.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}");
                    }
                    uriBuilder.Query = query.ToString();
                }
                
                // Create the request message
                var request = new HttpRequestMessage(method, uriBuilder.Uri);
                
                // Add the body if any
                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
                
                // Copy headers from the original request
                foreach (var header in Request.Headers)
                {
                    if (!header.Key.StartsWith("Host") && !header.Key.StartsWith("Content-Length"))
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
                
                // Send the request
                var response = await client.SendAsync(request);
                
                // Return the response
                var content = await response.Content.ReadAsStringAsync();
                var result = string.IsNullOrEmpty(content) ? null : JsonSerializer.Deserialize<object>(content);
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proxying request to service {Service}", service);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
