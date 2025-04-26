using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Transformers.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Transformers.Json
{
    public class JsonTransformer : ITransformer
    {
        private readonly ILogger<JsonTransformer> _logger;
        
        public string Type => "Json";
        
        public JsonTransformer(ILogger<JsonTransformer> logger)
        {
            _logger = logger;
        }
        
        public async Task<object> TransformAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Ensure the input is a list of DataRecord objects
                if (!(input is IEnumerable<DataRecord> inputRecords))
                {
                    throw new ArgumentException("Input must be a list of DataRecord objects");
                }
                
                var records = inputRecords.ToList();
                
                // Check if there are any records to transform
                if (!records.Any())
                {
                    return records;
                }
                
                // Get transformation type
                if (!configuration.TryGetValue("transformationType", out var transformationTypeObj))
                {
                    throw new ArgumentException("Transformation type is required");
                }
                
                var transformationType = transformationTypeObj.ToString();
                
                // Apply the transformation
                switch (transformationType.ToLowerInvariant())
                {
                    case "filter":
                        return await FilterRecordsAsync(records, configuration);
                    
                    case "map":
                        return await MapFieldsAsync(records, configuration);
                    
                    case "flatten":
                        return await FlattenNestedObjectsAsync(records, configuration);
                    
                    case "aggregate":
                        return await AggregateRecordsAsync(records, configuration);
                    
                    default:
                        throw new NotSupportedException($"Transformation type {transformationType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming data");
                throw;
            }
        }
        
        private async Task<object> FilterRecordsAsync(List<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get filter conditions
            if (!configuration.TryGetValue("filterConditions", out var filterConditionsObj))
            {
                throw new ArgumentException("Filter conditions are required for filter transformation");
            }
            
            var filterConditions = filterConditionsObj as Dictionary<string, object>;
            if (filterConditions == null)
            {
                throw new ArgumentException("Filter conditions must be a dictionary");
            }
            
            // Apply filters
            var filteredRecords = records.Where(record =>
            {
                foreach (var condition in filterConditions)
                {
                    var field = condition.Key;
                    var value = condition.Value;
                    
                    if (!record.Data.TryGetValue(field, out var fieldValue))
                    {
                        return false;
                    }
                    
                    if (!AreValuesEqual(fieldValue, value))
                    {
                        return false;
                    }
                }
                
                return true;
            }).ToList();
            
            return filteredRecords;
        }
        
        private async Task<object> MapFieldsAsync(List<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get field mappings
            if (!configuration.TryGetValue("fieldMappings", out var fieldMappingsObj))
            {
                throw new ArgumentException("Field mappings are required for map transformation");
            }
            
            var fieldMappings = fieldMappingsObj as Dictionary<string, object>;
            if (fieldMappings == null)
            {
                throw new ArgumentException("Field mappings must be a dictionary");
            }
            
            // Apply mappings
            var mappedRecords = new List<DataRecord>();
            
            foreach (var record in records)
            {
                var mappedData = new Dictionary<string, object>();
                
                foreach (var mapping in fieldMappings)
                {
                    var targetField = mapping.Key;
                    var sourceField = mapping.Value.ToString();
                    
                    if (record.Data.TryGetValue(sourceField, out var value))
                    {
                        mappedData[targetField] = value;
                    }
                }
                
                // Copy fields that are not mapped
                if (configuration.TryGetValue("includeUnmappedFields", out var includeUnmappedFieldsObj) && 
                    includeUnmappedFieldsObj is bool includeUnmappedFields && includeUnmappedFields)
                {
                    foreach (var field in record.Data)
                    {
                        if (!fieldMappings.Values.Contains(field.Key))
                        {
                            mappedData[field.Key] = field.Value;
                        }
                    }
                }
                
                var mappedRecord = new DataRecord
                {
                    Id = record.Id,
                    SchemaId = record.SchemaId,
                    SourceId = record.SourceId,
                    Data = mappedData,
                    Metadata = record.Metadata,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    Version = record.Version
                };
                
                mappedRecords.Add(mappedRecord);
            }
            
            return mappedRecords;
        }
        
        private async Task<object> FlattenNestedObjectsAsync(List<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get fields to flatten
            if (!configuration.TryGetValue("fieldsToFlatten", out var fieldsToFlattenObj))
            {
                throw new ArgumentException("Fields to flatten are required for flatten transformation");
            }
            
            var fieldsToFlatten = fieldsToFlattenObj as List<object>;
            if (fieldsToFlatten == null)
            {
                throw new ArgumentException("Fields to flatten must be a list");
            }
            
            // Apply flattening
            var flattenedRecords = new List<DataRecord>();
            
            foreach (var record in records)
            {
                var flattenedData = new Dictionary<string, object>(record.Data);
                
                foreach (var fieldObj in fieldsToFlatten)
                {
                    var field = fieldObj.ToString();
                    
                    if (record.Data.TryGetValue(field, out var value) && value is Dictionary<string, object> nestedObject)
                    {
                        // Remove the nested object
                        flattenedData.Remove(field);
                        
                        // Add flattened fields
                        foreach (var nestedField in nestedObject)
                        {
                            var flattenedFieldName = $"{field}_{nestedField.Key}";
                            flattenedData[flattenedFieldName] = nestedField.Value;
                        }
                    }
                }
                
                var flattenedRecord = new DataRecord
                {
                    Id = record.Id,
                    SchemaId = record.SchemaId,
                    SourceId = record.SourceId,
                    Data = flattenedData,
                    Metadata = record.Metadata,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    Version = record.Version
                };
                
                flattenedRecords.Add(flattenedRecord);
            }
            
            return flattenedRecords;
        }
        
        private async Task<object> AggregateRecordsAsync(List<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get aggregation fields
            if (!configuration.TryGetValue("aggregations", out var aggregationsObj))
            {
                throw new ArgumentException("Aggregations are required for aggregate transformation");
            }
            
            var aggregations = aggregationsObj as Dictionary<string, object>;
            if (aggregations == null)
            {
                throw new ArgumentException("Aggregations must be a dictionary");
            }
            
            // Get group by fields
            if (!configuration.TryGetValue("groupBy", out var groupByObj))
            {
                throw new ArgumentException("Group by fields are required for aggregate transformation");
            }
            
            var groupByFields = groupByObj as List<object>;
            if (groupByFields == null)
            {
                throw new ArgumentException("Group by fields must be a list");
            }
            
            // Group records
            var groups = records.GroupBy(record =>
            {
                var key = new Dictionary<string, object>();
                
                foreach (var fieldObj in groupByFields)
                {
                    var field = fieldObj.ToString();
                    
                    if (record.Data.TryGetValue(field, out var value))
                    {
                        key[field] = value;
                    }
                    else
                    {
                        key[field] = null;
                    }
                }
                
                return new GroupKey(key);
            });
            
            // Apply aggregations
            var aggregatedRecords = new List<DataRecord>();
            
            foreach (var group in groups)
            {
                var aggregatedData = new Dictionary<string, object>();
                
                // Add group by fields
                foreach (var field in group.Key.Fields)
                {
                    aggregatedData[field.Key] = field.Value;
                }
                
                // Apply aggregations
                foreach (var aggregation in aggregations)
                {
                    var targetField = aggregation.Key;
                    var aggregationConfig = aggregation.Value as Dictionary<string, object>;
                    
                    if (aggregationConfig == null)
                    {
                        throw new ArgumentException($"Aggregation configuration for {targetField} must be a dictionary");
                    }
                    
                    if (!aggregationConfig.TryGetValue("type", out var aggregationTypeObj))
                    {
                        throw new ArgumentException($"Aggregation type is required for {targetField}");
                    }
                    
                    var aggregationType = aggregationTypeObj.ToString();
                    
                    if (!aggregationConfig.TryGetValue("field", out var fieldObj))
                    {
                        throw new ArgumentException($"Field is required for {targetField} aggregation");
                    }
                    
                    var field = fieldObj.ToString();
                    
                    // Apply the aggregation
                    switch (aggregationType.ToLowerInvariant())
                    {
                        case "sum":
                            aggregatedData[targetField] = group.Sum(record => 
                                record.Data.TryGetValue(field, out var value) && value is IConvertible ? 
                                    Convert.ToDouble(value) : 0);
                            break;
                        
                        case "avg":
                            aggregatedData[targetField] = group.Average(record => 
                                record.Data.TryGetValue(field, out var value) && value is IConvertible ? 
                                    Convert.ToDouble(value) : 0);
                            break;
                        
                        case "min":
                            aggregatedData[targetField] = group.Min(record => 
                                record.Data.TryGetValue(field, out var value) && value is IConvertible ? 
                                    Convert.ToDouble(value) : 0);
                            break;
                        
                        case "max":
                            aggregatedData[targetField] = group.Max(record => 
                                record.Data.TryGetValue(field, out var value) && value is IConvertible ? 
                                    Convert.ToDouble(value) : 0);
                            break;
                        
                        case "count":
                            aggregatedData[targetField] = group.Count();
                            break;
                        
                        case "first":
                            aggregatedData[targetField] = group.First().Data.TryGetValue(field, out var firstValue) ? 
                                firstValue : null;
                            break;
                        
                        case "last":
                            aggregatedData[targetField] = group.Last().Data.TryGetValue(field, out var lastValue) ? 
                                lastValue : null;
                            break;
                        
                        default:
                            throw new NotSupportedException($"Aggregation type {aggregationType} is not supported");
                    }
                }
                
                var aggregatedRecord = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = records.First().SchemaId,
                    SourceId = records.First().SourceId,
                    Data = aggregatedData,
                    Metadata = new Dictionary<string, string>
                    {
                        { "aggregation", "true" },
                        { "recordCount", group.Count().ToString() }
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
                
                aggregatedRecords.Add(aggregatedRecord);
            }
            
            return aggregatedRecords;
        }
        
        private bool AreValuesEqual(object value1, object value2)
        {
            if (value1 == null && value2 == null)
            {
                return true;
            }
            
            if (value1 == null || value2 == null)
            {
                return false;
            }
            
            // Convert to string for comparison
            return value1.ToString().Equals(value2.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        private class GroupKey
        {
            public Dictionary<string, object> Fields { get; }
            
            public GroupKey(Dictionary<string, object> fields)
            {
                Fields = fields;
            }
            
            public override bool Equals(object obj)
            {
                if (obj is GroupKey other)
                {
                    if (Fields.Count != other.Fields.Count)
                    {
                        return false;
                    }
                    
                    foreach (var field in Fields)
                    {
                        if (!other.Fields.TryGetValue(field.Key, out var otherValue) || 
                            !AreValuesEqual(field.Value, otherValue))
                        {
                            return false;
                        }
                    }
                    
                    return true;
                }
                
                return false;
            }
            
            public override int GetHashCode()
            {
                var hashCode = 17;
                
                foreach (var field in Fields)
                {
                    hashCode = hashCode * 23 + (field.Key?.GetHashCode() ?? 0);
                    hashCode = hashCode * 23 + (field.Value?.ToString()?.GetHashCode() ?? 0);
                }
                
                return hashCode;
            }
            
            private bool AreValuesEqual(object value1, object value2)
            {
                if (value1 == null && value2 == null)
                {
                    return true;
                }
                
                if (value1 == null || value2 == null)
                {
                    return false;
                }
                
                // Convert to string for comparison
                return value1.ToString().Equals(value2.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
