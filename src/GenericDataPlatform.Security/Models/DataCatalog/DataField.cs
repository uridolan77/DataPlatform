using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models.DataCatalog
{
    /// <summary>
    /// Represents a field in a data asset schema
    /// </summary>
    public class DataField
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the field
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Data type of the field
        /// </summary>
        public string DataType { get; set; }
        
        /// <summary>
        /// Whether the field is nullable
        /// </summary>
        public bool IsNullable { get; set; }
        
        /// <summary>
        /// Whether the field is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        
        /// <summary>
        /// Whether the field is a foreign key
        /// </summary>
        public bool IsForeignKey { get; set; }
        
        /// <summary>
        /// Reference to the foreign key target
        /// </summary>
        public string ForeignKeyReference { get; set; }
        
        /// <summary>
        /// Default value for the field
        /// </summary>
        public string DefaultValue { get; set; }
        
        /// <summary>
        /// Tags associated with the field
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Sensitivity classification of the field
        /// </summary>
        public string SensitivityClassification { get; set; }
        
        /// <summary>
        /// Business glossary terms associated with the field
        /// </summary>
        public List<string> GlossaryTerms { get; set; } = new List<string>();
        
        /// <summary>
        /// Metadata about the field
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
