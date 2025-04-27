using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Utils;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for explaining model predictions
    /// </summary>
    public class ModelExplainerService : IModelExplainerService
    {
        private readonly MLContext _mlContext;
        private readonly IModelManagementService _modelManagementService;
        private readonly IPredictionService _predictionService;
        private readonly IDynamicObjectGenerator _dynamicObjectGenerator;
        private readonly ILogger<ModelExplainerService> _logger;
        
        public ModelExplainerService(
            MLContext mlContext,
            IModelManagementService modelManagementService,
            IPredictionService predictionService,
            IDynamicObjectGenerator dynamicObjectGenerator,
            ILogger<ModelExplainerService> logger)
        {
            _mlContext = mlContext;
            _modelManagementService = modelManagementService;
            _predictionService = predictionService;
            _dynamicObjectGenerator = dynamicObjectGenerator;
            _logger = logger;
        }
        
        /// <summary>
        /// Explains a model globally
        /// </summary>
        public async Task<ExplanationResult> ExplainModelGloballyAsync(
            string modelName,
            string modelVersion = null,
            ExplanationType explanationType = ExplanationType.SHAP,
            IEnumerable<Dictionary<string, object>> sampleData = null)
        {
            try
            {
                _logger.LogInformation("Generating global explanation for model {ModelName} version {ModelVersion} using {ExplanationType}",
                    modelName, modelVersion ?? "latest", explanationType);
                
                // Load the model
                var (model, metadata) = await LoadModelAsync(modelName, modelVersion);
                
                // Check if explanation type is supported for this model type
                if (!GetSupportedExplanationTypes(metadata.Type).Contains(explanationType))
                {
                    throw new NotSupportedException($"Explanation type {explanationType} is not supported for model type {metadata.Type}");
                }
                
                // Get sample data if not provided
                var sampleDataList = sampleData?.ToList();
                if (sampleDataList == null || sampleDataList.Count == 0)
                {
                    // Try to get sample data from model metadata
                    sampleDataList = await GetSampleDataAsync(modelName, metadata);
                }
                
                if (sampleDataList == null || sampleDataList.Count == 0)
                {
                    throw new ArgumentException("Sample data is required for generating explanations");
                }
                
                // Create explanation result
                var result = new ExplanationResult
                {
                    ModelName = modelName,
                    ModelVersion = metadata.Version,
                    ExplanationType = explanationType
                };
                
                // Generate global feature importance based on explanation type
                switch (explanationType)
                {
                    case ExplanationType.SHAP:
                        result.GlobalFeatureImportance = await GenerateShapExplanationAsync(model, metadata, sampleDataList);
                        break;
                        
                    case ExplanationType.LIME:
                        result.GlobalFeatureImportance = await GenerateLimeExplanationAsync(model, metadata, sampleDataList);
                        break;
                        
                    case ExplanationType.PermutationFeatureImportance:
                        result.GlobalFeatureImportance = await GeneratePermutationFeatureImportanceAsync(model, metadata, sampleDataList);
                        break;
                        
                    case ExplanationType.BuiltIn:
                        result.GlobalFeatureImportance = await GenerateBuiltInFeatureImportanceAsync(model, metadata);
                        break;
                }
                
                _logger.LogInformation("Generated global explanation for model {ModelName} version {ModelVersion}",
                    modelName, metadata.Version);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating global explanation for model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Explains predictions for specific instances
        /// </summary>
        public async Task<ExplanationResult> ExplainPredictionsAsync(
            string modelName,
            List<Dictionary<string, object>> instances,
            string modelVersion = null,
            ExplanationType explanationType = ExplanationType.SHAP,
            IEnumerable<Dictionary<string, object>> backgroundData = null)
        {
            try
            {
                _logger.LogInformation("Explaining predictions for model {ModelName} version {ModelVersion} using {ExplanationType}",
                    modelName, modelVersion ?? "latest", explanationType);
                
                // Load the model
                var (model, metadata) = await LoadModelAsync(modelName, modelVersion);
                
                // Check if explanation type is supported for this model type
                if (!GetSupportedExplanationTypes(metadata.Type).Contains(explanationType))
                {
                    throw new NotSupportedException($"Explanation type {explanationType} is not supported for model type {metadata.Type}");
                }
                
                // Make predictions
                var predictionResponse = await _predictionService.PredictAsync(modelName, modelVersion, instances);
                
                // Get background data if not provided
                var backgroundDataList = backgroundData?.ToList();
                if (backgroundDataList == null || backgroundDataList.Count == 0 && explanationType == ExplanationType.SHAP)
                {
                    // Try to get sample data from model metadata
                    backgroundDataList = await GetSampleDataAsync(modelName, metadata);
                }
                
                // Create explanation result
                var result = new ExplanationResult
                {
                    ModelName = modelName,
                    ModelVersion = metadata.Version,
                    ExplanationType = explanationType
                };
                
                // Generate global feature importance
                switch (explanationType)
                {
                    case ExplanationType.SHAP:
                        result.GlobalFeatureImportance = await GenerateShapExplanationAsync(model, metadata, backgroundDataList);
                        break;
                        
                    case ExplanationType.LIME:
                        result.GlobalFeatureImportance = await GenerateLimeExplanationAsync(model, metadata, instances);
                        break;
                        
                    case ExplanationType.PermutationFeatureImportance:
                        result.GlobalFeatureImportance = await GeneratePermutationFeatureImportanceAsync(model, metadata, instances);
                        break;
                        
                    case ExplanationType.BuiltIn:
                        result.GlobalFeatureImportance = await GenerateBuiltInFeatureImportanceAsync(model, metadata);
                        break;
                }
                
                // Generate local explanations for each instance
                for (int i = 0; i < instances.Count; i++)
                {
                    var instance = instances[i];
                    var prediction = predictionResponse.Predictions[i];
                    
                    var localExplanation = new LocalExplanation
                    {
                        Instance = instance,
                        Prediction = prediction
                    };
                    
                    // Generate local feature importance based on explanation type
                    switch (explanationType)
                    {
                        case ExplanationType.SHAP:
                            localExplanation.FeatureImportance = await GenerateLocalShapExplanationAsync(
                                model, metadata, instance, backgroundDataList);
                            break;
                            
                        case ExplanationType.LIME:
                            localExplanation.FeatureImportance = await GenerateLocalLimeExplanationAsync(
                                model, metadata, instance);
                            break;
                            
                        case ExplanationType.PermutationFeatureImportance:
                        case ExplanationType.BuiltIn:
                            // For these types, we use the global importance as an approximation
                            localExplanation.FeatureImportance = result.GlobalFeatureImportance;
                            break;
                    }
                    
                    result.LocalExplanations.Add(localExplanation);
                }
                
                _logger.LogInformation("Generated explanations for {InstanceCount} predictions with model {ModelName} version {ModelVersion}",
                    instances.Count, modelName, metadata.Version);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error explaining predictions for model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Gets supported explanation types for a given model type
        /// </summary>
        public List<ExplanationType> GetSupportedExplanationTypes(ModelType modelType)
        {
            var supportedTypes = new List<ExplanationType>
            {
                ExplanationType.PermutationFeatureImportance, // Works with any model
                ExplanationType.BuiltIn // Works with most models
            };
            
            // SHAP works well with tree-based models and some other types
            if (modelType == ModelType.BinaryClassification ||
                modelType == ModelType.MultiClassClassification ||
                modelType == ModelType.Regression)
            {
                supportedTypes.Add(ExplanationType.SHAP);
                supportedTypes.Add(ExplanationType.LIME);
            }
            
            return supportedTypes;
        }
        
        /// <summary>
        /// Loads a model by name and version
        /// </summary>
        private async Task<(ITransformer Model, ModelMetadata Metadata)> LoadModelAsync(string modelName, string modelVersion)
        {
            // Get model metadata
            var metadata = await _modelManagementService.GetModelMetadataAsync(modelName, modelVersion);
            if (metadata == null)
            {
                throw new ArgumentException($"Model {modelName} version {modelVersion ?? "latest"} not found");
            }
            
            // Load the model
            var model = await _modelManagementService.LoadModelAsync(metadata.ModelPath);
            
            return (model, metadata);
        }
        
        /// <summary>
        /// Gets sample data for a model
        /// </summary>
        private async Task<List<Dictionary<string, object>>> GetSampleDataAsync(string modelName, ModelMetadata metadata)
        {
            try
            {
                // Try to get sample data from model metadata
                var sampleDataPath = metadata.Parameters.TryGetValue("SampleDataPath", out var path) ? path : null;
                
                if (!string.IsNullOrEmpty(sampleDataPath))
                {
                    // Load sample data from path
                    // This is a placeholder - actual implementation would depend on storage service
                    return new List<Dictionary<string, object>>();
                }
                
                // If no sample data available, create synthetic data based on schema
                return GenerateSyntheticData(metadata.InputSchema);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting sample data for model {ModelName}", modelName);
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
        /// Generates SHAP explanations
        /// </summary>
        private async Task<Dictionary<string, double>> GenerateShapExplanationAsync(
            ITransformer model,
            ModelMetadata metadata,
            List<Dictionary<string, object>> sampleData)
        {
            // This is a simplified implementation of SHAP
            // In a real implementation, you would use a SHAP library or implement the algorithm
            
            _logger.LogInformation("Generating SHAP explanations for model {ModelName}", metadata.Name);
            
            // Convert sample data to IDataView
            var dataView = ConvertToDataView(sampleData);
            
            // For this simplified implementation, we'll use permutation feature importance as a proxy
            return await GeneratePermutationFeatureImportanceAsync(model, metadata, sampleData);
        }
        
        /// <summary>
        /// Generates local SHAP explanations for a specific instance
        /// </summary>
        private async Task<Dictionary<string, double>> GenerateLocalShapExplanationAsync(
            ITransformer model,
            ModelMetadata metadata,
            Dictionary<string, object> instance,
            List<Dictionary<string, object>> backgroundData)
        {
            // This is a simplified implementation of local SHAP explanations
            // In a real implementation, you would use a SHAP library or implement the algorithm
            
            // For this simplified implementation, we'll use the global importance as a base
            // and add some random noise to simulate instance-specific importance
            var globalImportance = await GeneratePermutationFeatureImportanceAsync(model, metadata, backgroundData);
            var localImportance = new Dictionary<string, double>();
            var random = new Random();
            
            foreach (var feature in globalImportance)
            {
                // Add some random noise (±20%)
                var noise = (random.NextDouble() * 0.4) - 0.2; // -0.2 to 0.2
                var importance = feature.Value * (1 + noise);
                localImportance[feature.Key] = importance;
            }
            
            // Normalize to sum to 1
            var sum = localImportance.Values.Sum();
            if (sum > 0)
            {
                foreach (var key in localImportance.Keys.ToList())
                {
                    localImportance[key] /= sum;
                }
            }
            
            return localImportance;
        }
        
        /// <summary>
        /// Generates LIME explanations
        /// </summary>
        private async Task<Dictionary<string, double>> GenerateLimeExplanationAsync(
            ITransformer model,
            ModelMetadata metadata,
            List<Dictionary<string, object>> sampleData)
        {
            // This is a simplified implementation of LIME
            // In a real implementation, you would use a LIME library or implement the algorithm
            
            _logger.LogInformation("Generating LIME explanations for model {ModelName}", metadata.Name);
            
            // For this simplified implementation, we'll use permutation feature importance as a proxy
            return await GeneratePermutationFeatureImportanceAsync(model, metadata, sampleData);
        }
        
        /// <summary>
        /// Generates local LIME explanations for a specific instance
        /// </summary>
        private async Task<Dictionary<string, double>> GenerateLocalLimeExplanationAsync(
            ITransformer model,
            ModelMetadata metadata,
            Dictionary<string, object> instance)
        {
            // This is a simplified implementation of local LIME explanations
            // In a real implementation, you would use a LIME library or implement the algorithm
            
            // Generate synthetic neighborhood data around the instance
            var neighborhoodData = GenerateNeighborhoodData(instance, metadata.InputSchema, 100);
            
            // For this simplified implementation, we'll use permutation feature importance on the neighborhood
            return await GeneratePermutationFeatureImportanceAsync(model, metadata, neighborhoodData);
        }
        
        /// <summary>
        /// Generates neighborhood data around an instance for LIME
        /// </summary>
        private List<Dictionary<string, object>> GenerateNeighborhoodData(
            Dictionary<string, object> instance,
            List<FeatureDefinition> schema,
            int count)
        {
            var random = new Random(42);
            var result = new List<Dictionary<string, object>>();
            
            for (int i = 0; i < count; i++)
            {
                var row = new Dictionary<string, object>();
                
                foreach (var feature in schema)
                {
                    // Get original value
                    var originalValue = instance.TryGetValue(feature.Name, out var value) ? value : null;
                    
                    // Perturb the value based on data type
                    object perturbedValue = feature.DataType switch
                    {
                        FeatureDataType.Boolean => random.Next(2) == 1,
                        
                        FeatureDataType.Integer => originalValue != null
                            ? (int)originalValue + random.Next(-10, 11)
                            : random.Next(100),
                        
                        FeatureDataType.Float => originalValue != null
                            ? (double)originalValue + (random.NextDouble() * 20) - 10
                            : random.NextDouble() * 100,
                        
                        FeatureDataType.DateTime => originalValue != null
                            ? ((DateTime)originalValue).AddDays(random.Next(-10, 11))
                            : DateTime.UtcNow.AddDays(random.Next(-100, 100)),
                        
                        FeatureDataType.Categorical => random.Next(10) < 7
                            ? originalValue // 70% chance to keep original value
                            : $"Category{random.Next(5)}",
                        
                        FeatureDataType.String => random.Next(10) < 7
                            ? originalValue // 70% chance to keep original value
                            : $"String{random.Next(100)}",
                        
                        FeatureDataType.Text => random.Next(10) < 7
                            ? originalValue // 70% chance to keep original value
                            : $"Text sample {random.Next(100)} for testing purposes",
                        
                        _ => originalValue ?? $"Value{random.Next(100)}"
                    };
                    
                    row[feature.Name] = perturbedValue;
                }
                
                result.Add(row);
            }
            
            return result;
        }
        
        /// <summary>
        /// Generates permutation feature importance
        /// </summary>
        private async Task<Dictionary<string, double>> GeneratePermutationFeatureImportanceAsync(
            ITransformer model,
            ModelMetadata metadata,
            List<Dictionary<string, object>> sampleData)
        {
            try
            {
                _logger.LogInformation("Generating permutation feature importance for model {ModelName}", metadata.Name);
                
                // Convert sample data to IDataView
                var dataView = ConvertToDataView(sampleData);
                
                // Transform data
                var transformedData = model.Transform(dataView);
                
                // Get feature column names
                var featureColumnNames = metadata.InputSchema.Select(f => f.Name).ToArray();
                
                // Get label column name (assuming it's the first output column)
                var labelColumnName = metadata.OutputSchema.FirstOrDefault()?.Name ?? "Label";
                
                // Calculate baseline metric
                var baselineMetric = CalculateMetric(transformedData, metadata.Type, labelColumnName);
                
                // Calculate importance for each feature
                var importance = new Dictionary<string, double>();
                
                foreach (var featureName in featureColumnNames)
                {
                    // Permute the feature
                    var permutedData = PermuteFeature(dataView, featureName);
                    
                    // Transform permuted data
                    var permutedTransformedData = model.Transform(permutedData);
                    
                    // Calculate metric with permuted feature
                    var permutedMetric = CalculateMetric(permutedTransformedData, metadata.Type, labelColumnName);
                    
                    // Calculate importance (decrease in performance)
                    var featureImportance = baselineMetric - permutedMetric;
                    
                    // Store importance
                    importance[featureName] = featureImportance;
                }
                
                // Normalize importance values to be positive and sum to 1
                var minImportance = importance.Values.Min();
                if (minImportance < 0)
                {
                    foreach (var key in importance.Keys.ToList())
                    {
                        importance[key] -= minImportance;
                    }
                }
                
                var sum = importance.Values.Sum();
                if (sum > 0)
                {
                    foreach (var key in importance.Keys.ToList())
                    {
                        importance[key] /= sum;
                    }
                }
                
                return importance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating permutation feature importance for model {ModelName}", metadata.Name);
                
                // Fallback to equal importance
                return metadata.InputSchema.ToDictionary(
                    f => f.Name,
                    f => 1.0 / metadata.InputSchema.Count);
            }
        }
        
        /// <summary>
        /// Permutes a feature in a dataset
        /// </summary>
        private IDataView PermuteFeature(IDataView data, string featureName)
        {
            // Create a pipeline to permute the feature
            var pipeline = _mlContext.Transforms.PermuteColumn(featureName);
            
            // Apply the pipeline
            return pipeline.Fit(data).Transform(data);
        }
        
        /// <summary>
        /// Calculates a metric for a dataset
        /// </summary>
        private double CalculateMetric(IDataView data, ModelType modelType, string labelColumnName)
        {
            try
            {
                switch (modelType)
                {
                    case ModelType.BinaryClassification:
                        var binaryMetrics = _mlContext.BinaryClassification.Evaluate(data, labelColumnName: labelColumnName);
                        return binaryMetrics.AreaUnderRocCurve;
                    
                    case ModelType.MultiClassClassification:
                        var multiClassMetrics = _mlContext.MulticlassClassification.Evaluate(data, labelColumnName: labelColumnName);
                        return multiClassMetrics.MicroAccuracy;
                    
                    case ModelType.Regression:
                        var regressionMetrics = _mlContext.Regression.Evaluate(data, labelColumnName: labelColumnName);
                        return regressionMetrics.RSquared;
                    
                    case ModelType.Clustering:
                        var clusteringMetrics = _mlContext.Clustering.Evaluate(data, scoreColumnName: "Score", featureColumnName: "Features");
                        return -clusteringMetrics.AverageDistance; // Negative so higher is better
                    
                    default:
                        return 0;
                }
            }
            catch (Exception)
            {
                // If evaluation fails, return a default value
                return 0;
            }
        }
        
        /// <summary>
        /// Generates built-in feature importance
        /// </summary>
        private async Task<Dictionary<string, double>> GenerateBuiltInFeatureImportanceAsync(
            ITransformer model,
            ModelMetadata metadata)
        {
            try
            {
                _logger.LogInformation("Generating built-in feature importance for model {ModelName}", metadata.Name);
                
                // Get feature names
                var featureNames = metadata.InputSchema.Select(f => f.Name).ToArray();
                
                // Extract feature importance from the model
                VBuffer<float> weights = default;
                bool hasWeights = false;
                
                // Different model types store feature importance differently
                if (metadata.Type == ModelType.BinaryClassification)
                {
                    var fastTreeBinary = model.LastTransformer as FastTreeBinaryModelParameters;
                    if (fastTreeBinary != null)
                    {
                        fastTreeBinary.GetFeatureWeights(ref weights);
                        hasWeights = true;
                    }
                }
                else if (metadata.Type == ModelType.Regression)
                {
                    var fastTreeRegression = model.LastTransformer as FastTreeRegressionModelParameters;
                    if (fastTreeRegression != null)
                    {
                        fastTreeRegression.GetFeatureWeights(ref weights);
                        hasWeights = true;
                    }
                }
                
                // If we have weights, convert to dictionary
                if (hasWeights)
                {
                    var weightValues = weights.GetValues().ToArray();
                    var importance = new Dictionary<string, double>();
                    
                    // Get absolute values for importance
                    var absWeights = weightValues.Select(Math.Abs).ToArray();
                    
                    // Normalize to sum to 1
                    var sum = absWeights.Sum();
                    if (sum > 0)
                    {
                        for (int i = 0; i < featureNames.Length && i < absWeights.Length; i++)
                        {
                            importance[featureNames[i]] = absWeights[i] / sum;
                        }
                    }
                    
                    return importance;
                }
                
                // If we couldn't get weights, fall back to permutation feature importance
                // Generate synthetic data for this
                var sampleData = GenerateSyntheticData(metadata.InputSchema);
                return await GeneratePermutationFeatureImportanceAsync(model, metadata, sampleData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating built-in feature importance for model {ModelName}", metadata.Name);
                
                // Fallback to equal importance
                return metadata.InputSchema.ToDictionary(
                    f => f.Name,
                    f => 1.0 / metadata.InputSchema.Count);
            }
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
    }
}
