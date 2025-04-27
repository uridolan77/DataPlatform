using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Infrastructure
{
    /// <summary>
    /// Repository for ML models
    /// </summary>
    public interface IModelRepository
    {
        /// <summary>
        /// Saves model metadata to the repository
        /// </summary>
        /// <param name="metadata">Model metadata to save</param>
        Task SaveModelMetadataAsync(ModelMetadata metadata);

        /// <summary>
        /// Gets model metadata from the repository
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model (null for latest)</param>
        /// <returns>Model metadata, or null if not found</returns>
        Task<ModelMetadata?> GetModelMetadataAsync(string modelName, string? version = null);

        /// <summary>
        /// Lists model metadata from the repository
        /// </summary>
        /// <param name="filter">Optional filter</param>
        /// <param name="skip">Number of models to skip</param>
        /// <param name="take">Number of models to take</param>
        /// <returns>List of model metadata</returns>
        Task<List<ModelMetadata>> ListModelMetadataAsync(string? filter = null, int skip = 0, int take = 20);

        /// <summary>
        /// Gets all versions of a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>List of model metadata</returns>
        Task<List<ModelMetadata>> GetModelVersionsAsync(string modelName);

        /// <summary>
        /// Gets models by stage
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="stage">Stage of the model</param>
        /// <returns>List of model metadata</returns>
        Task<List<ModelMetadata>> GetModelsByStageAsync(string modelName, string stage);

        /// <summary>
        /// Deletes model metadata from the repository
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model</param>
        Task DeleteModelMetadataAsync(string modelName, string version);

        /// <summary>
        /// Saves a model to the repository
        /// </summary>
        /// <param name="modelPath">Path to save the model to</param>
        /// <param name="modelBytes">Model data</param>
        Task SaveModelBytesAsync(string modelPath, byte[] modelBytes);

        /// <summary>
        /// Loads a model from the repository
        /// </summary>
        /// <param name="modelPath">Path to load the model from</param>
        /// <returns>Model data</returns>
        Task<byte[]> LoadModelBytesAsync(string modelPath);

        /// <summary>
        /// Deletes a model from the repository
        /// </summary>
        /// <param name="modelPath">Path to delete the model from</param>
        Task DeleteModelBytesAsync(string modelPath);
    }
}