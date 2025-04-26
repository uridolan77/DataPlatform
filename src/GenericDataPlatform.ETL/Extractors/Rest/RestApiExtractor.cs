using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Extractors.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Extractors.Rest
{
    public class RestApiExtractor : IExtractor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RestApiExtractor> _logger;
        
        public string Type => "RestApi";
        
        public RestApiExtractor(IHttpClientFactory httpClientFactory, ILogger<RestApiExtractor> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        
        public async Task<object> ExtractAsync(Dictionary<string, object> configuration, DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Get configuration values
                if (!configuration.TryGetValue("url", out var urlObj))
                {
                    throw new ArgumentException("URL is required for REST API extraction");
                }
                
                var url = urlObj.ToString();
                
                // Get HTTP method (default to GET)
                configuration.TryGetValue("method", out var methodObj);
                var method = methodObj?.ToString() ?? "GET";
                
                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                
                // Set default headers
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Apply authentication if specified
                ApplyAuthentication(client, configuration);
                
                // Add query parameters if any
                if (parameters != null && parameters.Count > 0 && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    var queryParams = new List<string>();
                    
                    foreach (var param in parameters)
                    {
                        if (param.Value != null)
                        {
                            queryParams.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value.ToString())}");
                        }
                    }
                    
                    if (queryParams.Count > 0)
                    {
                        url += (url.Contains("?") ? "&" : "?") + string.Join("&", queryParams);
                    }
                }
                
                // Make the request
                HttpResponseMessage response;
                
                if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    response = await client.GetAsync(url);
                }
                else if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if there's a request body
                    HttpContent content = null;
                    
                    if (parameters != null && parameters.TryGetValue("body", out var bodyObj) && bodyObj != null)
                    {
                        var body = bodyObj.ToString();
                        content = new StringContent(body, Encoding.UTF8, "application/json");
                    }
                    
                    response = await client.PostAsync(url, content);
                }
                else
                {
                    throw new NotSupportedException($"HTTP method {method} is not supported");
                }
                
                response.EnsureSuccessStatusCode();
                
                // Read the response
                var contentString = await response.Content.ReadAsStringAsync();
                
                // Parse the response based on the content type
                var contentType = response.Content.Headers.ContentType?.MediaType;
                
                if (contentType != null && contentType.Contains("json"))
                {
                    return ParseJsonResponse(contentString, configuration, source);
                }
                else
                {
                    // For non-JSON responses, return the raw content
                    return new List<DataRecord>
                    {
                        new DataRecord
                        {
                            Id = Guid.NewGuid().ToString(),
                            SchemaId = source.Schema?.Id,
                            SourceId = source.Id,
                            Data = new Dictionary<string, object>
                            {
                                { "content", contentString }
                            },
                            Metadata = new Dictionary<string, string>
                            {
                                { "contentType", contentType ?? "text/plain" },
                                { "url", url },
                                { "method", method }
                            },
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = "1.0"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data from REST API");
                throw;
            }
        }
        
        private void ApplyAuthentication(HttpClient client, Dictionary<string, object> configuration)
        {
            if (configuration.TryGetValue("authType", out var authTypeObj) && authTypeObj != null)
            {
                var authType = authTypeObj.ToString();
                
                switch (authType.ToLowerInvariant())
                {
                    case "basic":
                        if (configuration.TryGetValue("username", out var usernameObj) && 
                            configuration.TryGetValue("password", out var passwordObj))
                        {
                            var username = usernameObj.ToString();
                            var password = passwordObj.ToString();
                            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                        }
                        break;
                    
                    case "bearer":
                        if (configuration.TryGetValue("token", out var tokenObj))
                        {
                            var token = tokenObj.ToString();
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }
                        break;
                    
                    case "apikey":
                        if (configuration.TryGetValue("apiKey", out var apiKeyObj))
                        {
                            var apiKey = apiKeyObj.ToString();
                            
                            if (configuration.TryGetValue("apiKeyHeader", out var apiKeyHeaderObj))
                            {
                                var apiKeyHeader = apiKeyHeaderObj.ToString();
                                client.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
                            }
                            else
                            {
                                // Default to X-API-Key header
                                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                            }
                        }
                        break;
                }
            }
        }
        
        private List<DataRecord> ParseJsonResponse(string content, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                var records = new List<DataRecord>();
                
                // Parse the JSON
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                // Check if we need to extract a specific property
                if (configuration.TryGetValue("jsonPath", out var jsonPathObj) && jsonPathObj != null)
                {
                    var jsonPath = jsonPathObj.ToString();
                    
                    // Navigate to the specified path
                    var pathSegments = jsonPath.Split('.');
                    var current = root;
                    
                    foreach (var segment in pathSegments)
                    {
                        if (current.ValueKind != JsonValueKind.Object)
                        {
                            throw new InvalidOperationException($"Cannot navigate to {segment} in {current.ValueKind}");
                        }
                        
                        if (!current.TryGetProperty(segment, out var next))
                        {
                            throw new InvalidOperationException($"Property {segment} not found in JSON");
                        }
                        
                        current = next;
                    }
                    
                    // Use the current element as the root
                    root = current;
                }
                
                // If the root is an array, process each item
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var record = CreateDataRecordFromJsonElement(item, source);
                        records.Add(record);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    // If the root is an object, create a single record
                    var record = CreateDataRecordFromJsonElement(root, source);
                    records.Add(record);
                }
                else
                {
                    // For primitive values, create a single record with a value property
                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = new Dictionary<string, object>
                        {
                            { "value", GetValueFromJsonElement(root) }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "contentType", "application/json" }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };
                    
                    records.Add(record);
                }
                
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON response");
                throw;
            }
        }
        
        private DataRecord CreateDataRecordFromJsonElement(JsonElement element, DataSourceDefinition source)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("JSON element must be an object");
            }
            
            var data = new Dictionary<string, object>();
            
            foreach (var property in element.EnumerateObject())
            {
                data[property.Name] = GetValueFromJsonElement(property.Value);
            }
            
            // Extract ID field if specified
            string id = Guid.NewGuid().ToString();
            if (source.ConnectionProperties.TryGetValue("idField", out var idField) && 
                data.TryGetValue(idField, out var idValue))
            {
                id = idValue.ToString();
            }
            
            return new DataRecord
            {
                Id = id,
                SchemaId = source.Schema?.Id,
                SourceId = source.Id,
                Data = data,
                Metadata = new Dictionary<string, string>
                {
                    { "contentType", "application/json" }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }
        
        private object GetValueFromJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                    {
                        return intValue;
                    }
                    else if (element.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    else if (element.TryGetDouble(out var doubleValue))
                    {
                        return doubleValue;
                    }
                    else
                    {
                        return element.GetRawText();
                    }
                
                case JsonValueKind.True:
                    return true;
                
                case JsonValueKind.False:
                    return false;
                
                case JsonValueKind.Null:
                    return null;
                
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = GetValueFromJsonElement(property.Value);
                    }
                    return obj;
                
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(GetValueFromJsonElement(item));
                    }
                    return array;
                
                default:
                    return null;
            }
        }
    }
}
