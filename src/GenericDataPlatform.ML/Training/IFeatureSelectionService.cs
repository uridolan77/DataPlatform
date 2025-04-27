using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ML;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Service for automated feature selection
    /// </summary>
    public interface IFeatureSelectionService
    {
        /// <summary>
        /// Selects the most important features for a given dataset and model type
        /// </summary>
        /// <param name="data">Training data</param>
        /// <param name="modelType">Type of model to train</param>
        /// <param name="featureDefinitions">Original feature definitions</param>
        /// <param name="labelName">Name of the label column</param>
        /// <param name="maxFeatures">Maximum number of features to select (null for automatic)</param>
        /// <returns>List of selected feature names and their importance scores</returns>
        Task<Dictionary<string, double>> SelectFeaturesAsync(
            IDataView data, 
            ModelType modelType, 
            List<FeatureDefinition> featureDefinitions, 
            string labelName, 
            int? maxFeatures = null);
        
        /// <summary>
        /// Ranks features by importance for a given dataset and model type
        /// </summary>
        /// <param name="data">Training data</param>
        /// <param name="modelType">Type of model to train</param>
        /// <param name="featureDefinitions">Original feature definitions</param>
        /// <param name="labelName">Name of the label column</param>
        /// <returns>Dictionary of feature names and their importance scores</returns>
        Task<Dictionary<string, double>> RankFeaturesAsync(
            IDataView data, 
            ModelType modelType, 
            List<FeatureDefinition> featureDefinitions, 
            string labelName);
    }
}
