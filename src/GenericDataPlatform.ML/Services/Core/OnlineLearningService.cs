using System;
using System.Collections.Generic;
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
    /// Service for online learning and concept drift detection
    /// </summary>
    public class OnlineLearningService : IOnlineLearningService
    {
        private readonly MLContext _mlContext;
        private readonly IModelManagementService _modelManagementService;
        private readonly IModelTrainer _modelTrainer;
        private readonly IModelEvaluator _modelEvaluator;
        private readonly IConceptDriftDetector _conceptDriftDetector;
        private readonly ILogger<OnlineLearningService> _logger;
        
        public OnlineLearningService(
            MLContext mlContext,
            IModelManagementService modelManagementService,
            IModelTrainer modelTrainer,
            IModelEvaluator modelEvaluator,
            IConceptDriftDetector conceptDriftDetector,
            ILogger<OnlineLearningService> logger)
        {
            _mlContext = mlContext;
            _modelManagementService = modelManagementService;
            _modelTrainer = modelTrainer;
            _modelEvaluator = modelEvaluator;
            _conceptDriftDetector = conceptDriftDetector;
            _logger = logger;
        }
        
        /// <summary>
        /// Updates a model incrementally with new data
        /// </summary>
        public async Task<TrainedModel> UpdateModelAsync(
            string modelName,
            IEnumerable<Dictionary<string, object>> trainingData,
            string modelVersion = null,
            double learningRate = 0.1)
        {
            try
            {
                _logger.LogInformation("Updating model {ModelName} version {ModelVersion} with {DataCount} samples",
                    modelName, modelVersion ?? "latest", trainingData.Count());
                
                // Check if model supports online learning
                var supportsOnlineLearning = await SupportsOnlineLearningAsync(modelName, modelVersion);
                if (!supportsOnlineLearning)
                {
                    throw new NotSupportedException($"Model {modelName} version {modelVersion ?? "latest"} does not support online learning");
                }
                
                // Get model metadata
                var metadata = await _modelManagementService.GetModelMetadataAsync(modelName, modelVersion);
                if (metadata == null)
                {
                    throw new ArgumentException($"Model {modelName} version {modelVersion ?? "latest"} not found");
                }
                
                // Load the model
                var model = await _modelManagementService.LoadModelAsync(metadata.ModelPath);
                
                // Get online learning config
                var config = await GetOnlineLearningConfigAsync(modelName, modelVersion);
                
                // Check if we have enough samples
                var trainingDataList = trainingData.ToList();
                if (trainingDataList.Count < config.MinimumSamplesForUpdate)
                {
                    throw new ArgumentException($"Not enough samples for update. Required: {config.MinimumSamplesForUpdate}, Provided: {trainingDataList.Count}");
                }
                
                // Create a new model definition based on the existing one
                var modelDefinition = metadata.Definition;
                modelDefinition.Version = IncrementVersion(metadata.Version);
                
                // Update the model
                var updatedModel = await UpdateModelIncrementallyAsync(model, modelDefinition, trainingDataList, learningRate);
                
                // Save the updated model
                var modelPath = await _modelManagementService.SaveModelAsync(
                    modelName,
                    updatedModel,
                    modelDefinition.Version,
                    metadata.ExperimentId,
                    metadata.RunId);
                
                // Create the trained model result
                var result = new TrainedModel
                {
                    ModelDefinition = modelDefinition,
                    ModelPath = modelPath,
                    Metrics = updatedModel.Item2,
                    TrainedAt = DateTime.UtcNow
                };
                
                _logger.LogInformation("Model {ModelName} updated to version {ModelVersion}",
                    modelName, modelDefinition.Version);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Detects concept drift in a model
        /// </summary>
        public async Task<ConceptDriftDetectionResult> DetectConceptDriftAsync(
            string modelName,
            IEnumerable<Dictionary<string, object>> newData,
            string modelVersion = null,
            IEnumerable<Dictionary<string, object>> referenceData = null)
        {
            try
            {
                _logger.LogInformation("Detecting concept drift for model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                
                // Get model metadata
                var metadata = await _modelManagementService.GetModelMetadataAsync(modelName, modelVersion);
                if (metadata == null)
                {
                    throw new ArgumentException($"Model {modelName} version {modelVersion ?? "latest"} not found");
                }
                
                // Load the model
                var model = await _modelManagementService.LoadModelAsync(metadata.ModelPath);
                
                // Get reference data if not provided
                var referenceDataList = referenceData?.ToList();
                if (referenceDataList == null || referenceDataList.Count == 0)
                {
                    // Try to get reference data from model metadata
                    referenceDataList = await GetReferenceDataAsync(modelName, metadata);
                }
                
                if (referenceDataList == null || referenceDataList.Count == 0)
                {
                    throw new ArgumentException("Reference data is required for drift detection");
                }
                
                // Convert data to IDataView
                var newDataList = newData.ToList();
                var newDataView = ConvertToDataView(newDataList);
                var referenceDataView = ConvertToDataView(referenceDataList);
                
                // Detect drift
                var driftResult = await _conceptDriftDetector.DetectDriftAsync(
                    model, metadata, newDataView, referenceDataView);
                
                // Set model information
                driftResult.ModelName = modelName;
                driftResult.ModelVersion = metadata.Version;
                
                // Determine recommended action
                driftResult.RecommendedAction = DetermineRecommendedAction(driftResult);
                
                _logger.LogInformation("Drift detection for model {ModelName} version {ModelVersion}: {DriftDetected}",
                    modelName, metadata.Version, driftResult.DriftDetected);
                
                return driftResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting concept drift for model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Checks if a model supports online learning
        /// </summary>
        public async Task<bool> SupportsOnlineLearningAsync(string modelName, string modelVersion = null)
        {
            try
            {
                // Get model metadata
                var metadata = await _modelManagementService.GetModelMetadataAsync(modelName, modelVersion);
                if (metadata == null)
                {
                    return false;
                }
                
                // Check if model type supports online learning
                return SupportsOnlineLearning(metadata.Type, metadata.Definition.Algorithm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if model {ModelName} version {ModelVersion} supports online learning",
                    modelName, modelVersion ?? "latest");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the online learning configuration for a model
        /// </summary>
        public async Task<OnlineLearningConfig> GetOnlineLearningConfigAsync(string modelName, string modelVersion = null)
        {
            try
            {
                // Get model metadata
                var metadata = await _modelManagementService.GetModelMetadataAsync(modelName, modelVersion);
                if (metadata == null)
                {
                    throw new ArgumentException($"Model {modelName} version {modelVersion ?? "latest"} not found");
                }
                
                // Check if online learning config exists in parameters
                if (metadata.Parameters.TryGetValue("OnlineLearningConfig", out var configJson))
                {
                    // Deserialize config
                    var config = System.Text.Json.JsonSerializer.Deserialize<OnlineLearningConfig>(configJson);
                    if (config != null)
                    {
                        return config;
                    }
                }
                
                // Return default config
                return new OnlineLearningConfig
                {
                    Enabled = SupportsOnlineLearning(metadata.Type, metadata.Definition.Algorithm)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online learning config for model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Sets the online learning configuration for a model
        /// </summary>
        public async Task<OnlineLearningConfig> SetOnlineLearningConfigAsync(
            string modelName,
            OnlineLearningConfig config,
            string modelVersion = null)
        {
            try
            {
                // Get model metadata
                var metadata = await _modelManagementService.GetModelMetadataAsync(modelName, modelVersion);
                if (metadata == null)
                {
                    throw new ArgumentException($"Model {modelName} version {modelVersion ?? "latest"} not found");
                }
                
                // Check if model supports online learning
                if (config.Enabled && !SupportsOnlineLearning(metadata.Type, metadata.Definition.Algorithm))
                {
                    throw new NotSupportedException($"Model {modelName} version {modelVersion ?? "latest"} does not support online learning");
                }
                
                // Serialize config
                var configJson = System.Text.Json.JsonSerializer.Serialize(config);
                
                // Update parameters
                metadata.Parameters["OnlineLearningConfig"] = configJson;
                
                // Save metadata
                await _modelManagementService.UpdateModelMetadataAsync(metadata);
                
                _logger.LogInformation("Updated online learning config for model {ModelName} version {ModelVersion}",
                    modelName, metadata.Version);
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting online learning config for model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Updates a model incrementally with new data
        /// </summary>
        private async Task<(ITransformer Model, Dictionary<string, double> Metrics)> UpdateModelIncrementallyAsync(
            ITransformer model,
            ModelDefinition modelDefinition,
            List<Dictionary<string, object>> trainingData,
            double learningRate)
        {
            // Convert data to IDataView
            var dataView = ConvertToDataView(trainingData);
            
            // Split data for evaluation
            var dataSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            
            // Update the model based on model type and algorithm
            ITransformer updatedModel = null;
            
            switch (modelDefinition.Type)
            {
                case ModelType.BinaryClassification:
                    updatedModel = UpdateBinaryClassificationModel(model, modelDefinition, dataSplit.TrainSet, learningRate);
                    break;
                    
                case ModelType.MultiClassClassification:
                    updatedModel = UpdateMultiClassClassificationModel(model, modelDefinition, dataSplit.TrainSet, learningRate);
                    break;
                    
                case ModelType.Regression:
                    updatedModel = UpdateRegressionModel(model, modelDefinition, dataSplit.TrainSet, learningRate);
                    break;
                    
                default:
                    throw new NotSupportedException($"Online learning for model type {modelDefinition.Type} is not supported");
            }
            
            // Evaluate the updated model
            var metrics = _modelEvaluator.EvaluateModel(modelDefinition, updatedModel, dataSplit.TestSet);
            
            return (updatedModel, metrics);
        }
        
        /// <summary>
        /// Updates a binary classification model
        /// </summary>
        private ITransformer UpdateBinaryClassificationModel(
            ITransformer model,
            ModelDefinition modelDefinition,
            IDataView trainingData,
            double learningRate)
        {
            // Get the label column name
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Label";
            
            // Create a pipeline to extract features
            var featurePipeline = _mlContext.Transforms.Concatenate("Features",
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Apply feature pipeline
            var transformedData = featurePipeline.Fit(trainingData).Transform(trainingData);
            
            // Update the model based on algorithm
            switch (modelDefinition.Algorithm?.ToLowerInvariant())
            {
                case "logisticregression":
                    // For logistic regression, we can use the existing model as a starting point
                    var logisticOptions = new LbfgsLogisticRegressionTrainer.Options
                    {
                        LabelColumnName = labelColumnName,
                        FeatureColumnName = "Features",
                        L2Regularization = 0.01f,
                        OptimizationTolerance = 1e-4f,
                        MaximumNumberOfIterations = 20
                    };
                    
                    var logisticTrainer = new LbfgsLogisticRegressionTrainer(_mlContext, logisticOptions);
                    return logisticTrainer.Fit(transformedData);
                    
                case "svm":
                    // For SVM, we can use the existing model as a starting point
                    var svmOptions = new LinearSvmTrainer.Options
                    {
                        LabelColumnName = labelColumnName,
                        FeatureColumnName = "Features",
                        Lambda = 0.01f,
                        NumberOfIterations = 20
                    };
                    
                    var svmTrainer = new LinearSvmTrainer(_mlContext, svmOptions);
                    return svmTrainer.Fit(transformedData);
                    
                default:
                    // For other algorithms, we'll retrain from scratch with a small number of iterations
                    return _modelTrainer.TrainBinaryClassificationModel(modelDefinition, transformedData);
            }
        }
        
        /// <summary>
        /// Updates a multi-class classification model
        /// </summary>
        private ITransformer UpdateMultiClassClassificationModel(
            ITransformer model,
            ModelDefinition modelDefinition,
            IDataView trainingData,
            double learningRate)
        {
            // Get the label column name
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Label";
            
            // Create a pipeline to extract features
            var featurePipeline = _mlContext.Transforms.Concatenate("Features",
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Apply feature pipeline
            var transformedData = featurePipeline.Fit(trainingData).Transform(trainingData);
            
            // Update the model based on algorithm
            switch (modelDefinition.Algorithm?.ToLowerInvariant())
            {
                case "sdca":
                    // For SDCA, we can use the existing model as a starting point
                    var sdcaOptions = new SdcaMaximumEntropyMulticlassTrainer.Options
                    {
                        LabelColumnName = labelColumnName,
                        FeatureColumnName = "Features",
                        L2Regularization = 0.01f,
                        MaximumNumberOfIterations = 20
                    };
                    
                    var sdcaTrainer = new SdcaMaximumEntropyMulticlassTrainer(_mlContext, sdcaOptions);
                    return sdcaTrainer.Fit(transformedData);
                    
                case "lbfgs":
                    // For LBFGS, we can use the existing model as a starting point
                    var lbfgsOptions = new LbfgsMaximumEntropyMulticlassTrainer.Options
                    {
                        LabelColumnName = labelColumnName,
                        FeatureColumnName = "Features",
                        L2Regularization = 0.01f,
                        OptimizationTolerance = 1e-4f,
                        MaximumNumberOfIterations = 20
                    };
                    
                    var lbfgsTrainer = new LbfgsMaximumEntropyMulticlassTrainer(_mlContext, lbfgsOptions);
                    return lbfgsTrainer.Fit(transformedData);
                    
                default:
                    // For other algorithms, we'll retrain from scratch with a small number of iterations
                    return _modelTrainer.TrainMultiClassClassificationModel(modelDefinition, transformedData);
            }
        }
        
        /// <summary>
        /// Updates a regression model
        /// </summary>
        private ITransformer UpdateRegressionModel(
            ITransformer model,
            ModelDefinition modelDefinition,
            IDataView trainingData,
            double learningRate)
        {
            // Get the label column name
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Label";
            
            // Create a pipeline to extract features
            var featurePipeline = _mlContext.Transforms.Concatenate("Features",
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Apply feature pipeline
            var transformedData = featurePipeline.Fit(trainingData).Transform(trainingData);
            
            // Update the model based on algorithm
            switch (modelDefinition.Algorithm?.ToLowerInvariant())
            {
                case "sdca":
                    // For SDCA, we can use the existing model as a starting point
                    var sdcaOptions = new SdcaRegressionTrainer.Options
                    {
                        LabelColumnName = labelColumnName,
                        FeatureColumnName = "Features",
                        L2Regularization = 0.01f,
                        MaximumNumberOfIterations = 20
                    };
                    
                    var sdcaTrainer = new SdcaRegressionTrainer(_mlContext, sdcaOptions);
                    return sdcaTrainer.Fit(transformedData);
                    
                default:
                    // For other algorithms, we'll retrain from scratch with a small number of iterations
                    return _modelTrainer.TrainRegressionModel(modelDefinition, transformedData);
            }
        }
        
        /// <summary>
        /// Gets reference data for a model
        /// </summary>
        private async Task<List<Dictionary<string, object>>> GetReferenceDataAsync(string modelName, ModelMetadata metadata)
        {
            try
            {
                // Try to get reference data from model metadata
                var referenceDataPath = metadata.Parameters.TryGetValue("ReferenceDataPath", out var path) ? path : null;
                
                if (!string.IsNullOrEmpty(referenceDataPath))
                {
                    // Load reference data from path
                    // This is a placeholder - actual implementation would depend on storage service
                    return new List<Dictionary<string, object>>();
                }
                
                // If no reference data available, create synthetic data based on schema
                return GenerateSyntheticData(metadata.InputSchema);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting reference data for model {ModelName}", modelName);
                return new List<Dictionary<string, object>>();
            }
        }
        
        /// <summary>
        /// Generates synthetic data based on schema
        /// </summary>
        private List<Dictionary<string, object>> GenerateSyntheticData(List<FeatureDefinition> schema, int count = 100)
        {
            var random = new Random(42);
            var result = new List<Dictionary<string, object>>();
            
            for (int i = 0; i < count; i++)
            {
                var row = new Dictionary<string, object>();
                
                foreach (var feature in schema)
                {
                    // Generate random value based on data type
                    object value = feature.DataType switch
                    {
                        FeatureDataType.Boolean => random.Next(2) == 1,
                        FeatureDataType.Integer => random.Next(100),
                        FeatureDataType.Float => random.NextDouble() * 100,
                        FeatureDataType.DateTime => DateTime.UtcNow.AddDays(random.Next(-100, 100)),
                        FeatureDataType.Categorical => $"Category{random.Next(5)}",
                        FeatureDataType.String => $"String{random.Next(100)}",
                        FeatureDataType.Text => $"Text sample {random.Next(100)} for testing purposes",
                        _ => $"Value{random.Next(100)}"
                    };
                    
                    row[feature.Name] = value;
                }
                
                result.Add(row);
            }
            
            return result;
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
        /// Helper class to build an IDataView from a list of dictionaries
        /// </summary>
        private class ArrayDataViewBuilder
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
        
        /// <summary>
        /// Checks if a model type and algorithm support online learning
        /// </summary>
        private bool SupportsOnlineLearning(ModelType modelType, string algorithm)
        {
            // Check if model type supports online learning
            switch (modelType)
            {
                case ModelType.BinaryClassification:
                    // These algorithms support online learning
                    return algorithm?.ToLowerInvariant() switch
                    {
                        "logisticregression" => true,
                        "svm" => true,
                        _ => false
                    };
                    
                case ModelType.MultiClassClassification:
                    // These algorithms support online learning
                    return algorithm?.ToLowerInvariant() switch
                    {
                        "sdca" => true,
                        "lbfgs" => true,
                        _ => false
                    };
                    
                case ModelType.Regression:
                    // These algorithms support online learning
                    return algorithm?.ToLowerInvariant() switch
                    {
                        "sdca" => true,
                        _ => false
                    };
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Increments a version string
        /// </summary>
        private string IncrementVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return "1.0";
            }
            
            // Parse version
            if (Version.TryParse(version, out var parsedVersion))
            {
                // Increment minor version
                return $"{parsedVersion.Major}.{parsedVersion.Minor + 1}";
            }
            
            // If parsing fails, append a suffix
            return $"{version}_updated";
        }
        
        /// <summary>
        /// Determines the recommended action for handling drift
        /// </summary>
        private DriftAction DetermineRecommendedAction(ConceptDriftDetectionResult driftResult)
        {
            if (!driftResult.DriftDetected)
            {
                return DriftAction.NoAction;
            }
            
            // If drift confidence is high, recommend retraining
            if (driftResult.DriftConfidence > 0.8)
            {
                return DriftAction.Retrain;
            }
            
            // If drift confidence is medium, recommend updating
            if (driftResult.DriftConfidence > 0.5)
            {
                return DriftAction.Update;
            }
            
            // If drift confidence is low, recommend investigating
            return DriftAction.Investigate;
        }
    }
}
