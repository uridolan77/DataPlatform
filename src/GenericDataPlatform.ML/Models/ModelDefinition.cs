using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Represents a machine learning model definition
    /// </summary>
    public class ModelDefinition
    {
        /// <summary>
        /// Unique identifier for the model
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the model
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the model
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Type of the model
        /// </summary>
        public ModelType Type { get; set; }
        
        /// <summary>
        /// Algorithm used by the model
        /// </summary>
        public string Algorithm { get; set; }
        
        /// <summary>
        /// Input features for the model
        /// </summary>
        public List<FeatureDefinition> Features { get; set; } = new List<FeatureDefinition>();
        
        /// <summary>
        /// Output labels for the model
        /// </summary>
        public List<LabelDefinition> Labels { get; set; } = new List<LabelDefinition>();
        
        /// <summary>
        /// Hyperparameters for the model
        /// </summary>
        public Dictionary<string, string> Hyperparameters { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Version of the model
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// Date and time when the model was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date and time when the model was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Type of machine learning model
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// Binary classification model
        /// </summary>
        BinaryClassification,
        
        /// <summary>
        /// Multi-class classification model
        /// </summary>
        MultiClassClassification,
        
        /// <summary>
        /// Regression model
        /// </summary>
        Regression,
        
        /// <summary>
        /// Clustering model
        /// </summary>
        Clustering,
        
        /// <summary>
        /// Anomaly detection model
        /// </summary>
        AnomalyDetection,
        
        /// <summary>
        /// Recommendation model
        /// </summary>
        Recommendation,
        
        /// <summary>
        /// Time series forecasting model
        /// </summary>
        TimeSeries
    }
    
    /// <summary>
    /// Represents a feature definition for a machine learning model
    /// </summary>
    public class FeatureDefinition
    {
        /// <summary>
        /// Name of the feature
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the feature
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Data type of the feature
        /// </summary>
        public FeatureDataType DataType { get; set; }
        
        /// <summary>
        /// Whether the feature is required
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Whether the feature is an array
        /// </summary>
        public bool IsArray { get; set; }
        
        /// <summary>
        /// Default value for the feature
        /// </summary>
        public string DefaultValue { get; set; }
    }
    
    /// <summary>
    /// Data type of a feature
    /// </summary>
    public enum FeatureDataType
    {
        /// <summary>
        /// String data type
        /// </summary>
        String,
        
        /// <summary>
        /// Integer data type
        /// </summary>
        Integer,
        
        /// <summary>
        /// Float data type
        /// </summary>
        Float,
        
        /// <summary>
        /// Boolean data type
        /// </summary>
        Boolean,
        
        /// <summary>
        /// DateTime data type
        /// </summary>
        DateTime,
        
        /// <summary>
        /// Categorical data type
        /// </summary>
        Categorical,
        
        /// <summary>
        /// Text data type
        /// </summary>
        Text,
        
        /// <summary>
        /// Image data type
        /// </summary>
        Image
    }
    
    /// <summary>
    /// Represents a label definition for a machine learning model
    /// </summary>
    public class LabelDefinition
    {
        /// <summary>
        /// Name of the label
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the label
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Data type of the label
        /// </summary>
        public LabelDataType DataType { get; set; }
        
        /// <summary>
        /// Possible values for categorical labels
        /// </summary>
        public List<string> PossibleValues { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Data type of a label
    /// </summary>
    public enum LabelDataType
    {
        /// <summary>
        /// Binary label (0 or 1)
        /// </summary>
        Binary,
        
        /// <summary>
        /// Categorical label (one of several classes)
        /// </summary>
        Categorical,
        
        /// <summary>
        /// Continuous label (numeric value)
        /// </summary>
        Continuous
    }
}
