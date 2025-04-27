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
    /// Service for detecting concept drift
    /// </summary>
    public class ConceptDriftDetector : IConceptDriftDetector
    {
        private readonly MLContext _mlContext;
        private readonly IModelEvaluator _modelEvaluator;
        private readonly ILogger<ConceptDriftDetector> _logger;
        
        public ConceptDriftDetector(
            MLContext mlContext,
            IModelEvaluator modelEvaluator,
            ILogger<ConceptDriftDetector> logger)
        {
            _mlContext = mlContext;
            _modelEvaluator = modelEvaluator;
            _logger = logger;
        }
        
        /// <summary>
        /// Detects concept drift between two datasets
        /// </summary>
        public async Task<ConceptDriftDetectionResult> DetectDriftAsync(
            ITransformer model,
            ModelMetadata metadata,
            IDataView newData,
            IDataView referenceData)
        {
            try
            {
                _logger.LogInformation("Detecting concept drift for model {ModelName} version {ModelVersion}",
                    metadata.Name, metadata.Version);
                
                // Create result
                var result = new ConceptDriftDetectionResult
                {
                    ModelName = metadata.Name,
                    ModelVersion = metadata.Version,
                    DriftDetected = false,
                    DriftConfidence = 0,
                    DriftType = DriftType.None,
                    RecommendedAction = DriftAction.NoAction
                };
                
                // Evaluate model on reference data
                var referenceMetrics = _modelEvaluator.EvaluateModel(metadata.Definition, model, referenceData);
                result.BaselineMetrics = referenceMetrics;
                
                // Evaluate model on new data
                var newMetrics = _modelEvaluator.EvaluateModel(metadata.Definition, model, newData);
                result.CurrentMetrics = newMetrics;
                
                // Detect feature drift
                var featureDrift = await DetectFeatureDriftAsync(metadata, newData, referenceData);
                result.DriftContributors = featureDrift;
                
                // Calculate overall drift score
                var driftScore = CalculateOverallDriftScore(referenceMetrics, newMetrics, featureDrift);
                
                // Determine if drift is detected
                const double driftThreshold = 0.05; // 5% change
                if (driftScore > driftThreshold)
                {
                    result.DriftDetected = true;
                    result.DriftConfidence = Math.Min(1.0, driftScore / 0.2); // Scale to 0-1
                    result.DriftType = DetermineDriftType(featureDrift);
                }
                
                _logger.LogInformation("Drift detection completed for model {ModelName} version {ModelVersion}. Drift detected: {DriftDetected}, Score: {DriftScore}",
                    metadata.Name, metadata.Version, result.DriftDetected, driftScore);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting concept drift for model {ModelName} version {ModelVersion}",
                    metadata.Name, metadata.Version);
                throw;
            }
        }
        
        /// <summary>
        /// Detects feature drift between two datasets
        /// </summary>
        public async Task<Dictionary<string, double>> DetectFeatureDriftAsync(
            ModelMetadata metadata,
            IDataView newData,
            IDataView referenceData)
        {
            try
            {
                _logger.LogInformation("Detecting feature drift for model {ModelName} version {ModelVersion}",
                    metadata.Name, metadata.Version);
                
                var featureDrift = new Dictionary<string, double>();
                
                // Get feature names
                var featureNames = metadata.InputSchema.Select(f => f.Name).ToArray();
                
                // Calculate drift for each feature
                foreach (var featureName in featureNames)
                {
                    // Get feature data type
                    var featureDefinition = metadata.InputSchema.FirstOrDefault(f => f.Name == featureName);
                    if (featureDefinition == null)
                    {
                        continue;
                    }
                    
                    // Calculate drift based on data type
                    double drift = featureDefinition.DataType switch
                    {
                        FeatureDataType.Boolean => CalculateCategoricalDrift(referenceData, newData, featureName),
                        FeatureDataType.Integer => CalculateNumericalDrift(referenceData, newData, featureName),
                        FeatureDataType.Float => CalculateNumericalDrift(referenceData, newData, featureName),
                        FeatureDataType.DateTime => CalculateNumericalDrift(referenceData, newData, featureName),
                        FeatureDataType.Categorical => CalculateCategoricalDrift(referenceData, newData, featureName),
                        FeatureDataType.String => CalculateCategoricalDrift(referenceData, newData, featureName),
                        FeatureDataType.Text => CalculateTextDrift(referenceData, newData, featureName),
                        _ => 0
                    };
                    
                    featureDrift[featureName] = drift;
                }
                
                return featureDrift;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting feature drift for model {ModelName} version {ModelVersion}",
                    metadata.Name, metadata.Version);
                throw;
            }
        }
        
        /// <summary>
        /// Calculates numerical drift between two datasets
        /// </summary>
        private double CalculateNumericalDrift(IDataView referenceData, IDataView newData, string featureName)
        {
            try
            {
                // Calculate statistics for reference data
                var referencePipeline = _mlContext.Transforms.CalculateStatistics(featureName);
                var referenceTransformer = referencePipeline.Fit(referenceData);
                var referenceStats = referenceTransformer.GetOutputSchema().GetColumnOrNull(featureName + ".stats");
                
                // Calculate statistics for new data
                var newPipeline = _mlContext.Transforms.CalculateStatistics(featureName);
                var newTransformer = newPipeline.Fit(newData);
                var newStats = newTransformer.GetOutputSchema().GetColumnOrNull(featureName + ".stats");
                
                // If we couldn't get statistics, return 0
                if (referenceStats == null || newStats == null)
                {
                    return 0;
                }
                
                // Get mean and standard deviation
                var referenceMean = GetStatisticValue(referenceStats, "Mean");
                var referenceStdDev = GetStatisticValue(referenceStats, "StandardDeviation");
                var newMean = GetStatisticValue(newStats, "Mean");
                var newStdDev = GetStatisticValue(newStats, "StandardDeviation");
                
                // Calculate drift using Kolmogorov-Smirnov statistic approximation
                // This is a simplified approach - in a real implementation, you would use a proper KS test
                var meanDrift = Math.Abs(referenceMean - newMean) / (referenceStdDev + 1e-10);
                var stdDevDrift = Math.Abs(referenceStdDev - newStdDev) / (referenceStdDev + 1e-10);
                
                return (meanDrift + stdDevDrift) / 2;
            }
            catch (Exception)
            {
                // If calculation fails, return 0
                return 0;
            }
        }
        
        /// <summary>
        /// Calculates categorical drift between two datasets
        /// </summary>
        private double CalculateCategoricalDrift(IDataView referenceData, IDataView newData, string featureName)
        {
            try
            {
                // Calculate value counts for reference data
                var referenceValueCounts = GetValueCounts(referenceData, featureName);
                
                // Calculate value counts for new data
                var newValueCounts = GetValueCounts(newData, featureName);
                
                // Calculate Jensen-Shannon divergence
                return CalculateJensenShannonDivergence(referenceValueCounts, newValueCounts);
            }
            catch (Exception)
            {
                // If calculation fails, return 0
                return 0;
            }
        }
        
        /// <summary>
        /// Calculates text drift between two datasets
        /// </summary>
        private double CalculateTextDrift(IDataView referenceData, IDataView newData, string featureName)
        {
            // For text data, we'll use a simplified approach based on word frequencies
            // In a real implementation, you would use more sophisticated NLP techniques
            
            try
            {
                // Calculate word frequencies for reference data
                var referenceWordFreq = GetWordFrequencies(referenceData, featureName);
                
                // Calculate word frequencies for new data
                var newWordFreq = GetWordFrequencies(newData, featureName);
                
                // Calculate Jensen-Shannon divergence
                return CalculateJensenShannonDivergence(referenceWordFreq, newWordFreq);
            }
            catch (Exception)
            {
                // If calculation fails, return 0
                return 0;
            }
        }
        
        /// <summary>
        /// Gets value counts for a categorical feature
        /// </summary>
        private Dictionary<string, double> GetValueCounts(IDataView data, string featureName)
        {
            var valueCounts = new Dictionary<string, double>();
            
            // Get column
            var column = data.GetColumn<object>(featureName).ToList();
            
            // Count values
            var totalCount = column.Count;
            foreach (var value in column)
            {
                var strValue = value?.ToString() ?? "null";
                if (valueCounts.ContainsKey(strValue))
                {
                    valueCounts[strValue]++;
                }
                else
                {
                    valueCounts[strValue] = 1;
                }
            }
            
            // Convert to probabilities
            foreach (var key in valueCounts.Keys.ToList())
            {
                valueCounts[key] /= totalCount;
            }
            
            return valueCounts;
        }
        
        /// <summary>
        /// Gets word frequencies for a text feature
        /// </summary>
        private Dictionary<string, double> GetWordFrequencies(IDataView data, string featureName)
        {
            var wordFreq = new Dictionary<string, double>();
            
            // Get column
            var column = data.GetColumn<object>(featureName).ToList();
            
            // Count words
            var totalWords = 0;
            foreach (var value in column)
            {
                var text = value?.ToString() ?? "";
                var words = text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' },
                    StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var word in words)
                {
                    var normalizedWord = word.ToLowerInvariant();
                    if (wordFreq.ContainsKey(normalizedWord))
                    {
                        wordFreq[normalizedWord]++;
                    }
                    else
                    {
                        wordFreq[normalizedWord] = 1;
                    }
                    totalWords++;
                }
            }
            
            // Convert to probabilities
            if (totalWords > 0)
            {
                foreach (var key in wordFreq.Keys.ToList())
                {
                    wordFreq[key] /= totalWords;
                }
            }
            
            return wordFreq;
        }
        
        /// <summary>
        /// Calculates Jensen-Shannon divergence between two probability distributions
        /// </summary>
        private double CalculateJensenShannonDivergence(
            Dictionary<string, double> dist1,
            Dictionary<string, double> dist2)
        {
            // Get all keys
            var allKeys = new HashSet<string>(dist1.Keys.Concat(dist2.Keys));
            
            // Create the midpoint distribution
            var midpoint = new Dictionary<string, double>();
            foreach (var key in allKeys)
            {
                var p1 = dist1.TryGetValue(key, out var val1) ? val1 : 0;
                var p2 = dist2.TryGetValue(key, out var val2) ? val2 : 0;
                midpoint[key] = (p1 + p2) / 2;
            }
            
            // Calculate KL divergence from dist1 to midpoint
            var kl1 = 0.0;
            foreach (var key in dist1.Keys)
            {
                var p = dist1[key];
                var m = midpoint[key];
                if (p > 0 && m > 0)
                {
                    kl1 += p * Math.Log(p / m);
                }
            }
            
            // Calculate KL divergence from dist2 to midpoint
            var kl2 = 0.0;
            foreach (var key in dist2.Keys)
            {
                var p = dist2[key];
                var m = midpoint[key];
                if (p > 0 && m > 0)
                {
                    kl2 += p * Math.Log(p / m);
                }
            }
            
            // Jensen-Shannon divergence is the average of the two KL divergences
            return (kl1 + kl2) / 2;
        }
        
        /// <summary>
        /// Gets a statistic value from a column
        /// </summary>
        private double GetStatisticValue(DataViewSchema.Column column, string statName)
        {
            // This is a placeholder - in a real implementation, you would extract the value from the column metadata
            return 0;
        }
        
        /// <summary>
        /// Calculates overall drift score
        /// </summary>
        private double CalculateOverallDriftScore(
            Dictionary<string, double> referenceMetrics,
            Dictionary<string, double> newMetrics,
            Dictionary<string, double> featureDrift)
        {
            // Calculate performance drift
            var performanceDrift = CalculatePerformanceDrift(referenceMetrics, newMetrics);
            
            // Calculate average feature drift
            var avgFeatureDrift = featureDrift.Values.Average();
            
            // Combine the two (giving more weight to performance drift)
            return (performanceDrift * 0.7) + (avgFeatureDrift * 0.3);
        }
        
        /// <summary>
        /// Calculates performance drift
        /// </summary>
        private double CalculatePerformanceDrift(
            Dictionary<string, double> referenceMetrics,
            Dictionary<string, double> newMetrics)
        {
            var drifts = new List<double>();
            
            // Compare each metric
            foreach (var metric in referenceMetrics.Keys)
            {
                if (newMetrics.TryGetValue(metric, out var newValue))
                {
                    var refValue = referenceMetrics[metric];
                    
                    // Calculate relative change
                    if (refValue != 0)
                    {
                        var relativeChange = Math.Abs((newValue - refValue) / refValue);
                        drifts.Add(relativeChange);
                    }
                }
            }
            
            // Return average drift
            return drifts.Count > 0 ? drifts.Average() : 0;
        }
        
        /// <summary>
        /// Determines the type of drift
        /// </summary>
        private DriftType DetermineDriftType(Dictionary<string, double> featureDrift)
        {
            // This is a simplified approach - in a real implementation, you would use more sophisticated techniques
            
            // If many features have drifted, it's likely a sudden drift
            if (featureDrift.Count(f => f.Value > 0.1) > featureDrift.Count / 2)
            {
                return DriftType.Sudden;
            }
            
            // If only a few features have drifted significantly, it's likely an incremental drift
            if (featureDrift.Any(f => f.Value > 0.2))
            {
                return DriftType.Incremental;
            }
            
            // If drift is moderate across many features, it's likely a gradual drift
            return DriftType.Gradual;
        }
    }
}
