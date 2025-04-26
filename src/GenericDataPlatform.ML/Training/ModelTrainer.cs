using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Trainer for machine learning models
    /// </summary>
    public class ModelTrainer : IModelTrainer
    {
        private readonly ILogger<ModelTrainer> _logger;
        private readonly MLContext _mlContext;
        
        public ModelTrainer(ILogger<ModelTrainer> logger)
        {
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
        }
        
        /// <summary>
        /// Trains a model using the specified training data and model definition
        /// </summary>
        public async Task<TrainedModel> TrainModelAsync(ModelDefinition modelDefinition, IEnumerable<Dictionary<string, object>> trainingData)
        {
            try
            {
                _logger.LogInformation("Training model: {ModelName}", modelDefinition.Name);
                
                // Convert training data to IDataView
                var dataView = ConvertToDataView(trainingData, modelDefinition);
                
                // Split data into training and validation sets
                var dataSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
                
                // Create and train the model
                ITransformer trainedModel;
                
                switch (modelDefinition.Type)
                {
                    case ModelType.BinaryClassification:
                        trainedModel = TrainBinaryClassificationModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    case ModelType.MultiClassClassification:
                        trainedModel = TrainMultiClassClassificationModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    case ModelType.Regression:
                        trainedModel = TrainRegressionModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    case ModelType.Clustering:
                        trainedModel = TrainClusteringModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    case ModelType.AnomalyDetection:
                        trainedModel = TrainAnomalyDetectionModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    case ModelType.Recommendation:
                        trainedModel = TrainRecommendationModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    case ModelType.TimeSeries:
                        trainedModel = TrainTimeSeriesModel(modelDefinition, dataSplit.TrainSet);
                        break;
                    
                    default:
                        throw new NotSupportedException($"Model type {modelDefinition.Type} is not supported");
                }
                
                // Evaluate the model
                var metrics = EvaluateModel(modelDefinition, trainedModel, dataSplit.TestSet);
                
                // Save the model
                var modelPath = SaveModel(modelDefinition, trainedModel);
                
                // Create the trained model result
                var result = new TrainedModel
                {
                    ModelDefinition = modelDefinition,
                    ModelPath = modelPath,
                    Metrics = metrics,
                    TrainedAt = DateTime.UtcNow
                };
                
                _logger.LogInformation("Model training completed: {ModelName}", modelDefinition.Name);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model: {ModelName}", modelDefinition.Name);
                throw;
            }
        }
        
        /// <summary>
        /// Converts training data to IDataView
        /// </summary>
        private IDataView ConvertToDataView(IEnumerable<Dictionary<string, object>> trainingData, ModelDefinition modelDefinition)
        {
            // Create a list to hold the data
            var data = new List<object>();
            
            // Create a dynamic class to hold the data
            var dataType = CreateDynamicClass(modelDefinition);
            
            // Convert each training data item to the dynamic class
            foreach (var item in trainingData)
            {
                var instance = Activator.CreateInstance(dataType);
                
                // Set feature values
                foreach (var feature in modelDefinition.Features)
                {
                    if (item.TryGetValue(feature.Name, out var value))
                    {
                        var property = dataType.GetProperty(feature.Name);
                        if (property != null)
                        {
                            property.SetValue(instance, ConvertValue(value, feature.DataType));
                        }
                    }
                }
                
                // Set label value
                if (modelDefinition.Labels.Count > 0)
                {
                    var label = modelDefinition.Labels[0];
                    if (item.TryGetValue(label.Name, out var value))
                    {
                        var property = dataType.GetProperty(label.Name);
                        if (property != null)
                        {
                            property.SetValue(instance, ConvertValue(value, ConvertLabelDataType(label.DataType)));
                        }
                    }
                }
                
                data.Add(instance);
            }
            
            // Convert to IDataView
            return _mlContext.Data.LoadFromEnumerable(data);
        }
        
        /// <summary>
        /// Creates a dynamic class for the model
        /// </summary>
        private Type CreateDynamicClass(ModelDefinition modelDefinition)
        {
            // In a real implementation, we would use reflection or a library like Dynamitey
            // to create a dynamic class with the required properties
            // For simplicity, we'll return a generic type that can handle any schema
            
            return typeof(Dictionary<string, object>);
        }
        
        /// <summary>
        /// Converts a value to the specified data type
        /// </summary>
        private object ConvertValue(object value, FeatureDataType dataType)
        {
            if (value == null)
            {
                return null;
            }
            
            try
            {
                return dataType switch
                {
                    FeatureDataType.String => value.ToString(),
                    FeatureDataType.Integer => Convert.ToInt32(value),
                    FeatureDataType.Float => Convert.ToSingle(value),
                    FeatureDataType.Boolean => Convert.ToBoolean(value),
                    FeatureDataType.DateTime => Convert.ToDateTime(value),
                    FeatureDataType.Categorical => value.ToString(),
                    FeatureDataType.Text => value.ToString(),
                    FeatureDataType.Image => value, // Assuming value is already a byte array
                    _ => value
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting value {Value} to {DataType}", value, dataType);
                return null;
            }
        }
        
        /// <summary>
        /// Converts a label data type to a feature data type
        /// </summary>
        private FeatureDataType ConvertLabelDataType(LabelDataType labelDataType)
        {
            return labelDataType switch
            {
                LabelDataType.Binary => FeatureDataType.Boolean,
                LabelDataType.Categorical => FeatureDataType.Categorical,
                LabelDataType.Continuous => FeatureDataType.Float,
                _ => FeatureDataType.String
            };
        }
        
        /// <summary>
        /// Trains a binary classification model
        /// </summary>
        private ITransformer TrainBinaryClassificationModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Get the label column name
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Label";
            
            // Create a pipeline
            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Add the trainer
            var trainer = modelDefinition.Algorithm?.ToLowerInvariant() switch
            {
                "logisticregression" => _mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: labelColumnName),
                "svm" => _mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: labelColumnName),
                "fastforest" => _mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: labelColumnName),
                "fasttree" => _mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: labelColumnName),
                _ => _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: labelColumnName)
            };
            
            pipeline = pipeline.Append(trainer);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Trains a multi-class classification model
        /// </summary>
        private ITransformer TrainMultiClassClassificationModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Get the label column name
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Label";
            
            // Create a pipeline
            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Add the trainer
            var trainer = modelDefinition.Algorithm?.ToLowerInvariant() switch
            {
                "naivebayes" => _mlContext.MulticlassClassification.Trainers.NaiveBayes(labelColumnName: labelColumnName),
                "sdca" => _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: labelColumnName),
                "lbfgs" => _mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: labelColumnName),
                _ => _mlContext.MulticlassClassification.Trainers.OneVersusAll(
                    _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(), labelColumnName: labelColumnName)
            };
            
            pipeline = pipeline.Append(trainer);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Trains a regression model
        /// </summary>
        private ITransformer TrainRegressionModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Get the label column name
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Label";
            
            // Create a pipeline
            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Add the trainer
            var trainer = modelDefinition.Algorithm?.ToLowerInvariant() switch
            {
                "fastforest" => _mlContext.Regression.Trainers.FastForest(labelColumnName: labelColumnName),
                "fasttree" => _mlContext.Regression.Trainers.FastTree(labelColumnName: labelColumnName),
                "poisson" => _mlContext.Regression.Trainers.LbfgsPoissonRegression(labelColumnName: labelColumnName),
                "sdca" => _mlContext.Regression.Trainers.Sdca(labelColumnName: labelColumnName),
                _ => _mlContext.Regression.Trainers.LbfgsPoissonRegression(labelColumnName: labelColumnName)
            };
            
            pipeline = pipeline.Append(trainer);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Trains a clustering model
        /// </summary>
        private ITransformer TrainClusteringModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Create a pipeline
            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Add the trainer
            var trainer = modelDefinition.Algorithm?.ToLowerInvariant() switch
            {
                "kmeans" => _mlContext.Clustering.Trainers.KMeans(),
                _ => _mlContext.Clustering.Trainers.KMeans()
            };
            
            pipeline = pipeline.Append(trainer);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Trains an anomaly detection model
        /// </summary>
        private ITransformer TrainAnomalyDetectionModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Create a pipeline
            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                modelDefinition.Features.Select(f => f.Name).ToArray());
            
            // Add the trainer
            var trainer = modelDefinition.Algorithm?.ToLowerInvariant() switch
            {
                "randomizedpca" => _mlContext.AnomalyDetection.Trainers.RandomizedPca(),
                _ => _mlContext.AnomalyDetection.Trainers.RandomizedPca()
            };
            
            pipeline = pipeline.Append(trainer);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Trains a recommendation model
        /// </summary>
        private ITransformer TrainRecommendationModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Get the user and item column names
            var userColumnName = modelDefinition.Features.FirstOrDefault(f => f.Name.Contains("user", StringComparison.OrdinalIgnoreCase))?.Name ?? "UserId";
            var itemColumnName = modelDefinition.Features.FirstOrDefault(f => f.Name.Contains("item", StringComparison.OrdinalIgnoreCase))?.Name ?? "ItemId";
            var labelColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Rating";
            
            // Create a pipeline
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: userColumnName, outputColumnName: "UserIdEncoded")
                .Append(_mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: itemColumnName, outputColumnName: "ItemIdEncoded"));
            
            // Add the trainer
            var trainer = modelDefinition.Algorithm?.ToLowerInvariant() switch
            {
                "matrixfactorization" => _mlContext.Recommendation().Trainers.MatrixFactorization(
                    labelColumnName: labelColumnName,
                    matrixColumnIndexColumnName: "UserIdEncoded",
                    matrixRowIndexColumnName: "ItemIdEncoded"),
                _ => _mlContext.Recommendation().Trainers.MatrixFactorization(
                    labelColumnName: labelColumnName,
                    matrixColumnIndexColumnName: "UserIdEncoded",
                    matrixRowIndexColumnName: "ItemIdEncoded")
            };
            
            pipeline = pipeline.Append(trainer);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Trains a time series model
        /// </summary>
        private ITransformer TrainTimeSeriesModel(ModelDefinition modelDefinition, IDataView trainingData)
        {
            // Get the time column name
            var timeColumnName = modelDefinition.Features.FirstOrDefault(f => f.DataType == FeatureDataType.DateTime)?.Name ?? "Time";
            var valueColumnName = modelDefinition.Labels.FirstOrDefault()?.Name ?? "Value";
            
            // Create a pipeline
            var pipeline = _mlContext.Transforms.DetectSpike(
                outputColumnName: "Prediction",
                inputColumnName: valueColumnName,
                confidence: 95,
                pvalueHistoryLength: 30,
                trainingWindowSize: 90,
                seasonalityWindowSize: 30);
            
            // Train the model
            return pipeline.Fit(trainingData);
        }
        
        /// <summary>
        /// Evaluates a trained model
        /// </summary>
        private Dictionary<string, double> EvaluateModel(ModelDefinition modelDefinition, ITransformer trainedModel, IDataView testData)
        {
            var metrics = new Dictionary<string, double>();
            
            try
            {
                // Make predictions
                var predictions = trainedModel.Transform(testData);
                
                // Evaluate the model
                switch (modelDefinition.Type)
                {
                    case ModelType.BinaryClassification:
                        var binaryMetrics = _mlContext.BinaryClassification.Evaluate(predictions);
                        metrics["Accuracy"] = binaryMetrics.Accuracy;
                        metrics["AreaUnderRocCurve"] = binaryMetrics.AreaUnderRocCurve;
                        metrics["F1Score"] = binaryMetrics.F1Score;
                        break;
                    
                    case ModelType.MultiClassClassification:
                        var multiClassMetrics = _mlContext.MulticlassClassification.Evaluate(predictions);
                        metrics["MicroAccuracy"] = multiClassMetrics.MicroAccuracy;
                        metrics["MacroAccuracy"] = multiClassMetrics.MacroAccuracy;
                        metrics["LogLoss"] = multiClassMetrics.LogLoss;
                        break;
                    
                    case ModelType.Regression:
                        var regressionMetrics = _mlContext.Regression.Evaluate(predictions);
                        metrics["RSquared"] = regressionMetrics.RSquared;
                        metrics["MeanAbsoluteError"] = regressionMetrics.MeanAbsoluteError;
                        metrics["MeanSquaredError"] = regressionMetrics.MeanSquaredError;
                        metrics["RootMeanSquaredError"] = regressionMetrics.RootMeanSquaredError;
                        break;
                    
                    case ModelType.Clustering:
                        var clusteringMetrics = _mlContext.Clustering.Evaluate(predictions);
                        metrics["AverageDistance"] = clusteringMetrics.AverageDistance;
                        metrics["DaviesBouldinIndex"] = clusteringMetrics.DaviesBouldinIndex;
                        break;
                    
                    case ModelType.AnomalyDetection:
                        var anomalyMetrics = _mlContext.AnomalyDetection.Evaluate(predictions);
                        metrics["AreaUnderRocCurve"] = anomalyMetrics.AreaUnderRocCurve;
                        metrics["DetectionRateAtFalsePositiveCount"] = anomalyMetrics.DetectionRateAtFalsePositiveCount;
                        break;
                    
                    case ModelType.Recommendation:
                        var recommendationMetrics = _mlContext.Regression.Evaluate(predictions);
                        metrics["RSquared"] = recommendationMetrics.RSquared;
                        metrics["MeanAbsoluteError"] = recommendationMetrics.MeanAbsoluteError;
                        metrics["RootMeanSquaredError"] = recommendationMetrics.RootMeanSquaredError;
                        break;
                    
                    case ModelType.TimeSeries:
                        // No standard evaluation metrics for time series
                        metrics["CustomMetric"] = 0.0;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error evaluating model: {ModelName}", modelDefinition.Name);
            }
            
            return metrics;
        }
        
        /// <summary>
        /// Saves a trained model
        /// </summary>
        private string SaveModel(ModelDefinition modelDefinition, ITransformer trainedModel)
        {
            // Create the models directory if it doesn't exist
            var modelsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
            Directory.CreateDirectory(modelsDirectory);
            
            // Generate a file path
            var modelFileName = $"{modelDefinition.Name}_{modelDefinition.Version}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            var modelPath = Path.Combine(modelsDirectory, modelFileName);
            
            // Save the model
            _mlContext.Model.Save(trainedModel, null, modelPath);
            
            return modelPath;
        }
    }
    
    /// <summary>
    /// Interface for model trainer
    /// </summary>
    public interface IModelTrainer
    {
        Task<TrainedModel> TrainModelAsync(ModelDefinition modelDefinition, IEnumerable<Dictionary<string, object>> trainingData);
    }
    
    /// <summary>
    /// Represents a trained machine learning model
    /// </summary>
    public class TrainedModel
    {
        /// <summary>
        /// Definition of the model
        /// </summary>
        public ModelDefinition ModelDefinition { get; set; }
        
        /// <summary>
        /// Path to the saved model
        /// </summary>
        public string ModelPath { get; set; }
        
        /// <summary>
        /// Evaluation metrics for the model
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Date and time when the model was trained
        /// </summary>
        public DateTime TrainedAt { get; set; } = DateTime.UtcNow;
    }
}
