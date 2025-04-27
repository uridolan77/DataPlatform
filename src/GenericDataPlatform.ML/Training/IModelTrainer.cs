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
    }
}
