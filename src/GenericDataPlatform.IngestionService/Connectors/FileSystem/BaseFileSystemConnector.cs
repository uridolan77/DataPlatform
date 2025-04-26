using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.FileSystem
{
    public abstract class BaseFileSystemConnector : BaseConnector
    {
        protected BaseFileSystemConnector(ILogger logger) : base(logger)
        {
        }

        public override async Task<bool> ValidateConnectionAsync(DataSourceDefinition source)
        {
            try
            {
                // Check if we can list files
                var files = await ListFilesAsync(source, null);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error validating connection to file system {source}", source.Name);
                return false;
            }
        }

        public override async Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Get file pattern
                if (!source.ConnectionProperties.TryGetValue("filePattern", out var filePattern))
                {
                    filePattern = "*.*"; // Default to all files
                }
                
                // Get file format
                if (!source.ConnectionProperties.TryGetValue("format", out var format))
                {
                    // Try to infer format from file extension
                    format = InferFormatFromPattern(filePattern);
                }
                
                // List files
                var files = await ListFilesAsync(source, filePattern);
                
                // Check if we need to limit the number of files
                if (parameters != null && parameters.TryGetValue("maxFiles", out var maxFilesObj) && 
                    int.TryParse(maxFilesObj.ToString(), out var maxFiles) && maxFiles > 0)
                {
                    files = files.Take(maxFiles).ToList();
                }
                
                // Process each file
                var allRecords = new List<DataRecord>();
                
                foreach (var file in files)
                {
                    // Read file content
                    using var stream = await ReadFileAsync(source, file);
                    
                    // Parse file based on format
                    var records = await ParseFileAsync(stream, format, source);
                    
                    // Add file metadata to each record
                    foreach (var record in records)
                    {
                        record.Metadata["fileName"] = file;
                        record.Metadata["fileFormat"] = format;
                        
                        allRecords.Add(record);
                    }
                }
                
                return allRecords;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error fetching data from file system {source}", source.Name);
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
                // Get file pattern
                if (!source.ConnectionProperties.TryGetValue("filePattern", out var filePattern))
                {
                    filePattern = "*.*"; // Default to all files
                }
                
                // Get file format
                if (!source.ConnectionProperties.TryGetValue("format", out var format))
                {
                    // Try to infer format from file extension
                    format = InferFormatFromPattern(filePattern);
                }
                
                // List files
                var files = await ListFilesAsync(source, filePattern);
                
                if (!files.Any())
                {
                    throw new InvalidOperationException("No files found to infer schema");
                }
                
                // Get the first file
                var firstFile = files.First();
                
                // Read file content
                using var stream = await ReadFileAsync(source, firstFile);
                
                // Parse a sample of the file to infer schema
                var sampleRecords = await ParseFileAsync(stream, format, source, sampleSize: 10);
                
                // Infer schema from the sample records
                return InferSchemaFromSample(sampleRecords, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error inferring schema for file system {source}", source.Name);
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

        protected abstract Task<IEnumerable<string>> ListFilesAsync(DataSourceDefinition source, string filePattern);
        
        protected abstract Task<Stream> ReadFileAsync(DataSourceDefinition source, string filePath);
        
        protected virtual string InferFormatFromPattern(string filePattern)
        {
            var extension = Path.GetExtension(filePattern).ToLowerInvariant();
            
            switch (extension)
            {
                case ".csv":
                    return "csv";
                case ".json":
                    return "json";
                case ".xml":
                    return "xml";
                case ".parquet":
                    return "parquet";
                case ".avro":
                    return "avro";
                case ".txt":
                    return "text";
                default:
                    return "binary";
            }
        }
        
        protected virtual async Task<IEnumerable<DataRecord>> ParseFileAsync(Stream stream, string format, DataSourceDefinition source, int sampleSize = 0)
        {
            switch (format.ToLowerInvariant())
            {
                case "csv":
                    return await ParseCsvAsync(stream, source, sampleSize);
                case "json":
                    return await ParseJsonAsync(stream, source, sampleSize);
                case "xml":
                    return await ParseXmlAsync(stream, source, sampleSize);
                case "text":
                    return await ParseTextAsync(stream, source, sampleSize);
                default:
                    throw new NotSupportedException($"File format {format} is not supported");
            }
        }
        
        protected virtual async Task<IEnumerable<DataRecord>> ParseCsvAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();
            
            // Reset stream position
            stream.Position = 0;
            
            // Read the stream
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            
            // Check if the CSV has a header
            bool hasHeader = source.ConnectionProperties.TryGetValue("hasHeader", out var hasHeaderStr) && 
                bool.TryParse(hasHeaderStr, out var hasHeaderBool) && hasHeaderBool;
            
            // Get delimiter
            char delimiter = source.ConnectionProperties.TryGetValue("delimiter", out var delimiterStr) ? 
                delimiterStr[0] : ',';
            
            // Read header
            string[] headers = null;
            string line = await reader.ReadLineAsync();
            
            if (line != null)
            {
                if (hasHeader)
                {
                    headers = line.Split(delimiter);
                    line = await reader.ReadLineAsync();
                }
                else
                {
                    // Generate column names (Column1, Column2, etc.)
                    var columnCount = line.Split(delimiter).Length;
                    headers = Enumerable.Range(1, columnCount).Select(i => $"Column{i}").ToArray();
                }
            }
            
            // Read data rows
            int rowCount = 0;
            while (line != null && (sampleSize == 0 || rowCount < sampleSize))
            {
                var values = line.Split(delimiter);
                var data = new Dictionary<string, object>();
                
                for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
                {
                    data[headers[i]] = values[i];
                }
                
                // Create a record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "CSV",
                        ["rowNumber"] = (rowCount + 1).ToString()
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
                
                records.Add(record);
                rowCount++;
                
                line = await reader.ReadLineAsync();
            }
            
            return records;
        }
        
        protected virtual async Task<IEnumerable<DataRecord>> ParseJsonAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();
            
            // Reset stream position
            stream.Position = 0;
            
            // Read the stream
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            var json = await reader.ReadToEndAsync();
            
            // Parse JSON
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;
            
            // Check if the root is an array
            if (root.ValueKind == JsonValueKind.Array)
            {
                // Process each array element as a record
                int count = 0;
                foreach (var element in root.EnumerateArray())
                {
                    if (sampleSize > 0 && count >= sampleSize)
                    {
                        break;
                    }
                    
                    var data = new Dictionary<string, object>();
                    
                    // Process properties
                    foreach (var property in element.EnumerateObject())
                    {
                        switch (property.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                data[property.Name] = property.Value.GetString();
                                break;
                            
                            case JsonValueKind.Number:
                                if (property.Value.TryGetInt64(out var intValue))
                                {
                                    data[property.Name] = intValue;
                                }
                                else if (property.Value.TryGetDouble(out var doubleValue))
                                {
                                    data[property.Name] = doubleValue;
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
                    
                    // Create a record
                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = data,
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "FileSystem",
                            ["format"] = "JSON",
                            ["index"] = count.ToString()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };
                    
                    records.Add(record);
                    count++;
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Process the object as a single record
                var data = new Dictionary<string, object>();
                
                // Process properties
                foreach (var property in root.EnumerateObject())
                {
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            data[property.Name] = property.Value.GetString();
                            break;
                        
                        case JsonValueKind.Number:
                            if (property.Value.TryGetInt64(out var intValue))
                            {
                                data[property.Name] = intValue;
                            }
                            else if (property.Value.TryGetDouble(out var doubleValue))
                            {
                                data[property.Name] = doubleValue;
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
                
                // Create a record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "JSON"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
                
                records.Add(record);
            }
            
            return records;
        }
        
        protected virtual async Task<IEnumerable<DataRecord>> ParseXmlAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            // For simplicity, we'll just return a placeholder implementation
            // In a real implementation, you would use XmlReader to parse the XML
            return await Task.FromResult(new List<DataRecord>());
        }
        
        protected virtual async Task<IEnumerable<DataRecord>> ParseTextAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();
            
            // Reset stream position
            stream.Position = 0;
            
            // Read the stream
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            
            // Check if we should treat each line as a record
            bool lineByLine = source.ConnectionProperties.TryGetValue("lineByLine", out var lineByLineStr) && 
                bool.TryParse(lineByLineStr, out var lineByLineBool) && lineByLineBool;
            
            if (lineByLine)
            {
                // Read line by line
                int lineNumber = 0;
                string line;
                
                while ((line = await reader.ReadLineAsync()) != null && (sampleSize == 0 || lineNumber < sampleSize))
                {
                    var data = new Dictionary<string, object>
                    {
                        ["text"] = line
                    };
                    
                    // Create a record
                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = data,
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "FileSystem",
                            ["format"] = "Text",
                            ["lineNumber"] = lineNumber.ToString()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };
                    
                    records.Add(record);
                    lineNumber++;
                }
            }
            else
            {
                // Read the entire file as a single record
                var text = await reader.ReadToEndAsync();
                
                var data = new Dictionary<string, object>
                {
                    ["text"] = text
                };
                
                // Create a record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "Text"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
                
                records.Add(record);
            }
            
            return records;
        }
        
        protected virtual DataSchema InferSchemaFromSample(IEnumerable<DataRecord> sampleData, DataSourceDefinition source)
        {
            if (sampleData == null || !sampleData.Any())
            {
                return new DataSchema
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"{source.Name} Schema",
                    Description = $"Schema for {source.Name}",
                    Type = SchemaType.Dynamic,
                    Fields = new List<SchemaField>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            
            var schema = new DataSchema
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{source.Name} Schema",
                Description = $"Schema for {source.Name}",
                Type = SchemaType.Dynamic,
                Fields = new List<SchemaField>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
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
        
        protected virtual bool IsFieldRequired(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // A field is required if it's present in all records and never null
            return sampleData.All(r => r.Data.ContainsKey(fieldName) && r.Data[fieldName] != null);
        }
        
        protected virtual bool IsFieldArray(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // Check if any value for this field is an array
            return sampleData.Any(r => 
                r.Data.ContainsKey(fieldName) && 
                r.Data[fieldName] != null && 
                r.Data[fieldName].GetType().IsArray);
        }
        
        protected virtual FieldType InferFieldType(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // Get non-null values for this field
            var values = sampleData
                .Where(r => r.Data.ContainsKey(fieldName) && r.Data[fieldName] != null)
                .Select(r => r.Data[fieldName])
                .ToList();
            
            if (values.Count == 0)
            {
                return FieldType.String; // Default to string if no values
            }
            
            // Check if all values are of the same type
            var firstType = values[0].GetType();
            
            if (values.All(v => v.GetType() == firstType))
            {
                // All values are of the same type
                if (firstType == typeof(string))
                {
                    return FieldType.String;
                }
                else if (firstType == typeof(int) || firstType == typeof(long) || firstType == typeof(short))
                {
                    return FieldType.Integer;
                }
                else if (firstType == typeof(float) || firstType == typeof(double) || firstType == typeof(decimal))
                {
                    return FieldType.Decimal;
                }
                else if (firstType == typeof(bool))
                {
                    return FieldType.Boolean;
                }
                else if (firstType == typeof(DateTime))
                {
                    return FieldType.DateTime;
                }
                else if (firstType.IsArray)
                {
                    return FieldType.Array;
                }
                else
                {
                    // Try to see if it's JSON
                    try
                    {
                        var jsonString = values[0].ToString();
                        var jsonDoc = JsonDocument.Parse(jsonString);
                        
                        if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            return FieldType.Json;
                        }
                        else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            return FieldType.Array;
                        }
                    }
                    catch
                    {
                        // Not JSON
                    }
                    
                    return FieldType.Complex;
                }
            }
            else
            {
                // Mixed types, default to string
                return FieldType.String;
            }
        }
        
        protected virtual void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }
    }
}
