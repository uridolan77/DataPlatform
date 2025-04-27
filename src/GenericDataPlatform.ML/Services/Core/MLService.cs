using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Data;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Training;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for machine learning operations
    /// </summary>
    public class MLService : IMLService
    {
        private readonly ILogger<MLService> _logger;
        private readonly IModelTrainer _modelTrainer;
        private readonly MLContext _mlContext;
        private readonly Dictionary<string, ITransformer> _loadedModels = new Dictionary<string, ITransformer>();
        
        public MLService(ILogger<MLService> logger, IModelTrainer modelTrainer)
        {
            _logger = logger;
            _modelTrainer = modelTrainer;
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
                
                // Validate model definition
                ValidateModelDefinition(modelDefinition);
                
                // Train the model
                var trainedModel = await _modelTrainer.TrainModelAsync(modelDefinition, trainingData);
                
                _logger.LogInformation("Model training completed: {ModelName}", modelDefinition.Name);
                
                return trainedModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model: {ModelName}", modelDefinition.Name);
                throw;
            }
        }
        
        /// <summary>
        /// Makes predictions using a trained model
        /// </summary>
        public async Task<IEnumerable<Dictionary<string, object>>> PredictAsync(string modelId, IEnumerable<Dictionary<string, object>> data)
        {
            try
            {
                _logger.LogInformation("Making predictions with model: {ModelId}", modelId);
                
                // Load the model
                var model = await LoadModelAsync(modelId);
                
                // Convert data to IDataView
                var dataView = ConvertToDataView(data);
                
                // Make predictions
                var predictions = model.Transform(dataView);
                
                // Convert predictions to dictionaries
                var result = ConvertFromDataView(predictions);
                
                _logger.LogInformation("Predictions completed for model: {ModelId}", modelId);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making predictions with model: {ModelId}", modelId);
                throw;
            }
        }
        
        /// <summary>
        /// Gets a model by ID
        /// </summary>
        public async Task<ModelDefinition> GetModelAsync(string modelId)
        {
            try
            {
                _logger.LogInformation("Getting model: {ModelId}", modelId);
                
                // In a real implementation, we would retrieve the model from a database
                // For simplicity, we'll return a dummy model
                
                var model = new ModelDefinition
                {
                    Id = modelId,
                    Name = "Dummy Model",
                    Description = "A dummy model for testing",
                    Type = ModelType.BinaryClassification,
                    Algorithm = "LogisticRegression",
                    Features = new List<FeatureDefinition>
                    {
                        new FeatureDefinition
                        {
                            Name = "Feature1",
                            Description = "First feature",
                            DataType = FeatureDataType.Float,
                            IsRequired = true
                        },
                        new FeatureDefinition
                        {
                            Name = "Feature2",
                            Description = "Second feature",
                            DataType = FeatureDataType.Float,
                            IsRequired = true
                        }
                    },
                    Labels = new List<LabelDefinition>
                    {
                        new LabelDefinition
                        {
                            Name = "Label",
                            Description = "Target label",
                            DataType = LabelDataType.Binary
                        }
                    },
                    Version = "1.0",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                };
                
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model: {ModelId}", modelId);
                throw;
            }
        }
        
        /// <summary>
        /// Lists all models
        /// </summary>
        public async Task<IEnumerable<ModelDefinition>> ListModelsAsync()
        {
            try
            {
                _logger.LogInformation("Listing models");
                
                // In a real implementation, we would retrieve models from a database
                // For simplicity, we'll return a list of dummy models
                
                var models = new List<ModelDefinition>
                {
                    new ModelDefinition
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Dummy Model 1",
                        Description = "A dummy model for testing",
                        Type = ModelType.BinaryClassification,
                        Algorithm = "LogisticRegression",
                        Version = "1.0",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new ModelDefinition
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Dummy Model 2",
                        Description = "Another dummy model for testing",
                        Type = ModelType.Regression,
                        Algorithm = "FastTree",
                        Version = "1.0",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    }
                };
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a model
        /// </summary>
        public async Task DeleteModelAsync(string modelId)
        {
            try
            {
                _logger.LogInformation("Deleting model: {ModelId}", modelId);
                
                // In a real implementation, we would delete the model from a database
                // and remove the model file
                
                // Remove from loaded models
                _loadedModels.Remove(modelId);
                
                _logger.LogInformation("Model deleted: {ModelId}", modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model: {ModelId}", modelId);
                throw;
            }
        }
        
        /// <summary>
        /// Validates a model definition
        /// </summary>
        private void ValidateModelDefinition(ModelDefinition modelDefinition)
        {
            if (modelDefinition == null)
            {
                throw new ArgumentNullException(nameof(modelDefinition));
            }
            
            if (string.IsNullOrEmpty(modelDefinition.Name))
            {
                throw new ArgumentException("Model name is required");
            }
            
            if (modelDefinition.Features == null || !modelDefinition.Features.Any())
            {
                throw new ArgumentException("Model must have at least one feature");
            }
            
            if (modelDefinition.Type != ModelType.Clustering && (modelDefinition.Labels == null || !modelDefinition.Labels.Any()))
            {
                throw new ArgumentException("Model must have at least one label (except for clustering models)");
            }
        }
        
        /// <summary>
        /// Loads a model by ID
        /// </summary>
        private async Task<ITransformer> LoadModelAsync(string modelId)
        {
            // Check if the model is already loaded
            if (_loadedModels.TryGetValue(modelId, out var loadedModel))
            {
                return loadedModel;
            }
            
            // In a real implementation, we would retrieve the model path from a database
            // For simplicity, we'll use a dummy path
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", $"{modelId}.zip");
            
            // Check if the model file exists
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {modelPath}");
            }
            
            // Load the model
            var model = _mlContext.Model.Load(modelPath, out var _);
            
            // Cache the model
            _loadedModels[modelId] = model;
            
            return model;
        }
        
        /// <summary>
        /// Converts data to IDataView
        /// </summary>
        private IDataView ConvertToDataView(IEnumerable<Dictionary<string, object>> data)
        {
            // Convert to list of DataRecord
            var records = data.Select(d => new DataRecord
            {
                Data = d,
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = "prediction"
                }
            }).ToList();
            
            // Load as IDataView
            return _mlContext.Data.LoadFromEnumerable(records);
        }
        
        /// <summary>
        /// Converts IDataView to dictionaries
        /// </summary>
        private IEnumerable<Dictionary<string, object>> ConvertFromDataView(IDataView dataView)
        {
            // Convert to list of dictionaries
            var result = new List<Dictionary<string, object>>();
            
            // Get the column names
            var columnNames = dataView.Schema.Select(c => c.Name).ToArray();
            
            // Create a cursor
            using var cursor = dataView.GetRowCursor(columnNames);
            
            // Get value getters for each column
            var getters = columnNames.ToDictionary(
                name => name,
                name => cursor.GetGetter<object>(dataView.Schema[name]));
            
            // Iterate through rows
            while (cursor.MoveNext())
            {
                var row = new Dictionary<string, object>();
                
                // Get values for each column
                foreach (var name in columnNames)
                {
                    object value = null;
                    getters[name](ref value);
                    row[name] = value;
                }
                
                result.Add(row);
            }
            
            return result;
        }
    }
}
