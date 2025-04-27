using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for managing ML models
    /// </summary>
    public interface IModelManagementService
    {
        /// <summary>
        /// Registers a model in the registry
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="modelPath">Path to the model file</param>
        /// <param name="modelDefinition">Definition of the model</param>
        /// <param name="runId">ID of the MLflow run</param>
        /// <param name="experimentId">ID of the MLflow experiment</param>
        /// <param name="metrics">Training metrics</param>
        /// <returns>Metadata for the registered model</returns>
        Task<ModelMetadata> RegisterModelAsync(
            string modelName,
            string modelPath,
            ModelDefinition modelDefinition,
            string runId,
            string experimentId,
            Dictionary<string, double> metrics);
        
        /// <summary>
        /// Gets a model from the registry
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model (null for latest)</param>
        /// <returns>Model metadata, or null if not found</returns>
        Task<ModelMetadata> GetModelAsync(string modelName, string version = null);
        
        /// <summary>
        /// Lists all models in the registry
        /// </summary>
        /// <param name="filter">Optional filter</param>
        /// <param name="skip">Number of models to skip</param>
        /// <param name="take">Number of models to take</param>
        /// <returns>List of model metadata</returns>
        Task<List<ModelMetadata>> ListModelsAsync(string filter = null, int skip = 0, int take = 20);
        
        /// <summary>
        /// Transitions a model to a different stage
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model (null for latest)</param>
        /// <param name="stage">Stage to transition to (e.g., "Staging", "Production", "Archived")</param>
        /// <param name="reason">Optional reason for the transition</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> TransitionModelStageAsync(string modelName, string version, string stage, string reason = null);
        
        /// <summary>
        /// Deletes a model from the registry
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model (null for latest)</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteModelAsync(string modelName, string version = null);
        
        /// <summary>
        /// Gets all versions of a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>List of model metadata</returns>
        Task<List<ModelMetadata>> GetModelVersionsAsync(string modelName);
        
        /// <summary>
        /// Adds a tag to a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model (null for latest)</param>
        /// <param name="key">Tag key</param>
        /// <param name="value">Tag value</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AddModelTagAsync(string modelName, string version, string key, string value);
        
        /// <summary>
        /// Updates usage statistics for a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model</param>
        /// <param name="predictionCount">Number of predictions made</param>
        /// <param name="latencyMs">Latency in milliseconds</param>
        /// <param name="errorCount">Number of errors</param>
        /// <returns>Updated model metadata</returns>
        Task<ModelMetadata> UpdateModelUsageStatsAsync(
            string modelName, 
            string version, 
            int predictionCount, 
            double latencyMs, 
            int errorCount);
    }
}