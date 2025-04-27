using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Services.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for managing ML models
    /// </summary>
    public class ModelManagementService : IModelManagementService
    {
        private readonly IModelRepository _modelRepository;
        private readonly IMLflowIntegrationService _mlflowService;
        private readonly ILogger<ModelManagementService> _logger;

        public ModelManagementService(
            IModelRepository modelRepository,
            IMLflowIntegrationService mlflowService,
            ILogger<ModelManagementService> logger)
        {
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _mlflowService = mlflowService ?? throw new ArgumentNullException(nameof(mlflowService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers a model in the registry
        /// </summary>
        public async Task<ModelMetadata> RegisterModelAsync(
            string modelName,
            string modelPath,
            ModelDefinition modelDefinition,
            string runId,
            string experimentId,
            Dictionary<string, double> metrics)
        {
            try
            {
                _logger.LogInformation("Registering model {ModelName} from path {ModelPath}", modelName, modelPath);

                // Validate inputs
                if (string.IsNullOrEmpty(modelName))
                {
                    throw new ArgumentException("Model name is required", nameof(modelName));
                }

                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    throw new ArgumentException($"Model path {modelPath} does not exist", nameof(modelPath));
                }

                if (modelDefinition == null)
                {
                    throw new ArgumentNullException(nameof(modelDefinition));
                }

                // Register model in MLflow
                string version = await _mlflowService.RegisterModelAsync(modelName, modelPath, runId);

                // Create model metadata
                var metadata = new ModelMetadata
                {
                    Name = modelName,
                    Version = version,
                    Definition = modelDefinition,
                    Type = modelDefinition.Type,
                    Stage = "None", // Default stage
                    Description = modelDefinition.Description,
                    ModelPath = modelPath,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System", // TODO: Get from authentication context
                    RunId = runId,
                    ExperimentId = experimentId,
                    Metrics = metrics ?? new Dictionary<string, double>(),
                    Parameters = modelDefinition.Hyperparameters ?? new Dictionary<string, string>(),
                    InputSchema = modelDefinition.Features,
                    OutputSchema = modelDefinition.Labels,
                    StageTransitions = new List<ModelStageTransition>
                    {
                        new ModelStageTransition
                        {
                            FromStage = null,
                            ToStage = "None",
                            Timestamp = DateTime.UtcNow,
                            User = "System", // TODO: Get from authentication context
                            Reason = "Initial registration"
                        }
                    }
                };

                // Save metadata in the repository
                await _modelRepository.SaveModelMetadataAsync(metadata);

                _logger.LogInformation("Model {ModelName} version {ModelVersion} registered successfully",
                    modelName, version);

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering model {ModelName}", modelName);
                throw;
            }
        }

        /// <summary>
        /// Gets a model from the registry
        /// </summary>
        public async Task<ModelMetadata?> GetModelAsync(string modelName, string? version = null)
        {
            try
            {
                _logger.LogInformation("Getting model {ModelName} version {ModelVersion}",
                    modelName, version ?? "latest");

                // Get the model metadata from the repository
                var metadata = await _modelRepository.GetModelMetadataAsync(modelName, version);

                if (metadata == null)
                {
                    _logger.LogWarning("Model {ModelName} version {ModelVersion} not found",
                        modelName, version ?? "latest");
                    return null;
                }

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model {ModelName} version {ModelVersion}",
                    modelName, version ?? "latest");
                throw;
            }
        }

        /// <summary>
        /// Lists all models in the registry
        /// </summary>
        public async Task<List<ModelMetadata>> ListModelsAsync(string? filter = null, int skip = 0, int take = 20)
        {
            try
            {
                _logger.LogInformation("Listing models with filter {Filter}, skip {Skip}, take {Take}",
                    filter, skip, take);

                // Get models from the repository
                var models = await _modelRepository.ListModelMetadataAsync(filter, skip, take);

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                throw;
            }
        }

        /// <summary>
        /// Transitions a model to a different stage
        /// </summary>
        public async Task<bool> TransitionModelStageAsync(string modelName, string version, string stage, string reason = null)
        {
            try
            {
                _logger.LogInformation("Transitioning model {ModelName} version {ModelVersion} to stage {Stage}",
                    modelName, version ?? "latest", stage);

                // Get the model metadata
                var metadata = await _modelRepository.GetModelMetadataAsync(modelName, version);

                if (metadata == null)
                {
                    _logger.LogWarning("Model {ModelName} version {ModelVersion} not found for stage transition",
                        modelName, version ?? "latest");
                    return false;
                }

                // If the model is already in the target stage, do nothing
                if (metadata.Stage == stage)
                {
                    _logger.LogInformation("Model {ModelName} version {ModelVersion} is already in stage {Stage}",
                        modelName, metadata.Version, stage);
                    return true;
                }

                // Get the previous stage
                var previousStage = metadata.Stage;

                // Transition the model in MLflow
                await _mlflowService.TransitionModelStageAsync(modelName, metadata.Version, stage);

                // Update the model metadata
                metadata.Stage = stage;
                metadata.UpdatedAt = DateTime.UtcNow;

                // Add the stage transition to the history
                metadata.StageTransitions.Add(new ModelStageTransition
                {
                    FromStage = previousStage,
                    ToStage = stage,
                    Timestamp = DateTime.UtcNow,
                    User = "System", // TODO: Get from authentication context
                    Reason = string.IsNullOrEmpty(reason) ? $"Transition from {previousStage} to {stage}" : reason
                });

                // If transitioning to "Production", archive any existing production model
                if (stage == "Production")
                {
                    var productionModels = await _modelRepository.GetModelsByStageAsync(modelName, "Production");
                    foreach (var prodModel in productionModels.Where(m => m.Version != metadata.Version))
                    {
                        // Update stage to "Archived"
                        prodModel.Stage = "Archived";
                        prodModel.UpdatedAt = DateTime.UtcNow;

                        // Add the stage transition to the history
                        prodModel.StageTransitions.Add(new ModelStageTransition
                        {
                            FromStage = "Production",
                            ToStage = "Archived",
                            Timestamp = DateTime.UtcNow,
                            User = "System", // TODO: Get from authentication context
                            Reason = $"Automatically archived because model version {metadata.Version} was promoted to Production"
                        });

                        // Save the updated metadata
                        await _modelRepository.SaveModelMetadataAsync(prodModel);

                        // Transition the model in MLflow
                        await _mlflowService.TransitionModelStageAsync(modelName, prodModel.Version, "Archived");
                    }
                }

                // Save the updated metadata
                await _modelRepository.SaveModelMetadataAsync(metadata);

                _logger.LogInformation("Model {ModelName} version {ModelVersion} transitioned from stage {FromStage} to {ToStage}",
                    modelName, metadata.Version, previousStage, stage);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning model {ModelName} version {ModelVersion} to stage {Stage}",
                    modelName, version ?? "latest", stage);
                throw;
            }
        }

        /// <summary>
        /// Deletes a model from the registry
        /// </summary>
        public async Task<bool> DeleteModelAsync(string modelName, string version = null)
        {
            try
            {
                _logger.LogInformation("Deleting model {ModelName} version {ModelVersion}",
                    modelName, version ?? "latest");

                // Get the model metadata
                var metadata = await _modelRepository.GetModelMetadataAsync(modelName, version);

                if (metadata == null)
                {
                    _logger.LogWarning("Model {ModelName} version {ModelVersion} not found for deletion",
                        modelName, version ?? "latest");
                    return false;
                }

                // Delete the model in MLflow
                await _mlflowService.DeleteModelVersionAsync(modelName, metadata.Version);

                // Delete the model metadata from the repository
                await _modelRepository.DeleteModelMetadataAsync(modelName, metadata.Version);

                // Delete the model file if it exists
                if (!string.IsNullOrEmpty(metadata.ModelPath) && File.Exists(metadata.ModelPath))
                {
                    File.Delete(metadata.ModelPath);
                }

                _logger.LogInformation("Model {ModelName} version {ModelVersion} deleted successfully",
                    modelName, metadata.Version);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model {ModelName} version {ModelVersion}",
                    modelName, version ?? "latest");
                throw;
            }
        }

        /// <summary>
        /// Gets all versions of a model
        /// </summary>
        public async Task<List<ModelMetadata>> GetModelVersionsAsync(string modelName)
        {
            try
            {
                _logger.LogInformation("Getting all versions of model {ModelName}", modelName);

                // Get all versions from the repository
                var versions = await _modelRepository.GetModelVersionsAsync(modelName);

                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions of model {ModelName}", modelName);
                throw;
            }
        }

        /// <summary>
        /// Adds a tag to a model
        /// </summary>
        public async Task<bool> AddModelTagAsync(string modelName, string version, string key, string value)
        {
            try
            {
                _logger.LogInformation("Adding tag {Key}={Value} to model {ModelName} version {ModelVersion}",
                    key, value, modelName, version ?? "latest");

                // Get the model metadata
                var metadata = await _modelRepository.GetModelMetadataAsync(modelName, version);

                if (metadata == null)
                {
                    _logger.LogWarning("Model {ModelName} version {ModelVersion} not found for adding tag",
                        modelName, version ?? "latest");
                    return false;
                }

                // Add the tag to MLflow
                await _mlflowService.SetModelTagAsync(modelName, metadata.Version, key, value);

                // Update the model metadata
                metadata.Tags[key] = value;
                metadata.UpdatedAt = DateTime.UtcNow;

                // Save the updated metadata
                await _modelRepository.SaveModelMetadataAsync(metadata);

                _logger.LogInformation("Tag {Key}={Value} added to model {ModelName} version {ModelVersion}",
                    key, value, modelName, metadata.Version);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag {Key}={Value} to model {ModelName} version {ModelVersion}",
                    key, value, modelName, version ?? "latest");
                throw;
            }
        }

        /// <summary>
        /// Updates usage statistics for a model
        /// </summary>
        public async Task<ModelMetadata> UpdateModelUsageStatsAsync(
            string modelName,
            string version,
            int predictionCount,
            double latencyMs,
            int errorCount)
        {
            try
            {
                // Get the model metadata
                var metadata = await _modelRepository.GetModelMetadataAsync(modelName, version);

                if (metadata == null)
                {
                    _logger.LogWarning("Model {ModelName} version {ModelVersion} not found for updating usage stats",
                        modelName, version);
                    return null;
                }

                // Update usage statistics
                metadata.LastUsed = DateTime.UtcNow;
                metadata.UsageStats.TotalPredictions += predictionCount;

                // Update today's predictions count
                metadata.UsageStats.TodayPredictions += predictionCount;

                // Update week's predictions count
                metadata.UsageStats.WeekPredictions += predictionCount;

                // Update month's predictions count
                metadata.UsageStats.MonthPredictions += predictionCount;

                // Update average latency (weighted average)
                var totalPredictions = metadata.UsageStats.TotalPredictions;
                var previousWeight = (double)(totalPredictions - predictionCount) / totalPredictions;
                var newWeight = (double)predictionCount / totalPredictions;
                metadata.UsageStats.AverageLatencyMs =
                    (metadata.UsageStats.AverageLatencyMs * previousWeight) + (latencyMs * newWeight);

                // Update error count and rate
                metadata.UsageStats.ErrorCount += errorCount;
                metadata.UsageStats.ErrorRate = (double)metadata.UsageStats.ErrorCount / totalPredictions;

                // Save the updated metadata
                await _modelRepository.SaveModelMetadataAsync(metadata);

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating usage stats for model {ModelName} version {ModelVersion}",
                    modelName, version);
                throw;
            }
        }
    }
}