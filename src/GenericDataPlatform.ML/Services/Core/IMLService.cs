using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Interface for ML service
    /// </summary>
    public interface IMLService
    {
        /// <summary>
        /// Trains a model using the specified training data and model definition
        /// </summary>
        Task<TrainedModel> TrainModelAsync(ModelDefinition modelDefinition, IEnumerable<Dictionary<string, object>> trainingData);
        
        /// <summary>
        /// Makes predictions using a trained model
        /// </summary>
        Task<IEnumerable<Dictionary<string, object>>> PredictAsync(string modelId, IEnumerable<Dictionary<string, object>> data);
        
        /// <summary>
        /// Gets a model by ID
        /// </summary>
        Task<ModelDefinition> GetModelAsync(string modelId);
        
        /// <summary>
        /// Lists all models
        /// </summary>
        Task<IEnumerable<ModelDefinition>> ListModelsAsync();
        
        /// <summary>
        /// Deletes a model by ID
        /// </summary>
        Task DeleteModelAsync(string modelId);
    }
}
