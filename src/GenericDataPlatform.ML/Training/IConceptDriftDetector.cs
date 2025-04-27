using System.Threading.Tasks;
using Microsoft.ML;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Interface for concept drift detection
    /// </summary>
    public interface IConceptDriftDetector
    {
        /// <summary>
        /// Detects concept drift between two datasets
        /// </summary>
        /// <param name="model">The model to evaluate</param>
        /// <param name="metadata">Model metadata</param>
        /// <param name="newData">New data to check for drift</param>
        /// <param name="referenceData">Reference data to compare against</param>
        /// <returns>Concept drift detection result</returns>
        Task<ConceptDriftDetectionResult> DetectDriftAsync(
            ITransformer model,
            ModelMetadata metadata,
            IDataView newData,
            IDataView referenceData);
        
        /// <summary>
        /// Detects feature drift between two datasets
        /// </summary>
        /// <param name="metadata">Model metadata</param>
        /// <param name="newData">New data to check for drift</param>
        /// <param name="referenceData">Reference data to compare against</param>
        /// <returns>Dictionary of feature names and drift scores</returns>
        Task<System.Collections.Generic.Dictionary<string, double>> DetectFeatureDriftAsync(
            ModelMetadata metadata,
            IDataView newData,
            IDataView referenceData);
    }
}
