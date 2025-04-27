using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Configuration for AutoML process
    /// </summary>
    public class AutoMLConfig
    {
        /// <summary>
        /// Name for the model to be created
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Description for the model to be created
        /// </summary>
        public string ModelDescription { get; set; }
        
        /// <summary>
        /// Type of model to train
        /// </summary>
        public ModelType ModelType { get; set; }
        
        /// <summary>
        /// Maximum time to spend on training in seconds
        /// </summary>
        public int MaxTrainingTimeInSeconds { get; set; } = 3600; // Default: 1 hour
        
        /// <summary>
        /// Maximum number of models to try
        /// </summary>
        public int MaxModelsToTry { get; set; } = 10;
        
        /// <summary>
        /// Metric to optimize
        /// </summary>
        public string OptimizationMetric { get; set; }
        
        /// <summary>
        /// Whether to perform feature selection
        /// </summary>
        public bool EnableFeatureSelection { get; set; } = true;
        
        /// <summary>
        /// Whether to perform hyperparameter tuning
        /// </summary>
        public bool EnableHyperparameterTuning { get; set; } = true;
        
        /// <summary>
        /// List of algorithms to try (if empty, will try all applicable algorithms)
        /// </summary>
        public List<string> AlgorithmsToTry { get; set; } = new List<string>();
        
        /// <summary>
        /// Validation split percentage (0.0 to 1.0)
        /// </summary>
        public double ValidationSplitPercentage { get; set; } = 0.2;
        
        /// <summary>
        /// Whether to use cross-validation
        /// </summary>
        public bool UseCrossValidation { get; set; } = true;
        
        /// <summary>
        /// Number of folds for cross-validation
        /// </summary>
        public int CrossValidationFolds { get; set; } = 5;
        
        /// <summary>
        /// Whether to include feature importance in the results
        /// </summary>
        public bool IncludeFeatureImportance { get; set; } = true;
        
        /// <summary>
        /// Whether to save all tried models or just the best one
        /// </summary>
        public bool SaveAllTriedModels { get; set; } = false;
        
        /// <summary>
        /// Custom hyperparameter search space (if null, will use default search space)
        /// </summary>
        public Dictionary<string, object> CustomHyperparameterSearchSpace { get; set; }
    }
    
    /// <summary>
    /// Result of AutoML process
    /// </summary>
    public class AutoMLResult
    {
        /// <summary>
        /// Best model found
        /// </summary>
        public TrainedModel BestModel { get; set; }
        
        /// <summary>
        /// All models tried during the AutoML process
        /// </summary>
        public List<TrainedModel> AllTriedModels { get; set; } = new List<TrainedModel>();
        
        /// <summary>
        /// Feature importance for the best model
        /// </summary>
        public Dictionary<string, double> FeatureImportance { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Time spent on training in seconds
        /// </summary>
        public double TrainingTimeInSeconds { get; set; }
        
        /// <summary>
        /// Number of models tried
        /// </summary>
        public int ModelsTriedCount { get; set; }
        
        /// <summary>
        /// Best hyperparameters found
        /// </summary>
        public Dictionary<string, string> BestHyperparameters { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Best algorithm found
        /// </summary>
        public string BestAlgorithm { get; set; }
        
        /// <summary>
        /// Selected features
        /// </summary>
        public List<string> SelectedFeatures { get; set; } = new List<string>();
    }
}
