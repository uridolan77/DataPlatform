using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Training;
using GenericDataPlatform.ML.Utils;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for automated machine learning
    /// </summary>
    public class AutoMLService : IAutoMLService
    {
        private readonly MLContext _mlContext;
        private readonly IModelTrainer _modelTrainer;
        private readonly IModelEvaluator _modelEvaluator;
        private readonly IFeatureSelectionService _featureSelectionService;
        private readonly IHyperparameterTuningService _hyperparameterTuningService;
        private readonly IModelManagementService _modelManagementService;
        private readonly ISchemaConverter _schemaConverter;
        private readonly ILogger<AutoMLService> _logger;
        
        public AutoMLService(
            MLContext mlContext,
            IModelTrainer modelTrainer,
            IModelEvaluator modelEvaluator,
            IFeatureSelectionService featureSelectionService,
            IHyperparameterTuningService hyperparameterTuningService,
            IModelManagementService modelManagementService,
            ISchemaConverter schemaConverter,
            ILogger<AutoMLService> logger)
        {
            _mlContext = mlContext;
            _modelTrainer = modelTrainer;
            _modelEvaluator = modelEvaluator;
            _featureSelectionService = featureSelectionService;
            _hyperparameterTuningService = hyperparameterTuningService;
            _modelManagementService = modelManagementService;
            _schemaConverter = schemaConverter;
            _logger = logger;
        }
        
        /// <summary>
        /// Runs AutoML to find the best model for the given data
        /// </summary>
        public async Task<AutoMLResult> RunAutoMLAsync(
            AutoMLConfig config,
            IEnumerable<Dictionary<string, object>> trainingData,
            IEnumerable<Dictionary<string, object>> validationData = null)
        {
            try
            {
                _logger.LogInformation("Starting AutoML for {ModelType} model with name {ModelName}",
                    config.ModelType, config.ModelName);
                
                var stopwatch = Stopwatch.StartNew();
                var result = new AutoMLResult();
                
                // Convert data to IDataView
                var trainingDataList = trainingData.ToList();
                var trainingDataView = ConvertToDataView(trainingDataList);
                
                // Split data if validation data not provided
                IDataView validationDataView;
                if (validationData != null)
                {
                    validationDataView = ConvertToDataView(validationData.ToList());
                }
                else
                {
                    var dataSplit = _mlContext.Data.TrainTestSplit(
                        trainingDataView, testFraction: config.ValidationSplitPercentage);
                    trainingDataView = dataSplit.TrainSet;
                    validationDataView = dataSplit.TestSet;
                }
                
                // Infer schema from data
                var schema = InferSchema(trainingDataList);
                var featureDefinitions = schema.Item1;
                var labelDefinitions = schema.Item2;
                
                // Get label column name
                var labelColumnName = labelDefinitions.FirstOrDefault()?.Name ?? "Label";
                
                // Perform feature selection if enabled
                List<string> selectedFeatures = featureDefinitions.Select(f => f.Name).ToList();
                Dictionary<string, double> featureImportance = new Dictionary<string, double>();
                
                if (config.EnableFeatureSelection)
                {
                    _logger.LogInformation("Performing feature selection");
                    
                    // Concatenate features for feature selection
                    var featurePipeline = _mlContext.Transforms.Concatenate("Features", selectedFeatures.ToArray());
                    var featureDataView = featurePipeline.Fit(trainingDataView).Transform(trainingDataView);
                    
                    // Select features
                    featureImportance = await _featureSelectionService.SelectFeaturesAsync(
                        featureDataView, config.ModelType, featureDefinitions, labelColumnName);
                    
                    // Update selected features
                    selectedFeatures = featureImportance.Keys.ToList();
                    
                    _logger.LogInformation("Selected {FeatureCount} features: {Features}",
                        selectedFeatures.Count, string.Join(", ", selectedFeatures));
                }
                
                // Get algorithms to try
                var algorithmsToTry = config.AlgorithmsToTry.Count > 0
                    ? config.AlgorithmsToTry
                    : GetSupportedAlgorithms(config.ModelType);
                
                _logger.LogInformation("Will try {AlgorithmCount} algorithms: {Algorithms}",
                    algorithmsToTry.Count, string.Join(", ", algorithmsToTry));
                
                // Try each algorithm
                var bestModel = new TrainedModel();
                var bestScore = double.MinValue;
                var bestAlgorithm = "";
                var bestHyperparameters = new Dictionary<string, string>();
                
                foreach (var algorithm in algorithmsToTry)
                {
                    if (result.ModelsTriedCount >= config.MaxModelsToTry)
                    {
                        _logger.LogInformation("Reached maximum number of models to try ({MaxModels})",
                            config.MaxModelsToTry);
                        break;
                    }
                    
                    if (stopwatch.Elapsed.TotalSeconds >= config.MaxTrainingTimeInSeconds)
                    {
                        _logger.LogInformation("Reached maximum training time ({MaxTime} seconds)",
                            config.MaxTrainingTimeInSeconds);
                        break;
                    }
                    
                    _logger.LogInformation("Trying algorithm: {Algorithm}", algorithm);
                    
                    // Create model definition
                    var modelDefinition = new ModelDefinition
                    {
                        Name = $"{config.ModelName}_{algorithm}",
                        Description = config.ModelDescription,
                        Type = config.ModelType,
                        Algorithm = algorithm,
                        Features = featureDefinitions.Where(f => selectedFeatures.Contains(f.Name)).ToList(),
                        Labels = labelDefinitions
                    };
                    
                    // Perform hyperparameter tuning if enabled
                    if (config.EnableHyperparameterTuning)
                    {
                        _logger.LogInformation("Performing hyperparameter tuning for {Algorithm}", algorithm);
                        
                        // Concatenate features for hyperparameter tuning
                        var featurePipeline = _mlContext.Transforms.Concatenate("Features", selectedFeatures.ToArray());
                        var featureDataView = featurePipeline.Fit(trainingDataView).Transform(trainingDataView);
                        var featureValidationDataView = featurePipeline.Fit(validationDataView).Transform(validationDataView);
                        
                        // Tune hyperparameters
                        var hyperparameters = await _hyperparameterTuningService.TuneHyperparametersAsync(
                            featureDataView,
                            featureValidationDataView,
                            config.ModelType,
                            algorithm,
                            "Features",
                            labelColumnName,
                            config.CustomHyperparameterSearchSpace);
                        
                        // Update model definition with tuned hyperparameters
                        modelDefinition.Hyperparameters = hyperparameters;
                        
                        _logger.LogInformation("Tuned hyperparameters: {Hyperparameters}",
                            string.Join(", ", hyperparameters.Select(h => $"{h.Key}={h.Value}")));
                    }
                    
                    // Train the model
                    var trainedModel = await _modelTrainer.TrainModelAsync(modelDefinition, trainingDataList);
                    result.ModelsTriedCount++;
                    
                    // Evaluate the model
                    var score = GetModelScore(trainedModel.Metrics, config.OptimizationMetric ?? GetDefaultOptimizationMetric(config.ModelType));
                    
                    _logger.LogInformation("Model {Algorithm} score: {Score}", algorithm, score);
                    
                    // Save all tried models if configured
                    if (config.SaveAllTriedModels)
                    {
                        result.AllTriedModels.Add(trainedModel);
                    }
                    
                    // Update best model if better score
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestModel = trainedModel;
                        bestAlgorithm = algorithm;
                        bestHyperparameters = modelDefinition.Hyperparameters;
                        
                        _logger.LogInformation("New best model: {Algorithm} with score {Score}", algorithm, score);
                    }
                }
                
                // Set result properties
                result.BestModel = bestModel;
                result.BestAlgorithm = bestAlgorithm;
                result.BestHyperparameters = bestHyperparameters;
                result.FeatureImportance = featureImportance;
                result.SelectedFeatures = selectedFeatures;
                result.TrainingTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
                
                stopwatch.Stop();
                
                _logger.LogInformation("AutoML completed in {ElapsedTime} seconds. Best algorithm: {BestAlgorithm} with score {BestScore}",
                    result.TrainingTimeInSeconds, result.BestAlgorithm, bestScore);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AutoML");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the list of supported algorithms for a given model type
        /// </summary>
        public List<string> GetSupportedAlgorithms(ModelType modelType)
        {
            return modelType switch
            {
                ModelType.BinaryClassification => new List<string>
                {
                    "LogisticRegression",
                    "SVM",
                    "FastForest",
                    "FastTree"
                },
                
                ModelType.MultiClassClassification => new List<string>
                {
                    "NaiveBayes",
                    "SDCA",
                    "LBFGS"
                },
                
                ModelType.Regression => new List<string>
                {
                    "FastForest",
                    "FastTree",
                    "SDCA"
                },
                
                ModelType.Clustering => new List<string>
                {
                    "KMeans"
                },
                
                ModelType.Recommendation => new List<string>
                {
                    "MatrixFactorization"
                },
                
                ModelType.AnomalyDetection => new List<string>
                {
                    "RandomizedPca",
                    "SrCnn"
                },
                
                ModelType.TimeSeries => new List<string>
                {
                    "Ssa",
                    "Arima"
                },
                
                _ => new List<string>()
            };
        }
        
        /// <summary>
        /// Gets the default optimization metric for a given model type
        /// </summary>
        public string GetDefaultOptimizationMetric(ModelType modelType)
        {
            return modelType switch
            {
                ModelType.BinaryClassification => "AUC",
                ModelType.MultiClassClassification => "MicroAccuracy",
                ModelType.Regression => "RSquared",
                ModelType.Clustering => "NormalizedMutualInformation",
                ModelType.Recommendation => "NDCG",
                ModelType.AnomalyDetection => "DetectionRate",
                ModelType.TimeSeries => "RMSE",
                _ => "AUC"
            };
        }
        
        /// <summary>
        /// Converts a list of dictionaries to an IDataView
        /// </summary>
        private IDataView ConvertToDataView(List<Dictionary<string, object>> data)
        {
            if (data == null || data.Count == 0)
            {
                throw new ArgumentException("Data cannot be null or empty");
            }
            
            // Create a data view builder
            var builder = new ArrayDataViewBuilder(_mlContext);
            
            // Add rows
            foreach (var row in data)
            {
                builder.AddRow(row);
            }
            
            // Build the data view
            return builder.GetDataView();
        }
        
        /// <summary>
        /// Infers schema from data
        /// </summary>
        private (List<FeatureDefinition>, List<LabelDefinition>) InferSchema(List<Dictionary<string, object>> data)
        {
            if (data == null || data.Count == 0)
            {
                throw new ArgumentException("Data cannot be null or empty");
            }
            
            var featureDefinitions = new List<FeatureDefinition>();
            var labelDefinitions = new List<LabelDefinition>();
            
            // Get column names from first row
            var columnNames = data[0].Keys.ToList();
            
            // Assume last column is the label
            var labelName = columnNames.Last();
            
            // Create feature definitions for all columns except the label
            foreach (var columnName in columnNames.Where(c => c != labelName))
            {
                // Infer data type from values
                var values = data.Select(d => d[columnName]).ToList();
                var dataType = InferDataType(values);
                
                featureDefinitions.Add(new FeatureDefinition
                {
                    Name = columnName,
                    Description = $"Feature: {columnName}",
                    DataType = dataType,
                    IsRequired = true
                });
            }
            
            // Create label definition
            var labelValues = data.Select(d => d[labelName]).ToList();
            var labelDataType = InferLabelDataType(labelValues);
            
            labelDefinitions.Add(new LabelDefinition
            {
                Name = labelName,
                Description = $"Label: {labelName}",
                DataType = labelDataType
            });
            
            return (featureDefinitions, labelDefinitions);
        }
        
        /// <summary>
        /// Infers data type from values
        /// </summary>
        private FeatureDataType InferDataType(List<object> values)
        {
            // Check if all values are null or empty
            if (values.All(v => v == null || (v is string s && string.IsNullOrEmpty(s))))
            {
                return FeatureDataType.String;
            }
            
            // Get first non-null value
            var firstValue = values.FirstOrDefault(v => v != null);
            
            if (firstValue is bool)
            {
                return FeatureDataType.Boolean;
            }
            else if (firstValue is int or long or short or byte)
            {
                return FeatureDataType.Integer;
            }
            else if (firstValue is float or double or decimal)
            {
                return FeatureDataType.Float;
            }
            else if (firstValue is DateTime)
            {
                return FeatureDataType.DateTime;
            }
            else if (firstValue is string)
            {
                // Check if it's categorical (few unique values)
                var uniqueValues = values.Where(v => v != null).Distinct().Count();
                var totalValues = values.Count(v => v != null);
                
                if (uniqueValues <= 20 || (totalValues > 0 && (double)uniqueValues / totalValues <= 0.1))
                {
                    return FeatureDataType.Categorical;
                }
                else
                {
                    return FeatureDataType.Text;
                }
            }
            else
            {
                return FeatureDataType.String;
            }
        }
        
        /// <summary>
        /// Infers label data type from values
        /// </summary>
        private LabelDataType InferLabelDataType(List<object> values)
        {
            // Check if all values are null or empty
            if (values.All(v => v == null || (v is string s && string.IsNullOrEmpty(s))))
            {
                return LabelDataType.Categorical;
            }
            
            // Get first non-null value
            var firstValue = values.FirstOrDefault(v => v != null);
            
            if (firstValue is bool)
            {
                return LabelDataType.Binary;
            }
            else if (firstValue is int or long or short or byte or float or double or decimal)
            {
                // Check if it's binary (only 0 and 1)
                var uniqueValues = values.Where(v => v != null).Select(v => Convert.ToDouble(v)).Distinct().ToList();
                if (uniqueValues.Count == 2 && uniqueValues.Contains(0) && uniqueValues.Contains(1))
                {
                    return LabelDataType.Binary;
                }
                
                // Check if it's categorical (few unique values)
                if (uniqueValues.Count <= 20)
                {
                    return LabelDataType.Categorical;
                }
                else
                {
                    return LabelDataType.Continuous;
                }
            }
            else if (firstValue is string)
            {
                // Check if it's binary (only two unique values)
                var uniqueValues = values.Where(v => v != null).Distinct().ToList();
                if (uniqueValues.Count == 2)
                {
                    return LabelDataType.Binary;
                }
                else
                {
                    return LabelDataType.Categorical;
                }
            }
            else
            {
                return LabelDataType.Categorical;
            }
        }
        
        /// <summary>
        /// Gets model score based on the optimization metric
        /// </summary>
        private double GetModelScore(Dictionary<string, double> metrics, string optimizationMetric)
        {
            // If the metric exists in the metrics dictionary, use it
            if (metrics.TryGetValue(optimizationMetric, out var score))
            {
                return score;
            }
            
            // Otherwise, try to find a similar metric
            var metricKey = metrics.Keys.FirstOrDefault(k => k.Contains(optimizationMetric, StringComparison.OrdinalIgnoreCase));
            if (metricKey != null)
            {
                return metrics[metricKey];
            }
            
            // If no matching metric is found, use the first metric
            return metrics.Values.FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Helper class to build an IDataView from a list of dictionaries
    /// </summary>
    internal class ArrayDataViewBuilder
    {
        private readonly MLContext _mlContext;
        private readonly List<Dictionary<string, object>> _data = new List<Dictionary<string, object>>();
        
        public ArrayDataViewBuilder(MLContext mlContext)
        {
            _mlContext = mlContext;
        }
        
        public void AddRow(Dictionary<string, object> row)
        {
            _data.Add(row);
        }
        
        public IDataView GetDataView()
        {
            if (_data.Count == 0)
            {
                throw new InvalidOperationException("No data added to the builder");
            }
            
            // Create a list of columns
            var columns = _data[0].Keys.ToList();
            
            // Create a list of rows
            var rows = new List<object[]>();
            foreach (var row in _data)
            {
                var values = new object[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    values[i] = row[columns[i]];
                }
                rows.Add(values);
            }
            
            // Create schema
            var schemaDefinition = new SchemaDefinition();
            for (int i = 0; i < columns.Count; i++)
            {
                var columnName = columns[i];
                var columnType = InferColumnType(_data.Select(d => d[columnName]).ToList());
                
                schemaDefinition.Add(new SchemaDefinition.Column
                {
                    Name = columnName,
                    ColumnType = columnType
                });
            }
            
            // Create data view
            return _mlContext.Data.LoadFromEnumerable(rows, schemaDefinition);
        }
        
        private DataViewType InferColumnType(List<object> values)
        {
            // Get first non-null value
            var firstValue = values.FirstOrDefault(v => v != null);
            
            if (firstValue is bool)
            {
                return new BooleanDataViewType();
            }
            else if (firstValue is int or long or short or byte)
            {
                return new NumberDataViewType(DataKind.Int32);
            }
            else if (firstValue is float)
            {
                return new NumberDataViewType(DataKind.Single);
            }
            else if (firstValue is double)
            {
                return new NumberDataViewType(DataKind.Double);
            }
            else if (firstValue is DateTime)
            {
                return new DateTimeDataViewType();
            }
            else
            {
                return new TextDataViewType();
            }
        }
    }
}
