using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Metadata for a registered model
    /// </summary>
    public class ModelMetadata
    {
        /// <summary>
        /// Name of the model
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Version of the model
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Definition of the model
        /// </summary>
        public ModelDefinition Definition { get; set; }
        
        /// <summary>
        /// Type of the model
        /// </summary>
        public ModelType Type { get; set; }
        
        /// <summary>
        /// Current stage of the model (e.g., "Staging", "Production", "Archived")
        /// </summary>
        public string Stage { get; set; }
        
        /// <summary>
        /// Description of the model
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Tags for the model
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Path to the model file
        /// </summary>
        public string ModelPath { get; set; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// User who created the model
        /// </summary>
        public string CreatedBy { get; set; }
        
        /// <summary>
        /// ID of the run that created the model
        /// </summary>
        public string RunId { get; set; }
        
        /// <summary>
        /// ID of the experiment
        /// </summary>
        public string ExperimentId { get; set; }
        
        /// <summary>
        /// Training metrics
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Parameters used for training
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Schema of the input data
        /// </summary>
        public List<FeatureDefinition> InputSchema { get; set; } = new List<FeatureDefinition>();
        
        /// <summary>
        /// Schema of the output data
        /// </summary>
        public List<LabelDefinition> OutputSchema { get; set; } = new List<LabelDefinition>();
        
        /// <summary>
        /// History of stage transitions
        /// </summary>
        public List<ModelStageTransition> StageTransitions { get; set; } = new List<ModelStageTransition>();
        
        /// <summary>
        /// Last time the model was used for predictions
        /// </summary>
        public DateTime? LastUsed { get; set; }
        
        /// <summary>
        /// Usage statistics
        /// </summary>
        public ModelUsageStats UsageStats { get; set; } = new ModelUsageStats();
    }
    
    /// <summary>
    /// Record of a stage transition for a model
    /// </summary>
    public class ModelStageTransition
    {
        /// <summary>
        /// Previous stage
        /// </summary>
        public string FromStage { get; set; }
        
        /// <summary>
        /// New stage
        /// </summary>
        public string ToStage { get; set; }
        
        /// <summary>
        /// Timestamp of the transition
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// User who made the transition
        /// </summary>
        public string User { get; set; }
        
        /// <summary>
        /// Reason for the transition
        /// </summary>
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Usage statistics for a model
    /// </summary>
    public class ModelUsageStats
    {
        /// <summary>
        /// Total number of predictions made
        /// </summary>
        public long TotalPredictions { get; set; }
        
        /// <summary>
        /// Number of predictions made today
        /// </summary>
        public long TodayPredictions { get; set; }
        
        /// <summary>
        /// Number of predictions made this week
        /// </summary>
        public long WeekPredictions { get; set; }
        
        /// <summary>
        /// Number of predictions made this month
        /// </summary>
        public long MonthPredictions { get; set; }
        
        /// <summary>
        /// Average latency (ms)
        /// </summary>
        public double AverageLatencyMs { get; set; }
        
        /// <summary>
        /// Number of errors
        /// </summary>
        public long ErrorCount { get; set; }
        
        /// <summary>
        /// Error rate (percentage)
        /// </summary>
        public double ErrorRate { get; set; }
    }
}