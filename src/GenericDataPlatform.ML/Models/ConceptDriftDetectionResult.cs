using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Result of concept drift detection
    /// </summary>
    public class ConceptDriftDetectionResult
    {
        /// <summary>
        /// Model name
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Model version
        /// </summary>
        public string ModelVersion { get; set; }
        
        /// <summary>
        /// Whether drift was detected
        /// </summary>
        public bool DriftDetected { get; set; }
        
        /// <summary>
        /// Confidence score of the drift detection (0-1)
        /// </summary>
        public double DriftConfidence { get; set; }
        
        /// <summary>
        /// Type of drift detected
        /// </summary>
        public DriftType DriftType { get; set; }
        
        /// <summary>
        /// Features that contributed most to the drift
        /// </summary>
        public Dictionary<string, double> DriftContributors { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Metrics before drift
        /// </summary>
        public Dictionary<string, double> BaselineMetrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Metrics after drift
        /// </summary>
        public Dictionary<string, double> CurrentMetrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Date and time when the drift was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Recommended action to take
        /// </summary>
        public DriftAction RecommendedAction { get; set; }
    }
    
    /// <summary>
    /// Type of concept drift
    /// </summary>
    public enum DriftType
    {
        /// <summary>
        /// No drift detected
        /// </summary>
        None,
        
        /// <summary>
        /// Sudden drift (abrupt change)
        /// </summary>
        Sudden,
        
        /// <summary>
        /// Gradual drift (slow change over time)
        /// </summary>
        Gradual,
        
        /// <summary>
        /// Recurring drift (seasonal or cyclical patterns)
        /// </summary>
        Recurring,
        
        /// <summary>
        /// Incremental drift (small changes that accumulate)
        /// </summary>
        Incremental
    }
    
    /// <summary>
    /// Recommended action for handling drift
    /// </summary>
    public enum DriftAction
    {
        /// <summary>
        /// No action needed
        /// </summary>
        NoAction,
        
        /// <summary>
        /// Retrain the model with new data
        /// </summary>
        Retrain,
        
        /// <summary>
        /// Update the model incrementally
        /// </summary>
        Update,
        
        /// <summary>
        /// Investigate the drift further
        /// </summary>
        Investigate,
        
        /// <summary>
        /// Deploy a new model
        /// </summary>
        DeployNew
    }
}
