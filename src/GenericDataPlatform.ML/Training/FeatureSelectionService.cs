using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Service for automated feature selection
    /// </summary>
    public class FeatureSelectionService : IFeatureSelectionService
    {
        private readonly MLContext _mlContext;
        private readonly ILogger<FeatureSelectionService> _logger;
        
        public FeatureSelectionService(MLContext mlContext, ILogger<FeatureSelectionService> logger)
        {
            _mlContext = mlContext;
            _logger = logger;
        }
        
        /// <summary>
        /// Selects the most important features for a given dataset and model type
        /// </summary>
        public async Task<Dictionary<string, double>> SelectFeaturesAsync(
            IDataView data, 
            ModelType modelType, 
            List<FeatureDefinition> featureDefinitions, 
            string labelName, 
            int? maxFeatures = null)
        {
            try
            {
                _logger.LogInformation("Starting feature selection for model type {ModelType}", modelType);
                
                // Rank features by importance
                var featureRanking = await RankFeaturesAsync(data, modelType, featureDefinitions, labelName);
                
                // Determine how many features to select
                int featuresToSelect = maxFeatures ?? DetermineOptimalFeatureCount(featureRanking, featureDefinitions.Count);
                
                _logger.LogInformation("Selecting top {FeatureCount} features out of {TotalFeatures}", 
                    featuresToSelect, featureDefinitions.Count);
                
                // Select top N features
                var selectedFeatures = featureRanking
                    .OrderByDescending(f => f.Value)
                    .Take(featuresToSelect)
                    .ToDictionary(f => f.Key, f => f.Value);
                
                return selectedFeatures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during feature selection");
                throw;
            }
        }
        
        /// <summary>
        /// Ranks features by importance for a given dataset and model type
        /// </summary>
        public async Task<Dictionary<string, double>> RankFeaturesAsync(
            IDataView data, 
            ModelType modelType, 
            List<FeatureDefinition> featureDefinitions, 
            string labelName)
        {
            try
            {
                _logger.LogInformation("Ranking features for model type {ModelType}", modelType);
                
                // Get feature names
                var featureNames = featureDefinitions.Select(f => f.Name).ToArray();
                
                // Create a pipeline to concatenate all features
                var pipeline = _mlContext.Transforms.Concatenate("Features", featureNames);
                
                // Add a trainer based on model type to get feature importance
                IEstimator<ITransformer> trainer = modelType switch
                {
                    ModelType.BinaryClassification => _mlContext.BinaryClassification.Trainers.FastTree(
                        labelColumnName: labelName, featureColumnName: "Features", numberOfTrees: 20),
                    
                    ModelType.MultiClassClassification => _mlContext.MulticlassClassification.Trainers.OneVersusAll(
                        _mlContext.BinaryClassification.Trainers.FastTree(
                            labelColumnName: labelName, featureColumnName: "Features", numberOfTrees: 20)),
                    
                    ModelType.Regression => _mlContext.Regression.Trainers.FastTree(
                        labelColumnName: labelName, featureColumnName: "Features", numberOfTrees: 20),
                    
                    ModelType.Clustering => _mlContext.Clustering.Trainers.KMeans(
                        featureColumnName: "Features", numberOfClusters: 5),
                    
                    _ => throw new NotSupportedException($"Feature selection for model type {modelType} is not supported")
                };
                
                pipeline = pipeline.Append(trainer);
                
                // Train the model to get feature importance
                var model = pipeline.Fit(data);
                
                // Extract feature importance
                VBuffer<float> weights = default;
                
                // Different model types store feature importance differently
                if (modelType == ModelType.BinaryClassification)
                {
                    var fastTreeBinary = model.LastTransformer as FastTreeBinaryModelParameters;
                    fastTreeBinary?.GetFeatureWeights(ref weights);
                }
                else if (modelType == ModelType.Regression)
                {
                    var fastTreeRegression = model.LastTransformer as FastTreeRegressionModelParameters;
                    fastTreeRegression?.GetFeatureWeights(ref weights);
                }
                else if (modelType == ModelType.Clustering)
                {
                    // For clustering, we use the distance to centroids as a proxy for importance
                    // This is a simplified approach
                    var kMeansModel = model.LastTransformer as KMeansModelParameters;
                    if (kMeansModel != null)
                    {
                        kMeansModel.GetClusterCentroids(ref weights, out _);
                    }
                }
                else if (modelType == ModelType.MultiClassClassification)
                {
                    // For multi-class, we need to extract from the underlying binary classifiers
                    // This is a simplified approach
                    var multiClassModel = model.LastTransformer as OneVersusAllModelParameters;
                    if (multiClassModel != null && multiClassModel.SubModels.Length > 0)
                    {
                        var firstBinaryModel = multiClassModel.SubModels[0] as FastTreeBinaryModelParameters;
                        firstBinaryModel?.GetFeatureWeights(ref weights);
                    }
                }
                
                // Convert to dictionary
                var featureImportance = new Dictionary<string, double>();
                var weightValues = weights.GetValues().ToArray();
                
                // Normalize weights if we have them
                if (weightValues.Length > 0)
                {
                    // Get absolute values for importance
                    var absWeights = weightValues.Select(Math.Abs).ToArray();
                    
                    // Normalize to sum to 1
                    var sum = absWeights.Sum();
                    if (sum > 0)
                    {
                        for (int i = 0; i < featureNames.Length && i < absWeights.Length; i++)
                        {
                            featureImportance[featureNames[i]] = absWeights[i] / sum;
                        }
                    }
                }
                
                // If we couldn't get weights, assign equal importance
                if (featureImportance.Count == 0)
                {
                    double equalWeight = 1.0 / featureNames.Length;
                    foreach (var feature in featureNames)
                    {
                        featureImportance[feature] = equalWeight;
                    }
                }
                
                return featureImportance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during feature ranking");
                throw;
            }
        }
        
        /// <summary>
        /// Determines the optimal number of features to select based on importance scores
        /// </summary>
        private int DetermineOptimalFeatureCount(Dictionary<string, double> featureRanking, int totalFeatures)
        {
            // Sort features by importance
            var sortedImportance = featureRanking.OrderByDescending(f => f.Value).Select(f => f.Value).ToArray();
            
            // Calculate cumulative importance
            var cumulativeImportance = new double[sortedImportance.Length];
            double sum = 0;
            
            for (int i = 0; i < sortedImportance.Length; i++)
            {
                sum += sortedImportance[i];
                cumulativeImportance[i] = sum;
            }
            
            // Find the "elbow point" where adding more features gives diminishing returns
            // Using a simple heuristic: select features that contribute to 95% of total importance
            const double importanceThreshold = 0.95;
            
            for (int i = 0; i < cumulativeImportance.Length; i++)
            {
                if (cumulativeImportance[i] / sum >= importanceThreshold)
                {
                    return i + 1; // +1 because we're counting features
                }
            }
            
            // If we can't determine, use a reasonable default (e.g., 80% of features)
            return Math.Max(1, (int)(totalFeatures * 0.8));
        }
    }
}
