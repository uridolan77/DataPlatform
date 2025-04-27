using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for online learning and concept drift detection
    /// </summary>
    public interface IOnlineLearningService
    {
        /// <summary>
        /// Updates a model incrementally with new data
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="trainingData">New training data</param>
        /// <param name="modelVersion">Version of the model to update (null for latest)</param>
        /// <param name="learningRate">Learning rate for the update (0-1)</param>
        /// <returns>Updated model</returns>
        Task<TrainedModel> UpdateModelAsync(
            string modelName,
            IEnumerable<Dictionary<string, object>> trainingData,
            string modelVersion = null,
            double learningRate = 0.1);
        
        /// <summary>
        /// Detects concept drift in a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="newData">New data to check for drift</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <param name="referenceData">Reference data to compare against (null to use original training data)</param>
        /// <returns>Concept drift detection result</returns>
        Task<ConceptDriftDetectionResult> DetectConceptDriftAsync(
            string modelName,
            IEnumerable<Dictionary<string, object>> newData,
            string modelVersion = null,
            IEnumerable<Dictionary<string, object>> referenceData = null);
        
        /// <summary>
        /// Checks if a model supports online learning
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <returns>Whether the model supports online learning</returns>
        Task<bool> SupportsOnlineLearningAsync(string modelName, string modelVersion = null);
        
        /// <summary>
        /// Gets the online learning configuration for a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <returns>Online learning configuration</returns>
        Task<OnlineLearningConfig> GetOnlineLearningConfigAsync(string modelName, string modelVersion = null);
        
        /// <summary>
        /// Sets the online learning configuration for a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="config">Online learning configuration</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <returns>Updated configuration</returns>
        Task<OnlineLearningConfig> SetOnlineLearningConfigAsync(
            string modelName,
            OnlineLearningConfig config,
            string modelVersion = null);
    }
    
    /// <summary>
    /// Configuration for online learning
    /// </summary>
    public class OnlineLearningConfig
    {
        /// <summary>
        /// Whether online learning is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;
        
        /// <summary>
        /// Default learning rate
        /// </summary>
        public double DefaultLearningRate { get; set; } = 0.1;
        
        /// <summary>
        /// Minimum number of samples required for an update
        /// </summary>
        public int MinimumSamplesForUpdate { get; set; } = 100;
        
        /// <summary>
        /// Maximum number of samples to store for drift detection
        /// </summary>
        public int MaxSamplesForDriftDetection { get; set; } = 1000;
        
        /// <summary>
        /// Drift detection threshold (0-1)
        /// </summary>
        public double DriftDetectionThreshold { get; set; } = 0.05;
        
        /// <summary>
        /// Whether to automatically update the model when drift is detected
        /// </summary>
        public bool AutoUpdateOnDrift { get; set; } = false;
        
        /// <summary>
        /// Minimum confidence required for auto-update
        /// </summary>
        public double AutoUpdateMinConfidence { get; set; } = 0.7;
        
        /// <summary>
        /// Whether to keep version history for online learning updates
        /// </summary>
        public bool KeepVersionHistory { get; set; } = true;
        
        /// <summary>
        /// Maximum number of versions to keep
        /// </summary>
        public int MaxVersionsToKeep { get; set; } = 5;
    }
}
