using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Evaluates machine learning models
    /// </summary>
    public class ModelEvaluator
    {
        private readonly ILogger<ModelEvaluator> _logger;
        private readonly MLContext _mlContext;
        
        public ModelEvaluator(ILogger<ModelEvaluator> logger)
        {
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
        }
        
        /// <summary>
        /// Evaluates a model using the specified test data
        /// </summary>
        public async Task<Dictionary<string, double>> EvaluateModelAsync(
            ModelDefinition modelDefinition, 
            ITransformer model, 
            IDataView testData)
        {
            try
            {
                _logger.LogInformation("Evaluating model: {ModelName}", modelDefinition.Name);
                
                // Make predictions on the test data
                var predictions = model.Transform(testData);
                
                // Evaluate the model based on its type
                var metrics = new Dictionary<string, double>();
                
                switch (modelDefinition.Type)
                {
                    case ModelType.BinaryClassification:
                        EvaluateBinaryClassificationModel(predictions, metrics);
                        break;
                    
                    case ModelType.MultiClassClassification:
                        EvaluateMultiClassClassificationModel(predictions, metrics);
                        break;
                    
                    case ModelType.Regression:
                        EvaluateRegressionModel(predictions, metrics);
                        break;
                    
                    case ModelType.Clustering:
                        EvaluateClusteringModel(predictions, metrics);
                        break;
                    
                    case ModelType.AnomalyDetection:
                        EvaluateAnomalyDetectionModel(predictions, metrics);
                        break;
                    
                    case ModelType.Recommendation:
                        EvaluateRecommendationModel(predictions, metrics);
                        break;
                    
                    case ModelType.TimeSeries:
                        EvaluateTimeSeriesModel(predictions, metrics);
                        break;
                    
                    default:
                        throw new NotSupportedException($"Model type {modelDefinition.Type} is not supported for evaluation");
                }
                
                _logger.LogInformation("Model evaluation completed: {ModelName}", modelDefinition.Name);
                
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating model: {ModelName}", modelDefinition.Name);
                throw;
            }
        }
        
        /// <summary>
        /// Evaluates a binary classification model
        /// </summary>
        private void EvaluateBinaryClassificationModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // Get the label column name
            var labelColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Label"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Label";
            
            // Get the score column name
            var scoreColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Score"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Score";
            
            // Get the probability column name
            var probabilityColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Probability"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Probability";
            
            // Evaluate the model
            var evaluationMetrics = _mlContext.BinaryClassification.Evaluate(
                predictions,
                labelColumnName: labelColumnName,
                scoreColumnName: scoreColumnName,
                probabilityColumnName: probabilityColumnName);
            
            // Add metrics
            metrics["Accuracy"] = evaluationMetrics.Accuracy;
            metrics["AreaUnderRocCurve"] = evaluationMetrics.AreaUnderRocCurve;
            metrics["AreaUnderPrecisionRecallCurve"] = evaluationMetrics.AreaUnderPrecisionRecallCurve;
            metrics["F1Score"] = evaluationMetrics.F1Score;
            metrics["PositivePrecision"] = evaluationMetrics.PositivePrecision;
            metrics["PositiveRecall"] = evaluationMetrics.PositiveRecall;
            metrics["NegativePrecision"] = evaluationMetrics.NegativePrecision;
            metrics["NegativeRecall"] = evaluationMetrics.NegativeRecall;
        }
        
        /// <summary>
        /// Evaluates a multi-class classification model
        /// </summary>
        private void EvaluateMultiClassClassificationModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // Get the label column name
            var labelColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Label"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Label";
            
            // Get the score column name
            var scoreColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Score"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Score";
            
            // Evaluate the model
            var evaluationMetrics = _mlContext.MulticlassClassification.Evaluate(
                predictions,
                labelColumnName: labelColumnName,
                scoreColumnName: scoreColumnName);
            
            // Add metrics
            metrics["MicroAccuracy"] = evaluationMetrics.MicroAccuracy;
            metrics["MacroAccuracy"] = evaluationMetrics.MacroAccuracy;
            metrics["LogLoss"] = evaluationMetrics.LogLoss;
            metrics["LogLossReduction"] = evaluationMetrics.LogLossReduction;
            
            // Add per-class metrics
            for (int i = 0; i < evaluationMetrics.PerClassPrecision.Length; i++)
            {
                metrics[$"Precision_Class{i}"] = evaluationMetrics.PerClassPrecision[i];
                metrics[$"Recall_Class{i}"] = evaluationMetrics.PerClassRecall[i];
            }
        }
        
        /// <summary>
        /// Evaluates a regression model
        /// </summary>
        private void EvaluateRegressionModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // Get the label column name
            var labelColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Label"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Label";
            
            // Get the score column name
            var scoreColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Score"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Score";
            
            // Evaluate the model
            var evaluationMetrics = _mlContext.Regression.Evaluate(
                predictions,
                labelColumnName: labelColumnName,
                scoreColumnName: scoreColumnName);
            
            // Add metrics
            metrics["RSquared"] = evaluationMetrics.RSquared;
            metrics["MeanAbsoluteError"] = evaluationMetrics.MeanAbsoluteError;
            metrics["MeanSquaredError"] = evaluationMetrics.MeanSquaredError;
            metrics["RootMeanSquaredError"] = evaluationMetrics.RootMeanSquaredError;
        }
        
        /// <summary>
        /// Evaluates a clustering model
        /// </summary>
        private void EvaluateClusteringModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // Get the feature column name
            var featureColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Features"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Features";
            
            // Get the score column name
            var scoreColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Score"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Score";
            
            // Evaluate the model
            var evaluationMetrics = _mlContext.Clustering.Evaluate(
                predictions,
                featuresColumnName: featureColumnName,
                scoreColumnName: scoreColumnName);
            
            // Add metrics
            metrics["AverageDistance"] = evaluationMetrics.AverageDistance;
            metrics["DaviesBouldinIndex"] = evaluationMetrics.DaviesBouldinIndex;
        }
        
        /// <summary>
        /// Evaluates an anomaly detection model
        /// </summary>
        private void EvaluateAnomalyDetectionModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // Get the label column name
            var labelColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Label"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Label";
            
            // Get the score column name
            var scoreColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Score"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Score";
            
            // Get the prediction column name
            var predictionColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("PredictedLabel"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "PredictedLabel";
            
            // Evaluate the model
            var evaluationMetrics = _mlContext.AnomalyDetection.Evaluate(
                predictions,
                labelColumnName: labelColumnName,
                scoreColumnName: scoreColumnName,
                predictedLabelColumnName: predictionColumnName);
            
            // Add metrics
            metrics["AreaUnderRocCurve"] = evaluationMetrics.AreaUnderRocCurve;
            metrics["DetectionRateAtFalsePositiveCount"] = evaluationMetrics.DetectionRateAtFalsePositiveCount;
        }
        
        /// <summary>
        /// Evaluates a recommendation model
        /// </summary>
        private void EvaluateRecommendationModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // For recommendation models, we use regression metrics
            EvaluateRegressionModel(predictions, metrics);
        }
        
        /// <summary>
        /// Evaluates a time series model
        /// </summary>
        private void EvaluateTimeSeriesModel(IDataView predictions, Dictionary<string, double> metrics)
        {
            // For time series models, we use regression metrics
            EvaluateRegressionModel(predictions, metrics);
            
            // Add custom metrics
            metrics["MeanAbsolutePercentageError"] = CalculateMeanAbsolutePercentageError(predictions);
        }
        
        /// <summary>
        /// Calculates the mean absolute percentage error
        /// </summary>
        private double CalculateMeanAbsolutePercentageError(IDataView predictions)
        {
            // Get the label column name
            var labelColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Label"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Label";
            
            // Get the score column name
            var scoreColumnName = predictions.Schema
                .Where(c => c.Name.EndsWith("Score"))
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Score";
            
            // Get the label and score columns
            var labelColumn = predictions.GetColumn<float>(labelColumnName).ToArray();
            var scoreColumn = predictions.GetColumn<float>(scoreColumnName).ToArray();
            
            // Calculate MAPE
            double sum = 0;
            int count = 0;
            
            for (int i = 0; i < labelColumn.Length; i++)
            {
                if (labelColumn[i] != 0)
                {
                    sum += Math.Abs((labelColumn[i] - scoreColumn[i]) / labelColumn[i]);
                    count++;
                }
            }
            
            return count > 0 ? sum / count : 0;
        }
    }
}
