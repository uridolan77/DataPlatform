using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Interface for model trainer
    /// </summary>
    public interface IModelTrainer
    {
        /// <summary>
        /// Trains a model using the specified training data and model definition
        /// </summary>
        Task<TrainedModel> TrainModelAsync(ModelDefinition modelDefinition, IEnumerable<Dictionary<string, object>> trainingData);

        /// <summary>
        /// Trains a model using the specified training data, validation data, and model definition
        /// </summary>
        /// <param name="modelDefinition">Definition of the model to train</param>
        /// <param name="trainingData">Training data</param>
        /// <param name="validationData">Optional validation data</param>
        /// <param name="context">Training context</param>
        /// <returns>Trained model</returns>
        Task<TrainedModel> TrainModelAsync(
            ModelDefinition modelDefinition,
            IEnumerable<Dictionary<string, object>> trainingData,
            IEnumerable<Dictionary<string, object>> validationData,
            TrainingContext context);
    }
}
