using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Enrichers
{
    public class LookupEnricher : IEnricher
    {
        private readonly ILogger<LookupEnricher> _logger;
        
        public string Type => "Lookup";
        
        public LookupEnricher(ILogger<LookupEnricher> logger)
        {
            _logger = logger;
        }
        
        public async Task<object> EnrichAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Ensure the input is a list of DataRecord objects
                if (!(input is IEnumerable<DataRecord> inputRecords))
                {
                    throw new ArgumentException("Input must be a list of DataRecord objects");
                }
                
                var records = inputRecords.ToList();
                
                // Get lookup configuration
                if (!configuration.TryGetValue("lookupSource", out var lookupSourceObj))
                {
                    throw new ArgumentException("Lookup source is required");
                }
                
                var lookupSource = lookupSourceObj.ToString();
                
                if (!configuration.TryGetValue("lookupType", out var lookupTypeObj))
                {
                    throw new ArgumentException("Lookup type is required");
                }
                
                var lookupType = lookupTypeObj.ToString();
                
                if (!configuration.TryGetValue("sourceField", out var sourceFieldObj))
                {
                    throw new ArgumentException("Source field is required");
                }
                
                var sourceField = sourceFieldObj.ToString();
                
                if (!configuration.TryGetValue("lookupField", out var lookupFieldObj))
                {
                    throw new ArgumentException("Lookup field is required");
                }
                
                var lookupField = lookupFieldObj.ToString();
                
                if (!configuration.TryGetValue("targetFields", out var targetFieldsObj) || 
                    !(targetFieldsObj is Dictionary<string, object> targetFieldsDict))
                {
                    throw new ArgumentException("Target fields are required");
                }
                
                var targetFields = targetFieldsDict.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                
                // Get lookup data
                var lookupData = await GetLookupDataAsync(lookupType, lookupSource, configuration);
                
                // Apply lookup to each record
                var enrichedRecords = new List<DataRecord>();
                
                foreach (var record in records)
                {
                    var enrichedData = new Dictionary<string, object>(record.Data);
                    
                    // Get the source value
                    if (record.Data.TryGetValue(sourceField, out var sourceValue) && sourceValue != null)
                    {
                        var sourceKey = sourceValue.ToString();
                        
                        // Find matching lookup record
                        var lookupRecord = lookupData.FirstOrDefault(lr => 
                            lr.TryGetValue(lookupField, out var lookupValue) && 
                            lookupValue?.ToString() == sourceKey);
                        
                        if (lookupRecord != null)
                        {
                            // Apply lookup values to target fields
                            foreach (var targetField in targetFields)
                            {
                                var targetFieldName = targetField.Key;
                                var lookupFieldName = targetField.Value;
                                
                                if (lookupRecord.TryGetValue(lookupFieldName, out var lookupValue))
                                {
                                    enrichedData[targetFieldName] = lookupValue;
                                }
                            }
                        }
                        else if (configuration.TryGetValue("defaultValues", out var defaultValuesObj) && 
                                 defaultValuesObj is Dictionary<string, object> defaultValues)
                        {
                            // Apply default values for non-matching records
                            foreach (var defaultValue in defaultValues)
                            {
                                enrichedData[defaultValue.Key] = defaultValue.Value;
                            }
                        }
                    }
                    
                    // Create a new record with enriched data
                    var enrichedRecord = new DataRecord
                    {
                        Id = record.Id,
                        SchemaId = record.SchemaId,
                        SourceId = record.SourceId,
                        Data = enrichedData,
                        Metadata = new Dictionary<string, string>(record.Metadata)
                        {
                            ["enriched"] = "true",
                            ["enrichmentTime"] = DateTime.UtcNow.ToString("o"),
                            ["lookupSource"] = lookupSource
                        },
                        CreatedAt = record.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                        Version = record.Version
                    };
                    
                    enrichedRecords.Add(enrichedRecord);
                }
                
                return enrichedRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching data with lookup");
                throw;
            }
        }
        
        private async Task<List<Dictionary<string, object>>> GetLookupDataAsync(string lookupType, string lookupSource, Dictionary<string, object> configuration)
        {
            switch (lookupType.ToLowerInvariant())
            {
                case "inline":
                    return GetInlineLookupData(configuration);
                
                case "file":
                    return await GetFileLookupDataAsync(lookupSource, configuration);
                
                case "database":
                    return await GetDatabaseLookupDataAsync(lookupSource, configuration);
                
                case "api":
                    return await GetApiLookupDataAsync(lookupSource, configuration);
                
                default:
                    throw new ArgumentException($"Unsupported lookup type: {lookupType}");
            }
        }
        
        private List<Dictionary<string, object>> GetInlineLookupData(Dictionary<string, object> configuration)
        {
            if (!configuration.TryGetValue("lookupData", out var lookupDataObj) || 
                !(lookupDataObj is List<object> lookupDataList))
            {
                throw new ArgumentException("Lookup data is required for inline lookup");
            }
            
            var lookupData = new List<Dictionary<string, object>>();
            
            foreach (var item in lookupDataList)
            {
                if (item is Dictionary<string, object> itemDict)
                {
                    lookupData.Add(itemDict);
                }
            }
            
            return lookupData;
        }
        
        private async Task<List<Dictionary<string, object>>> GetFileLookupDataAsync(string lookupSource, Dictionary<string, object> configuration)
        {
            // In a real implementation, this would read data from a file
            // For this example, we'll just return an empty list
            _logger.LogWarning("File lookup is not implemented");
            
            return await Task.FromResult(new List<Dictionary<string, object>>());
        }
        
        private async Task<List<Dictionary<string, object>>> GetDatabaseLookupDataAsync(string lookupSource, Dictionary<string, object> configuration)
        {
            // In a real implementation, this would query a database
            // For this example, we'll just return an empty list
            _logger.LogWarning("Database lookup is not implemented");
            
            return await Task.FromResult(new List<Dictionary<string, object>>());
        }
        
        private async Task<List<Dictionary<string, object>>> GetApiLookupDataAsync(string lookupSource, Dictionary<string, object> configuration)
        {
            // In a real implementation, this would call an API
            // For this example, we'll just return an empty list
            _logger.LogWarning("API lookup is not implemented");
            
            return await Task.FromResult(new List<Dictionary<string, object>>());
        }
    }
}
