using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Common.Models
{
    public class DataRecord
    {
        public string Id { get; set; }
        public string SchemaId { get; set; }
        public string SourceId { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Version { get; set; }
        
        // Helper methods for accessing typed data
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }
        
        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            if (Data.TryGetValue(key, out var objValue) && objValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            return false;
        }
    }
}
