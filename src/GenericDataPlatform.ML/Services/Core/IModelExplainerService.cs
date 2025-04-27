using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for explaining model predictions
    /// </summary>
    public interface IModelExplainerService
    {
        /// <summary>
        /// Explains a model globally
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <param name="explanationType">Type of explanation to generate</param>
        /// <param name="sampleData">Sample data for generating explanations</param>
        /// <returns>Explanation result</returns>
        Task<ExplanationResult> ExplainModelGloballyAsync(
            string modelName,
            string modelVersion = null,
            ExplanationType explanationType = ExplanationType.SHAP,
            IEnumerable<Dictionary<string, object>> sampleData = null);
        
        /// <summary>
        /// Explains predictions for specific instances
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="instances">Instances to explain</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <param name="explanationType">Type of explanation to generate</param>
        /// <param name="backgroundData">Background data for SHAP explanations</param>
        /// <returns>Explanation result</returns>
        Task<ExplanationResult> ExplainPredictionsAsync(
            string modelName,
            List<Dictionary<string, object>> instances,
            string modelVersion = null,
            ExplanationType explanationType = ExplanationType.SHAP,
            IEnumerable<Dictionary<string, object>> backgroundData = null);
        
        /// <summary>
        /// Gets supported explanation types for a given model type
        /// </summary>
        /// <param name="modelType">Type of model</param>
        /// <returns>List of supported explanation types</returns>
        List<ExplanationType> GetSupportedExplanationTypes(ModelType modelType);
    }
}
