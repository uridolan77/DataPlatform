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
    /// Service for hyperparameter tuning
    /// </summary>
    public class HyperparameterTuningService : IHyperparameterTuningService
    {
        private readonly MLContext _mlContext;
        private readonly ILogger<HyperparameterTuningService> _logger;
        private readonly Random _random = new Random(42);
        
        public HyperparameterTuningService(MLContext mlContext, ILogger<HyperparameterTuningService> logger)
        {
            _mlContext = mlContext;
            _logger = logger;
        }
        
        /// <summary>
        /// Tunes hyperparameters for a given algorithm and dataset
        /// </summary>
        public async Task<Dictionary<string, string>> TuneHyperparametersAsync(
            IDataView data,
            IDataView validationData,
            ModelType modelType,
            string algorithm,
            string featureColumnName,
            string labelColumnName,
            Dictionary<string, object> customSearchSpace = null,
            int maxIterations = 20)
        {
            try
            {
                _logger.LogInformation("Starting hyperparameter tuning for {ModelType} model with {Algorithm} algorithm",
                    modelType, algorithm);
                
                // Get search space (either custom or default)
                var searchSpace = customSearchSpace ?? GetDefaultSearchSpace(modelType, algorithm);
                
                // Initialize best parameters and score
                var bestParameters = new Dictionary<string, string>();
                double bestScore = double.MinValue;
                
                // Random search for hyperparameters
                for (int i = 0; i < maxIterations; i++)
                {
                    _logger.LogDebug("Hyperparameter tuning iteration {Iteration}/{MaxIterations}", i + 1, maxIterations);
                    
                    // Sample parameters from search space
                    var parameters = SampleFromSearchSpace(searchSpace);
                    
                    // Train model with these parameters
                    var (model, score) = await TrainAndEvaluateModelAsync(
                        data, validationData, modelType, algorithm, featureColumnName, labelColumnName, parameters);
                    
                    _logger.LogDebug("Iteration {Iteration} score: {Score}", i + 1, score);
                    
                    // Update best parameters if better score
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestParameters = parameters;
                        _logger.LogInformation("New best score: {Score} with parameters: {Parameters}",
                            bestScore, string.Join(", ", bestParameters.Select(p => $"{p.Key}={p.Value}")));
                    }
                }
                
                _logger.LogInformation("Hyperparameter tuning completed. Best score: {Score}", bestScore);
                return bestParameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hyperparameter tuning");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the default hyperparameter search space for a given algorithm
        /// </summary>
        public Dictionary<string, object> GetDefaultSearchSpace(ModelType modelType, string algorithm)
        {
            var searchSpace = new Dictionary<string, object>();
            
            // Define search spaces based on model type and algorithm
            switch (modelType)
            {
                case ModelType.BinaryClassification:
                    switch (algorithm.ToLowerInvariant())
                    {
                        case "logisticregression":
                            searchSpace["L1Regularization"] = new[] { 0.01f, 0.1f, 1.0f, 10.0f };
                            searchSpace["L2Regularization"] = new[] { 0.01f, 0.1f, 1.0f, 10.0f };
                            break;
                            
                        case "svm":
                            searchSpace["Lambda"] = new[] { 0.00001f, 0.0001f, 0.001f, 0.01f };
                            searchSpace["NumberOfIterations"] = new[] { 10, 50, 100, 500 };
                            break;
                            
                        case "fastforest":
                        case "fasttree":
                            searchSpace["NumberOfTrees"] = new[] { 10, 50, 100, 200, 500 };
                            searchSpace["NumberOfLeaves"] = new[] { 10, 20, 50, 100 };
                            searchSpace["MinimumExampleCountPerLeaf"] = new[] { 1, 5, 10, 20 };
                            searchSpace["LearningRate"] = new[] { 0.01f, 0.05f, 0.1f, 0.2f, 0.5f };
                            break;
                    }
                    break;
                    
                case ModelType.MultiClassClassification:
                    switch (algorithm.ToLowerInvariant())
                    {
                        case "naivebayes":
                            // Naive Bayes has few hyperparameters to tune
                            break;
                            
                        case "sdca":
                        case "lbfgs":
                            searchSpace["L1Regularization"] = new[] { 0.01f, 0.1f, 1.0f, 10.0f };
                            searchSpace["L2Regularization"] = new[] { 0.01f, 0.1f, 1.0f, 10.0f };
                            break;
                    }
                    break;
                    
                case ModelType.Regression:
                    switch (algorithm.ToLowerInvariant())
                    {
                        case "fastforest":
                        case "fasttree":
                            searchSpace["NumberOfTrees"] = new[] { 10, 50, 100, 200, 500 };
                            searchSpace["NumberOfLeaves"] = new[] { 10, 20, 50, 100 };
                            searchSpace["MinimumExampleCountPerLeaf"] = new[] { 1, 5, 10, 20 };
                            searchSpace["LearningRate"] = new[] { 0.01f, 0.05f, 0.1f, 0.2f, 0.5f };
                            break;
                            
                        case "sdca":
                            searchSpace["L1Regularization"] = new[] { 0.01f, 0.1f, 1.0f, 10.0f };
                            searchSpace["L2Regularization"] = new[] { 0.01f, 0.1f, 1.0f, 10.0f };
                            break;
                    }
                    break;
                    
                case ModelType.Clustering:
                    switch (algorithm.ToLowerInvariant())
                    {
                        case "kmeans":
                            searchSpace["NumberOfClusters"] = new[] { 2, 3, 5, 8, 10, 15 };
                            searchSpace["InitializationAlgorithm"] = new[] { "KMeansParallel", "KMeansPlusPlus" };
                            break;
                    }
                    break;
                    
                case ModelType.Recommendation:
                    switch (algorithm.ToLowerInvariant())
                    {
                        case "matrixfactorization":
                            searchSpace["NumberOfIterations"] = new[] { 10, 20, 50, 100 };
                            searchSpace["ApproximationRank"] = new[] { 8, 16, 32, 64, 128 };
                            searchSpace["LearningRate"] = new[] { 0.001f, 0.01f, 0.05f, 0.1f };
                            break;
                    }
                    break;
            }
            
            return searchSpace;
        }
        
        /// <summary>
        /// Samples parameters from the search space
        /// </summary>
        private Dictionary<string, string> SampleFromSearchSpace(Dictionary<string, object> searchSpace)
        {
            var parameters = new Dictionary<string, string>();
            
            foreach (var param in searchSpace)
            {
                if (param.Value is Array array)
                {
                    // Randomly select one value from the array
                    int index = _random.Next(array.Length);
                    var value = array.GetValue(index);
                    parameters[param.Key] = value.ToString();
                }
                else if (param.Value is Range range)
                {
                    // Sample from range
                    var value = _random.Next(range.Start.Value, range.End.Value);
                    parameters[param.Key] = value.ToString();
                }
                else
                {
                    // Use the value directly
                    parameters[param.Key] = param.Value.ToString();
                }
            }
            
            return parameters;
        }
        
        /// <summary>
        /// Trains and evaluates a model with the given parameters
        /// </summary>
        private async Task<(ITransformer Model, double Score)> TrainAndEvaluateModelAsync(
            IDataView trainData,
            IDataView validationData,
            ModelType modelType,
            string algorithm,
            string featureColumnName,
            string labelColumnName,
            Dictionary<string, string> parameters)
        {
            // Create a pipeline with the specified parameters
            var pipeline = CreatePipeline(modelType, algorithm, featureColumnName, labelColumnName, parameters);
            
            // Train the model
            var model = pipeline.Fit(trainData);
            
            // Evaluate the model
            var predictions = model.Transform(validationData);
            var score = EvaluateModel(predictions, modelType, labelColumnName);
            
            return (model, score);
        }
        
        /// <summary>
        /// Creates a training pipeline with the specified parameters
        /// </summary>
        private IEstimator<ITransformer> CreatePipeline(
            ModelType modelType,
            string algorithm,
            string featureColumnName,
            string labelColumnName,
            Dictionary<string, string> parameters)
        {
            IEstimator<ITransformer> trainer = null;
            
            // Create trainer based on model type and algorithm
            switch (modelType)
            {
                case ModelType.BinaryClassification:
                    trainer = CreateBinaryClassificationTrainer(algorithm, labelColumnName, featureColumnName, parameters);
                    break;
                    
                case ModelType.MultiClassClassification:
                    trainer = CreateMultiClassClassificationTrainer(algorithm, labelColumnName, featureColumnName, parameters);
                    break;
                    
                case ModelType.Regression:
                    trainer = CreateRegressionTrainer(algorithm, labelColumnName, featureColumnName, parameters);
                    break;
                    
                case ModelType.Clustering:
                    trainer = CreateClusteringTrainer(algorithm, featureColumnName, parameters);
                    break;
                    
                case ModelType.Recommendation:
                    trainer = CreateRecommendationTrainer(algorithm, labelColumnName, parameters);
                    break;
                    
                default:
                    throw new NotSupportedException($"Model type {modelType} is not supported for hyperparameter tuning");
            }
            
            return trainer;
        }
        
        /// <summary>
        /// Creates a binary classification trainer with the specified parameters
        /// </summary>
        private IEstimator<ITransformer> CreateBinaryClassificationTrainer(
            string algorithm,
            string labelColumnName,
            string featureColumnName,
            Dictionary<string, string> parameters)
        {
            return algorithm.ToLowerInvariant() switch
            {
                "logisticregression" => _mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    l1Regularization: TryParseFloat(parameters, "L1Regularization"),
                    l2Regularization: TryParseFloat(parameters, "L2Regularization")),
                
                "svm" => _mlContext.BinaryClassification.Trainers.LinearSvm(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    numberOfIterations: TryParseInt(parameters, "NumberOfIterations"),
                    lambda: TryParseFloat(parameters, "Lambda")),
                
                "fastforest" => _mlContext.BinaryClassification.Trainers.FastForest(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    numberOfTrees: TryParseInt(parameters, "NumberOfTrees"),
                    numberOfLeaves: TryParseInt(parameters, "NumberOfLeaves"),
                    minimumExampleCountPerLeaf: TryParseInt(parameters, "MinimumExampleCountPerLeaf")),
                
                "fasttree" => _mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    numberOfTrees: TryParseInt(parameters, "NumberOfTrees"),
                    numberOfLeaves: TryParseInt(parameters, "NumberOfLeaves"),
                    minimumExampleCountPerLeaf: TryParseInt(parameters, "MinimumExampleCountPerLeaf"),
                    learningRate: TryParseFloat(parameters, "LearningRate")),
                
                _ => _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName)
            };
        }
        
        /// <summary>
        /// Creates a multi-class classification trainer with the specified parameters
        /// </summary>
        private IEstimator<ITransformer> CreateMultiClassClassificationTrainer(
            string algorithm,
            string labelColumnName,
            string featureColumnName,
            Dictionary<string, string> parameters)
        {
            return algorithm.ToLowerInvariant() switch
            {
                "naivebayes" => _mlContext.MulticlassClassification.Trainers.NaiveBayes(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName),
                
                "sdca" => _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    l1Regularization: TryParseFloat(parameters, "L1Regularization"),
                    l2Regularization: TryParseFloat(parameters, "L2Regularization")),
                
                "lbfgs" => _mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    l1Regularization: TryParseFloat(parameters, "L1Regularization"),
                    l2Regularization: TryParseFloat(parameters, "L2Regularization")),
                
                _ => _mlContext.MulticlassClassification.Trainers.OneVersusAll(
                    _mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                        labelColumnName: labelColumnName,
                        featureColumnName: featureColumnName))
            };
        }
        
        /// <summary>
        /// Creates a regression trainer with the specified parameters
        /// </summary>
        private IEstimator<ITransformer> CreateRegressionTrainer(
            string algorithm,
            string labelColumnName,
            string featureColumnName,
            Dictionary<string, string> parameters)
        {
            return algorithm.ToLowerInvariant() switch
            {
                "fastforest" => _mlContext.Regression.Trainers.FastForest(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    numberOfTrees: TryParseInt(parameters, "NumberOfTrees"),
                    numberOfLeaves: TryParseInt(parameters, "NumberOfLeaves"),
                    minimumExampleCountPerLeaf: TryParseInt(parameters, "MinimumExampleCountPerLeaf")),
                
                "fasttree" => _mlContext.Regression.Trainers.FastTree(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    numberOfTrees: TryParseInt(parameters, "NumberOfTrees"),
                    numberOfLeaves: TryParseInt(parameters, "NumberOfLeaves"),
                    minimumExampleCountPerLeaf: TryParseInt(parameters, "MinimumExampleCountPerLeaf"),
                    learningRate: TryParseFloat(parameters, "LearningRate")),
                
                "sdca" => _mlContext.Regression.Trainers.Sdca(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName,
                    l1Regularization: TryParseFloat(parameters, "L1Regularization"),
                    l2Regularization: TryParseFloat(parameters, "L2Regularization")),
                
                _ => _mlContext.Regression.Trainers.LbfgsPoissonRegression(
                    labelColumnName: labelColumnName,
                    featureColumnName: featureColumnName)
            };
        }
        
        /// <summary>
        /// Creates a clustering trainer with the specified parameters
        /// </summary>
        private IEstimator<ITransformer> CreateClusteringTrainer(
            string algorithm,
            string featureColumnName,
            Dictionary<string, string> parameters)
        {
            return algorithm.ToLowerInvariant() switch
            {
                "kmeans" => _mlContext.Clustering.Trainers.KMeans(
                    featureColumnName: featureColumnName,
                    numberOfClusters: TryParseInt(parameters, "NumberOfClusters")),
                
                _ => _mlContext.Clustering.Trainers.KMeans(
                    featureColumnName: featureColumnName)
            };
        }
        
        /// <summary>
        /// Creates a recommendation trainer with the specified parameters
        /// </summary>
        private IEstimator<ITransformer> CreateRecommendationTrainer(
            string algorithm,
            string labelColumnName,
            Dictionary<string, string> parameters)
        {
            return algorithm.ToLowerInvariant() switch
            {
                "matrixfactorization" => _mlContext.Recommendation().Trainers.MatrixFactorization(
                    labelColumnName: labelColumnName,
                    numberOfIterations: TryParseInt(parameters, "NumberOfIterations"),
                    approximationRank: TryParseInt(parameters, "ApproximationRank"),
                    learningRate: TryParseFloat(parameters, "LearningRate")),
                
                _ => _mlContext.Recommendation().Trainers.MatrixFactorization(
                    labelColumnName: labelColumnName)
            };
        }
        
        /// <summary>
        /// Evaluates a model and returns a score
        /// </summary>
        private double EvaluateModel(IDataView predictions, ModelType modelType, string labelColumnName)
        {
            switch (modelType)
            {
                case ModelType.BinaryClassification:
                    var binaryMetrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: labelColumnName);
                    return binaryMetrics.AreaUnderRocCurve; // Use AUC as the score
                
                case ModelType.MultiClassClassification:
                    var multiClassMetrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: labelColumnName);
                    return multiClassMetrics.MicroAccuracy; // Use accuracy as the score
                
                case ModelType.Regression:
                    var regressionMetrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: labelColumnName);
                    return -regressionMetrics.RootMeanSquaredError; // Negative RMSE (higher is better)
                
                case ModelType.Clustering:
                    var clusteringMetrics = _mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score", featureColumnName: "Features");
                    return -clusteringMetrics.AverageDistance; // Negative average distance (higher is better)
                
                case ModelType.Recommendation:
                    var recommendationMetrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: labelColumnName);
                    return -recommendationMetrics.RootMeanSquaredError; // Negative RMSE (higher is better)
                
                default:
                    throw new NotSupportedException($"Evaluation for model type {modelType} is not supported");
            }
        }
        
        /// <summary>
        /// Helper method to parse float from parameters
        /// </summary>
        private float? TryParseFloat(Dictionary<string, string> parameters, string key)
        {
            if (parameters.TryGetValue(key, out var value) && float.TryParse(value, out var result))
            {
                return result;
            }
            return null;
        }
        
        /// <summary>
        /// Helper method to parse int from parameters
        /// </summary>
        private int? TryParseInt(Dictionary<string, string> parameters, string key)
        {
            if (parameters.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            {
                return result;
            }
            return null;
        }
    }
}
