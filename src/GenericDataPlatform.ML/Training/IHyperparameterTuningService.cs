using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ML;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Service for hyperparameter tuning
    /// </summary>
    public interface IHyperparameterTuningService
    {
        /// <summary>
        /// Tunes hyperparameters for a given algorithm and dataset
        /// </summary>
        /// <param name="data">Training data</param>
        /// <param name="validationData">Validation data</param>
        /// <param name="modelType">Type of model to train</param>
        /// <param name="algorithm">Algorithm to tune</param>
        /// <param name="featureColumnName">Name of the feature column</param>
        /// <param name="labelColumnName">Name of the label column</param>
        /// <param name="customSearchSpace">Custom hyperparameter search space (optional)</param>
        /// <param name="maxIterations">Maximum number of iterations</param>
        /// <returns>Best hyperparameters found</returns>
        Task<Dictionary<string, string>> TuneHyperparametersAsync(
            IDataView data,
            IDataView validationData,
            ModelType modelType,
            string algorithm,
            string featureColumnName,
            string labelColumnName,
            Dictionary<string, object> customSearchSpace = null,
            int maxIterations = 20);
        
        /// <summary>
        /// Gets the default hyperparameter search space for a given algorithm
        /// </summary>
        /// <param name="modelType">Type of model</param>
        /// <param name="algorithm">Algorithm name</param>
        /// <returns>Default hyperparameter search space</returns>
        Dictionary<string, object> GetDefaultSearchSpace(ModelType modelType, string algorithm);
    }
}
