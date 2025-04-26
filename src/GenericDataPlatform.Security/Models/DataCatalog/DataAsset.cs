using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models.DataCatalog
{
    /// <summary>
    /// Represents a data asset in the catalog
    /// </summary>
    public class DataAsset
    {
        /// <summary>
        /// Unique identifier for the data asset
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the data asset
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the data asset
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Type of the data asset (e.g., Table, File, API)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Format of the data asset (e.g., CSV, JSON, Parquet)
        /// </summary>
        public string Format { get; set; }
        
        /// <summary>
        /// Location of the data asset
        /// </summary>
        public string Location { get; set; }
        
        /// <summary>
        /// Owner of the data asset
        /// </summary>
        public string Owner { get; set; }
        
        /// <summary>
        /// Steward of the data asset
        /// </summary>
        public string Steward { get; set; }
        
        /// <summary>
        /// Tags associated with the data asset
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Schema of the data asset
        /// </summary>
        public List<DataField> Schema { get; set; } = new List<DataField>();
        
        /// <summary>
        /// Metadata about the data asset
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Quality metrics for the data asset
        /// </summary>
        public Dictionary<string, double> QualityMetrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Sensitivity classification of the data asset
        /// </summary>
        public string SensitivityClassification { get; set; }
        
        /// <summary>
        /// Retention policy for the data asset
        /// </summary>
        public string RetentionPolicy { get; set; }
        
        /// <summary>
        /// Date and time when the data asset was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date and time when the data asset was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Date and time when the data asset was last accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }
    }
}
