using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for automated machine learning
    /// </summary>
    public interface IAutoMLService
    {
        /// <summary>
        /// Runs AutoML to find the best model for the given data
        /// </summary>
        /// <param name="config">AutoML configuration</param>
        /// <param name="trainingData">Training data</param>
        /// <param name="validationData">Optional validation data (if not provided, will be split from training data)</param>
        /// <returns>AutoML result with the best model</returns>
        Task<AutoMLResult> RunAutoMLAsync(
            AutoMLConfig config,
            IEnumerable<Dictionary<string, object>> trainingData,
            IEnumerable<Dictionary<string, object>> validationData = null);
        
        /// <summary>
        /// Gets the list of supported algorithms for a given model type
        /// </summary>
        /// <param name="modelType">Type of model</param>
        /// <returns>List of supported algorithms</returns>
        List<string> GetSupportedAlgorithms(ModelType modelType);
        
        /// <summary>
        /// Gets the default optimization metric for a given model type
        /// </summary>
        /// <param name="modelType">Type of model</param>
        /// <returns>Default optimization metric</returns>
        string GetDefaultOptimizationMetric(ModelType modelType);
    }
}
