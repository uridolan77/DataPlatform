using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.Rest
{
    public class RestApiConnector : BaseConnector
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RestApiConnector(IHttpClientFactory httpClientFactory, ILogger<RestApiConnector> logger)
            : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        public override async Task<bool> ValidateConnectionAsync(DataSourceDefinition source)
        {
            try
            {
                // Extract connection properties
                if (!source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl))
                {
                    throw new ArgumentException("Base URL is required for REST API connection");
                }

                // Create HTTP client
                var client = CreateHttpClient(source);

                // Make a test request to the base URL or health endpoint
                var testUrl = baseUrl;
                if (source.ConnectionProperties.TryGetValue("healthEndpoint", out var healthEndpoint))
                {
                    testUrl = CombineUrls(baseUrl, healthEndpoint);
                }

                var response = await client.GetAsync(testUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error validating connection to REST API {source}", source.Name);
                return false;
            }
        }

        public override async Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Extract connection properties
                if (!source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl))
                {
                    throw new ArgumentException("Base URL is required for REST API connection");
                }

                if (!source.ConnectionProperties.TryGetValue("endpoint", out var endpoint))
                {
                    throw new ArgumentException("Endpoint is required for REST API connection");
                }

                // Create HTTP client
                var client = CreateHttpClient(source);

                // Build the request URL with parameters
                var requestUrl = BuildRequestUrl(baseUrl, endpoint, parameters);

                // Get the HTTP method (default to GET)
                source.ConnectionProperties.TryGetValue("method", out var method);
                method = string.IsNullOrEmpty(method) ? "GET" : method.ToUpper();

                // Make the request
                HttpResponseMessage response;

                if (method == "GET")
                {
                    response = await client.GetAsync(requestUrl);
                }
                else if (method == "POST")
                {
                    // Check if there's a request body
                    if (parameters != null && parameters.TryGetValue("body", out var body))
                    {
                        var content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                        response = await client.PostAsync(requestUrl, content);
                    }
                    else
                    {
                        response = await client.PostAsync(requestUrl, null);
                    }
                }
                else
                {
                    throw new NotSupportedException($"HTTP method {method} is not supported");
                }

                response.EnsureSuccessStatusCode();

                // Parse the response
                var content = await response.Content.ReadAsStringAsync();

                // Convert to data records based on response format
                if (source.ConnectionProperties.TryGetValue("responseFormat", out var format))
                {
                    switch (format.ToLowerInvariant())
                    {
                        case "json":
                            return await ParseJsonResponseAsync(content, source);
                        case "xml":
                            return await ParseXmlResponseAsync(content, source);
                        case "csv":
                            return await ParseCsvResponseAsync(content, source);
                        default:
                            throw new NotSupportedException($"Response format {format} is not supported");
                    }
                }

                // Default to JSON
                return await ParseJsonResponseAsync(content, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error fetching data from REST API {source}", source.Name);
                throw;
            }
        }

        public override async Task<IAsyncEnumerable<DataRecord>> StreamDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            // For simplicity, we'll just return the fetched data as an async enumerable
            var records = await FetchDataAsync(source, parameters);
            return records.ToAsyncEnumerable();
        }

        public override async Task<DataSchema> InferSchemaAsync(DataSourceDefinition source)
        {
            try
            {
                // Fetch a sample of data
                var parameters = new Dictionary<string, object>();

                // Add limit parameter if supported
                if (source.ConnectionProperties.TryGetValue("supportsLimit", out var supportsLimit) &&
                    supportsLimit.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    parameters["limit"] = 10;
                }

                var sampleData = await FetchDataAsync(source, parameters);

                // Infer schema from the sample data
                return InferSchemaFromSample(sampleData, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error inferring schema for REST API {source}", source.Name);
                throw;
            }
        }

        public override async Task<DataIngestCheckpoint> GetLatestCheckpointAsync(string sourceId)
        {
            // In a real implementation, this would retrieve the checkpoint from a persistent store
            // For demonstration, we'll return a dummy checkpoint
            return await Task.FromResult(new DataIngestCheckpoint
            {
                SourceId = sourceId,
                CheckpointValue = DateTime.UtcNow.AddDays(-1).ToString("o"),
                ProcessedAt = DateTime.UtcNow.AddDays(-1),
                RecordsProcessed = 0,
                AdditionalInfo = new Dictionary<string, string>()
            });
        }

        public override async Task SaveCheckpointAsync(DataIngestCheckpoint checkpoint)
        {
            // In a real implementation, this would save the checkpoint to a persistent store
            await Task.CompletedTask;
        }

        private HttpClient CreateHttpClient(DataSourceDefinition source)
        {
            var client = _httpClientFactory.CreateClient();

            // Set default headers
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Apply authentication if needed
            ApplyAuthentication(client, source);

            // Set timeout if specified
            if (source.ConnectionProperties.TryGetValue("timeoutSeconds", out var timeoutStr) &&
                int.TryParse(timeoutStr, out var timeout))
            {
                client.Timeout = TimeSpan.FromSeconds(timeout);
            }

            return client;
        }

        private void ApplyAuthentication(HttpClient client, DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("authType", out var authType))
            {
                return;
            }

            switch (authType.ToLowerInvariant())
            {
                case "basic":
                    if (source.ConnectionProperties.TryGetValue("username", out var username) &&
                        source.ConnectionProperties.TryGetValue("password", out var password))
                    {
                        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    }
                    break;

                case "bearer":
                    if (source.ConnectionProperties.TryGetValue("token", out var token))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    break;

                case "apikey":
                    if (source.ConnectionProperties.TryGetValue("apiKey", out var apiKey))
                    {
                        if (source.ConnectionProperties.TryGetValue("apiKeyHeader", out var apiKeyHeader))
                        {
                            client.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
                        }
                        else if (source.ConnectionProperties.TryGetValue("apiKeyQueryParam", out var apiKeyQueryParam))
                        {
                            // This will be handled in BuildRequestUrl
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

        private string BuildRequestUrl(string baseUrl, string endpoint, Dictionary<string, object> parameters)
        {
            var url = CombineUrls(baseUrl, endpoint);

            if (parameters == null || parameters.Count == 0)
            {
                return url;
            }

            // Check if we need to add an API key as a query parameter
            var queryParams = new List<string>();

            if (parameters.ContainsKey("apiKeyQueryParam") && parameters.ContainsKey("apiKey"))
            {
                queryParams.Add($"{parameters["apiKeyQueryParam"]}={WebUtility.UrlEncode(parameters["apiKey"].ToString())}");
            }

            // Add other parameters
            foreach (var param in parameters)
            {
                // Skip special parameters
                if (param.Key == "body" || param.Key == "apiKeyQueryParam" || param.Key == "apiKey")
                {
                    continue;
                }

                queryParams.Add($"{WebUtility.UrlEncode(param.Key)}={WebUtility.UrlEncode(param.Value?.ToString())}");
            }

            if (queryParams.Count == 0)
            {
                return url;
            }

            return url + (url.Contains("?") ? "&" : "?") + string.Join("&", queryParams);
        }

        private string CombineUrls(string baseUrl, string relativePath)
        {
            baseUrl = baseUrl.TrimEnd('/');
            relativePath = relativePath.TrimStart('/');
            return $"{baseUrl}/{relativePath}";
        }

        private async Task<IEnumerable<DataRecord>> ParseJsonResponseAsync(string content, DataSourceDefinition source)
        {
            try
            {
                // Parse the JSON
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                // Check if we need to extract a specific property
                string dataPath = null;
                if (source.ConnectionProperties.TryGetValue("jsonDataPath", out var path))
                {
                    dataPath = path;
                }

                // Get the array of items
                JsonElement dataArray;

                if (!string.IsNullOrEmpty(dataPath))
                {
                    // Navigate to the specified path
                    var pathSegments = dataPath.Split('.');
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

                    dataArray = current;
                }
                else
                {
                    // Use the root element
                    dataArray = root;
                }

                // If the data is an object, wrap it in an array
                if (dataArray.ValueKind == JsonValueKind.Object)
                {
                    return new[] { ConvertJsonObjectToDataRecord(dataArray, source) };
                }

                // If the data is an array, process each item
                if (dataArray.ValueKind == JsonValueKind.Array)
                {
                    var records = new List<DataRecord>();

                    foreach (var item in dataArray.EnumerateArray())
                    {
                        records.Add(ConvertJsonObjectToDataRecord(item, source));
                    }

                    return records;
                }

                throw new InvalidOperationException($"Expected JSON array or object, but got {dataArray.ValueKind}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error parsing JSON response");
                throw;
            }
        }

        private DataRecord ConvertJsonObjectToDataRecord(JsonElement jsonObject, DataSourceDefinition source)
        {
            var data = new Dictionary<string, object>();
            var metadata = new Dictionary<string, string>();

            // Extract ID field if specified
            string id = Guid.NewGuid().ToString();
            if (source.ConnectionProperties.TryGetValue("idField", out var idField) &&
                jsonObject.TryGetProperty(idField, out var idValue))
            {
                id = idValue.ToString();
            }

            // Process all properties
            foreach (var property in jsonObject.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        data[property.Name] = property.Value.GetString();
                        break;

                    case JsonValueKind.Number:
                        if (property.Value.TryGetInt32(out var intValue))
                        {
                            data[property.Name] = intValue;
                        }
                        else if (property.Value.TryGetInt64(out var longValue))
                        {
                            data[property.Name] = longValue;
                        }
                        else if (property.Value.TryGetDouble(out var doubleValue))
                        {
                            data[property.Name] = doubleValue;
                        }
                        else
                        {
                            data[property.Name] = property.Value.GetRawText();
                        }
                        break;

                    case JsonValueKind.True:
                        data[property.Name] = true;
                        break;

                    case JsonValueKind.False:
                        data[property.Name] = false;
                        break;

                    case JsonValueKind.Null:
                        data[property.Name] = null;
                        break;

                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        // For complex types, store the JSON string
                        data[property.Name] = property.Value.GetRawText();
                        break;
                }
            }

            // Add metadata
            metadata["source"] = "REST API";
            metadata["sourceUrl"] = source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl) ? baseUrl : "Unknown";

            return new DataRecord
            {
                Id = id,
                SchemaId = source.Schema?.Id,
                SourceId = source.Id,
                Data = data,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        private Task<IEnumerable<DataRecord>> ParseXmlResponseAsync(string content, DataSourceDefinition source)
        {
            try
            {
                // Parse the XML
                var xmlDoc = XDocument.Parse(content);

                // Check if we need to extract a specific element
                string elementPath = null;
                if (source.ConnectionProperties.TryGetValue("xmlElementPath", out var path))
                {
                    elementPath = path;
                }

                // Get the elements to process
                IEnumerable<XElement> elements;

                if (!string.IsNullOrEmpty(elementPath))
                {
                    // Navigate to the specified path
                    var pathSegments = elementPath.Split('/');
                    var current = xmlDoc.Root;

                    foreach (var segment in pathSegments)
                    {
                        if (current == null)
                        {
                            throw new InvalidOperationException($"Cannot navigate to {segment} in XML");
                        }

                        // Handle namespace if present
                        if (segment.Contains(':'))
                        {
                            var parts = segment.Split(':');
                            var ns = current.GetNamespaceOfPrefix(parts[0]);
                            if (ns != null)
                            {
                                current = current.Element(ns + parts[1]);
                            }
                            else
                            {
                                current = current.Element(segment);
                            }
                        }
                        else
                        {
                            current = current.Element(segment);
                        }

                        if (current == null)
                        {
                            throw new InvalidOperationException($"Element {segment} not found in XML");
                        }
                    }

                    // Get the elements to process (either the current element or its children)
                    var rootElementName = source.ConnectionProperties.TryGetValue("xmlRootElement", out var rootElement) ?
                        rootElement : null;

                    if (!string.IsNullOrEmpty(rootElementName))
                    {
                        elements = current.Elements(rootElementName);
                    }
                    else
                    {
                        elements = new[] { current };
                    }
                }
                else
                {
                    // Get the root element's children or the root itself
                    var rootElementName = source.ConnectionProperties.TryGetValue("xmlRootElement", out var rootElement) ?
                        rootElement : null;

                    if (!string.IsNullOrEmpty(rootElementName))
                    {
                        elements = xmlDoc.Root.Elements(rootElementName);
                    }
                    else
                    {
                        elements = new[] { xmlDoc.Root };
                    }
                }

                // Process each element
                var records = new List<DataRecord>();

                foreach (var element in elements)
                {
                    records.Add(ConvertXmlElementToDataRecord(element, source));
                }

                return Task.FromResult<IEnumerable<DataRecord>>(records);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error parsing XML response");
                throw;
            }
        }

        private DataRecord ConvertXmlElementToDataRecord(XElement element, DataSourceDefinition source)
        {
            var data = new Dictionary<string, object>();
            var metadata = new Dictionary<string, string>();

            // Extract ID attribute if specified
            string id = Guid.NewGuid().ToString();
            if (source.ConnectionProperties.TryGetValue("idAttribute", out var idAttribute) &&
                element.Attribute(idAttribute) != null)
            {
                id = element.Attribute(idAttribute).Value;
            }

            // Process all attributes
            foreach (var attribute in element.Attributes())
            {
                data[attribute.Name.LocalName] = attribute.Value;
            }

            // Process all child elements
            foreach (var child in element.Elements())
            {
                // If the child has child elements, convert to JSON
                if (child.Elements().Any() || child.Attributes().Any())
                {
                    data[child.Name.LocalName] = child.ToString();
                }
                else
                {
                    // Try to parse the value based on its content
                    var value = child.Value;

                    if (int.TryParse(value, out var intValue))
                    {
                        data[child.Name.LocalName] = intValue;
                    }
                    else if (double.TryParse(value, out var doubleValue))
                    {
                        data[child.Name.LocalName] = doubleValue;
                    }
                    else if (bool.TryParse(value, out var boolValue))
                    {
                        data[child.Name.LocalName] = boolValue;
                    }
                    else if (DateTime.TryParse(value, out var dateValue))
                    {
                        data[child.Name.LocalName] = dateValue;
                    }
                    else
                    {
                        data[child.Name.LocalName] = value;
                    }
                }
            }

            // Add metadata
            metadata["source"] = "REST API";
            metadata["sourceUrl"] = source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl) ? baseUrl : "Unknown";
            metadata["format"] = "XML";

            return new DataRecord
            {
                Id = id,
                SchemaId = source.Schema?.Id,
                SourceId = source.Id,
                Data = data,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        private Task<IEnumerable<DataRecord>> ParseCsvResponseAsync(string content, DataSourceDefinition source)
        {
            try
            {
                var records = new List<DataRecord>();

                // Configure CSV reader
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null
                };

                // Check if we need to use a different delimiter
                if (source.ConnectionProperties.TryGetValue("csvDelimiter", out var delimiter) &&
                    !string.IsNullOrEmpty(delimiter))
                {
                    config.Delimiter = delimiter;
                }

                using (var reader = new StringReader(content))
                using (var csv = new CsvReader(reader, config))
                {
                    // Read all records
                    var csvRecords = csv.GetRecords<dynamic>().ToList();

                    // Convert each CSV record to a DataRecord
                    foreach (var csvRecord in csvRecords)
                    {
                        var data = new Dictionary<string, object>();
                        var metadata = new Dictionary<string, string>();

                        // Extract all fields from the dynamic object
                        foreach (var property in (IDictionary<string, object>)csvRecord)
                        {
                            var value = property.Value;

                            // Try to parse the value based on its content
                            if (value is string stringValue)
                            {
                                if (int.TryParse(stringValue, out var intValue))
                                {
                                    data[property.Key] = intValue;
                                }
                                else if (double.TryParse(stringValue, out var doubleValue))
                                {
                                    data[property.Key] = doubleValue;
                                }
                                else if (bool.TryParse(stringValue, out var boolValue))
                                {
                                    data[property.Key] = boolValue;
                                }
                                else if (DateTime.TryParse(stringValue, out var dateValue))
                                {
                                    data[property.Key] = dateValue;
                                }
                                else
                                {
                                    data[property.Key] = stringValue;
                                }
                            }
                            else
                            {
                                data[property.Key] = value;
                            }
                        }

                        // Generate a unique ID
                        string id = Guid.NewGuid().ToString();

                        // Check if we need to use a specific field as ID
                        if (source.ConnectionProperties.TryGetValue("idField", out var idField) &&
                            data.ContainsKey(idField))
                        {
                            id = data[idField].ToString();
                        }

                        // Add metadata
                        metadata["source"] = "REST API";
                        metadata["sourceUrl"] = source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl) ? baseUrl : "Unknown";
                        metadata["format"] = "CSV";

                        records.Add(new DataRecord
                        {
                            Id = id,
                            SchemaId = source.Schema?.Id,
                            SourceId = source.Id,
                            Data = data,
                            Metadata = metadata,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = "1.0"
                        });
                    }
                }

                return Task.FromResult<IEnumerable<DataRecord>>(records);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error parsing CSV response");
                throw;
            }
        }

        private DataSchema InferSchemaFromSample(IEnumerable<DataRecord> sampleData, DataSourceDefinition source)
        {
            // Create a new schema
            var schema = new DataSchema
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{source.Name} Schema",
                Description = $"Inferred schema for {source.Name}",
                Type = SchemaType.Dynamic,
                Fields = new List<SchemaField>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = new SchemaVersion
                {
                    VersionNumber = "1.0",
                    EffectiveDate = DateTime.UtcNow,
                    PreviousVersion = null,
                    ChangeDescription = "Inferred from sample data"
                }
            };

            // If there's no sample data, return an empty schema
            if (!sampleData.Any())
            {
                return schema;
            }

            // Get all unique field names from the sample data
            var fieldNames = new HashSet<string>();
            foreach (var record in sampleData)
            {
                foreach (var key in record.Data.Keys)
                {
                    fieldNames.Add(key);
                }
            }

            // For each field, determine its type and other properties
            foreach (var fieldName in fieldNames)
            {
                var field = new SchemaField
                {
                    Name = fieldName,
                    Description = $"Field {fieldName}",
                    IsRequired = IsFieldRequired(fieldName, sampleData),
                    IsArray = IsFieldArray(fieldName, sampleData),
                    Type = InferFieldType(fieldName, sampleData),
                    DefaultValue = null,
                    Validation = new ValidationRules(),
                    NestedFields = new List<SchemaField>()
                };

                schema.Fields.Add(field);
            }

            return schema;
        }

        private bool IsFieldRequired(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // A field is required if it exists in all records and is never null
            return sampleData.All(record =>
                record.Data.TryGetValue(fieldName, out var value) && value != null);
        }

        private bool IsFieldArray(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // Check if any value for this field is an array
            foreach (var record in sampleData)
            {
                if (record.Data.TryGetValue(fieldName, out var value) && value is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        return true;
                    }
                }
                else if (record.Data.TryGetValue(fieldName, out value) && value is Array)
                {
                    return true;
                }
            }

            return false;
        }

        private FieldType InferFieldType(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // Get all non-null values for this field
            var values = sampleData
                .Where(record => record.Data.TryGetValue(fieldName, out var value) && value != null)
                .Select(record => record.Data[fieldName])
                .ToList();

            if (!values.Any())
            {
                return FieldType.String; // Default to string for empty fields
            }

            // Check if all values are of the same type
            var firstValue = values.First();
            var firstType = GetValueType(firstValue);

            if (values.All(value => GetValueType(value) == firstType))
            {
                return firstType;
            }

            // If types are mixed, use the most general type (string)
            return FieldType.String;
        }

        private FieldType GetValueType(object value)
        {
            if (value is string)
            {
                // Try to parse as DateTime
                if (DateTime.TryParse(value.ToString(), out _))
                {
                    return FieldType.DateTime;
                }

                return FieldType.String;
            }
            else if (value is int || value is long)
            {
                return FieldType.Integer;
            }
            else if (value is float || value is double || value is decimal)
            {
                return FieldType.Decimal;
            }
            else if (value is bool)
            {
                return FieldType.Boolean;
            }
            else if (value is DateTime)
            {
                return FieldType.DateTime;
            }
            else if (value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        // Try to parse as DateTime
                        if (DateTime.TryParse(jsonElement.GetString(), out _))
                        {
                            return FieldType.DateTime;
                        }
                        return FieldType.String;

                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out _) || jsonElement.TryGetInt64(out _))
                        {
                            return FieldType.Integer;
                        }
                        return FieldType.Decimal;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return FieldType.Boolean;

                    case JsonValueKind.Object:
                        return FieldType.Complex;

                    case JsonValueKind.Array:
                        return FieldType.Complex;

                    default:
                        return FieldType.String;
                }
            }

            // For complex objects, use the JSON type
            return FieldType.Complex;
        }
    }
}
