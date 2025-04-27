using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Data;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Services.Infrastructure;
using GenericDataPlatform.ML.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for making predictions with ML models
    /// </summary>
    public class PredictionService : IPredictionService
    {
        private readonly IModelManagementService _modelManagementService;
        private readonly IModelRepository _modelRepository;
        private readonly IMetadataRepository _metadataRepository;
        private readonly IDynamicObjectGenerator _dynamicObjectGenerator;
        private readonly IStorageService _storageService;
        private readonly MLContext _mlContext;
        private readonly ILogger<PredictionService> _logger;

        // Cache of loaded models to avoid reloading the same model multiple times
        private readonly Dictionary<string, (ITransformer Model, DateTime LoadedAt, ModelMetadata Metadata)> _modelCache =
            new Dictionary<string, (ITransformer, DateTime, ModelMetadata)>();
        private readonly SemaphoreSlim _modelCacheLock = new SemaphoreSlim(1, 1);

        // Queue of batch prediction jobs
        private readonly Queue<BatchPredictionJob> _jobQueue = new Queue<BatchPredictionJob>();
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);

        // In-memory cache of jobs for quick lookup
        private readonly Dictionary<string, BatchPredictionJob> _jobs = new Dictionary<string, BatchPredictionJob>();
        private readonly SemaphoreSlim _jobsLock = new SemaphoreSlim(1, 1);

        // Cache expiration time (models will be reloaded if older than this)
        private readonly TimeSpan _cacheExpirationTime = TimeSpan.FromHours(1);

        public PredictionService(
            IModelManagementService modelManagementService,
            IModelRepository modelRepository,
            IMetadataRepository metadataRepository,
            IDynamicObjectGenerator dynamicObjectGenerator,
            IStorageService storageService,
            MLContext mlContext,
            ILogger<PredictionService> logger)
        {
            _modelManagementService = modelManagementService ?? throw new ArgumentNullException(nameof(modelManagementService));
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
            _dynamicObjectGenerator = dynamicObjectGenerator ?? throw new ArgumentNullException(nameof(dynamicObjectGenerator));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _mlContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Restore active jobs from repository
            Task.Run(async () => await RestoreActiveJobsAsync());
        }

        /// <summary>
        /// Makes predictions using a model
        /// </summary>
        public async Task<PredictionResponse> PredictAsync(
            string modelName,
            string? modelVersion,
            List<Dictionary<string, object>> instances)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Making predictions with model {ModelName} version {ModelVersion} for {InstanceCount} instances",
                    modelName, modelVersion ?? "latest", instances.Count);

                // Validate inputs
                if (string.IsNullOrEmpty(modelName))
                {
                    throw new ArgumentException("Model name is required", nameof(modelName));
                }

                if (instances == null || instances.Count == 0)
                {
                    throw new ArgumentException("Instances are required", nameof(instances));
                }

                // Load the model
                (ITransformer model, ModelMetadata metadata) = await LoadModelAsync(modelName, modelVersion);

                // Create prediction engine pool
                var predictionEngine = _dynamicObjectGenerator.CreatePredictionEngine(
                    _mlContext, model, metadata.InputSchema, metadata.OutputSchema);

                // Make predictions
                var predictions = new List<Dictionary<string, object>>();
                var errorCount = 0;

                foreach (var instance in instances)
                {
                    try
                    {
                        // Convert instance to the expected input type
                        var input = _dynamicObjectGenerator.CreateObject(instance, metadata.InputSchema);

                        // Make prediction
                        var prediction = predictionEngine.Predict(input);

                        // Convert prediction to dictionary
                        var predictionDict = _dynamicObjectGenerator.ConvertToDictionary(prediction);

                        predictions.Add(predictionDict);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error making prediction for instance: {Instance}",
                            System.Text.Json.JsonSerializer.Serialize(instance));

                        // Add an error prediction
                        predictions.Add(new Dictionary<string, object>
                        {
                            ["error"] = ex.Message
                        });

                        errorCount++;
                    }
                }

                // Track model usage statistics
                stopwatch.Stop();
                await _modelManagementService.UpdateModelUsageStatsAsync(
                    modelName,
                    metadata.Version,
                    instances.Count,
                    stopwatch.ElapsedMilliseconds / (double)instances.Count,
                    errorCount);

                // Create response
                var response = new PredictionResponse
                {
                    Predictions = predictions,
                    ModelInfo = new ModelInfo
                    {
                        Name = metadata.Name,
                        Version = metadata.Version,
                        Type = metadata.Type,
                        InputSchema = metadata.InputSchema,
                        OutputSchema = metadata.OutputSchema
                    },
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Made predictions with model {ModelName} version {ModelVersion} in {ElapsedMs}ms",
                    modelName, metadata.Version, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making predictions with model {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                throw;
            }
        }

        /// <summary>
        /// Submits a batch prediction job
        /// </summary>
        public async Task<string> SubmitBatchPredictionJobAsync(BatchPredictionRequest request)
        {
            // Validate the request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.ModelName))
            {
                throw new ArgumentException("ModelName is required", nameof(request));
            }

            if (string.IsNullOrEmpty(request.InputDataLocation))
            {
                throw new ArgumentException("InputDataLocation is required", nameof(request));
            }

            if (string.IsNullOrEmpty(request.OutputDataLocation))
            {
                throw new ArgumentException("OutputDataLocation is required", nameof(request));
            }

            // Create a new job
            var job = BatchPredictionJob.FromRequest(request);

            // Add the job to the repository
            await _metadataRepository.SaveBatchPredictionJobAsync(job);

            // Add the job to the in-memory cache
            await _jobsLock.WaitAsync();
            try
            {
                _jobs[job.Id] = job;
            }
            finally
            {
                _jobsLock.Release();
            }

            // Add the job to the queue
            await _queueLock.WaitAsync();
            try
            {
                _jobQueue.Enqueue(job);
            }
            finally
            {
                _queueLock.Release();
            }

            _logger.LogInformation("Batch prediction job {JobId} submitted for model {ModelName}",
                job.Id, request.ModelName);

            return job.Id;
        }

        /// <summary>
        /// Gets the status of a batch prediction job
        /// </summary>
        public async Task<BatchPredictionStatus> GetBatchPredictionStatusAsync(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentException("Job ID is required", nameof(jobId));
            }

            // Check the in-memory cache first
            await _jobsLock.WaitAsync();
            try
            {
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    return job.Status;
                }
            }
            finally
            {
                _jobsLock.Release();
            }

            // If not in memory, check the repository
            var persistedJob = await _metadataRepository.GetBatchPredictionJobAsync(jobId);
            if (persistedJob != null)
            {
                // Add to in-memory cache
                await _jobsLock.WaitAsync();
                try
                {
                    _jobs[jobId] = persistedJob;
                }
                finally
                {
                    _jobsLock.Release();
                }

                return persistedJob.Status;
            }

            return null;
        }

        /// <summary>
        /// Gets a prediction result by ID
        /// </summary>
        public async Task<PredictionResponse?> GetPredictionResultAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("ID is required", nameof(id));
            }

            try
            {
                _logger.LogInformation("Getting prediction result: {Id}", id);

                // In a real implementation, we would retrieve the prediction result from a database
                // For now, we'll return null to indicate that the result was not found
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prediction result: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets all batch prediction jobs
        /// </summary>
        public async Task<List<BatchPredictionStatus>> GetBatchPredictionJobsAsync(string? filter = null, int skip = 0, int take = 20)
        {
            var jobs = await _metadataRepository.GetBatchPredictionJobsAsync(filter, skip, take);
            return jobs.Select(j => j.Status).ToList();
        }

        /// <summary>
        /// Cancels a batch prediction job
        /// </summary>
        public async Task<bool> CancelBatchPredictionJobAsync(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentException("Job ID is required", nameof(jobId));
            }

            // Get the job
            BatchPredictionJob job = null;

            await _jobsLock.WaitAsync();
            try
            {
                if (_jobs.TryGetValue(jobId, out job))
                {
                    // Can only cancel jobs that are not completed, failed, or already cancelled
                    if (job.Status.State == BatchPredictionState.Completed ||
                        job.Status.State == BatchPredictionState.Failed ||
                        job.Status.State == BatchPredictionState.Cancelled)
                    {
                        return false;
                    }

                    // Update the job status
                    job.Status.State = BatchPredictionState.Cancelled;
                    job.Status.UpdatedAt = DateTime.UtcNow;
                }
            }
            finally
            {
                _jobsLock.Release();
            }

            if (job == null)
            {
                // Try to get from repository
                job = await _metadataRepository.GetBatchPredictionJobAsync(jobId);
                if (job == null)
                {
                    return false;
                }

                // Can only cancel jobs that are not completed, failed, or already cancelled
                if (job.Status.State == BatchPredictionState.Completed ||
                    job.Status.State == BatchPredictionState.Failed ||
                    job.Status.State == BatchPredictionState.Cancelled)
                {
                    return false;
                }

                // Update the job status
                job.Status.State = BatchPredictionState.Cancelled;
                job.Status.UpdatedAt = DateTime.UtcNow;
            }

            // Update in the repository
            await _metadataRepository.SaveBatchPredictionJobAsync(job);

            _logger.LogInformation("Batch prediction job {JobId} cancelled", jobId);

            return true;
        }

        /// <summary>
        /// Executes a batch prediction job
        /// </summary>
        public async Task<BatchPredictionJob> ExecuteBatchPredictionJobAsync(BatchPredictionJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting execution of batch prediction job {JobId} for model {ModelName}",
                    job.Id, job.Request.ModelName);

                // Update job status
                job.Status.State = BatchPredictionState.LoadingData;
                job.Status.StartedAt = DateTime.UtcNow;
                job.Status.UpdatedAt = DateTime.UtcNow;
                job.Status.Progress = 10;
                await _metadataRepository.SaveBatchPredictionJobAsync(job);

                // Load the model
                (ITransformer model, ModelMetadata metadata) = await LoadModelAsync(job.Request.ModelName, job.Request.ModelVersion);

                // Update model version in job status
                job.Status.ModelVersion = metadata.Version;

                // Create prediction engine pool
                var predictionEngine = _dynamicObjectGenerator.CreatePredictionEngine(
                    _mlContext, model, metadata.InputSchema, metadata.OutputSchema);

                // Load input data
                var inputData = await _storageService.LoadDataAsync(job.Request.InputDataLocation);

                job.Status.Stats.TotalRecords = inputData?.Count ?? 0;

                // Update progress
                job.Status.State = BatchPredictionState.Processing;
                job.Status.Progress = 30;
                job.Status.UpdatedAt = DateTime.UtcNow;
                await _metadataRepository.SaveBatchPredictionJobAsync(job);

                // Make predictions
                var predictions = new List<Dictionary<string, object>>();
                var errorCount = 0;
                var processingTimes = new List<double>();

                // Skip processing if no input data
                if (inputData == null || inputData.Count == 0)
                {
                    _logger.LogWarning("No input data found for batch prediction job: {JobId}", job.Id);
                    job.Status.ErrorMessage = "No input data found";
                    return job;
                }

                for (int i = 0; i < inputData.Count; i++)
                {
                    try
                    {
                        var instanceStopwatch = Stopwatch.StartNew();

                        // Convert instance to the expected input type
                        var input = _dynamicObjectGenerator.CreateObject(inputData[i], metadata.InputSchema);

                        // Make prediction
                        var prediction = predictionEngine.Predict(input);

                        // Convert prediction to dictionary
                        var predictionDict = _dynamicObjectGenerator.ConvertToDictionary(prediction);

                        // Add the original input data to the prediction
                        foreach (var kvp in inputData[i])
                        {
                            predictionDict[$"input_{kvp.Key}"] = kvp.Value;
                        }

                        predictions.Add(predictionDict);

                        instanceStopwatch.Stop();
                        processingTimes.Add(instanceStopwatch.ElapsedMilliseconds);

                        job.Status.Stats.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error making prediction for instance {Index}: {Instance}",
                            i, System.Text.Json.JsonSerializer.Serialize(inputData[i]));

                        // Add an error prediction
                        var errorDict = new Dictionary<string, object>
                        {
                            ["error"] = ex.Message
                        };

                        // Add the original input data to the error
                        foreach (var kvp in inputData[i])
                        {
                            errorDict[$"input_{kvp.Key}"] = kvp.Value;
                        }

                        predictions.Add(errorDict);

                        job.Status.Stats.FailedRecords++;
                        errorCount++;
                    }

                    // Update progress every 10% of records
                    if (i % Math.Max(1, inputData.Count / 10) == 0)
                    {
                        var progressPercentage = 30 + (i * 50 / inputData.Count);
                        job.Status.Progress = progressPercentage;
                        job.Status.UpdatedAt = DateTime.UtcNow;
                        job.Status.Stats.AverageProcessingTimeMs = processingTimes.Count > 0 ? processingTimes.Average() : 0;
                        await _metadataRepository.SaveBatchPredictionJobAsync(job);
                    }
                }

                // Calculate average processing time
                job.Status.Stats.AverageProcessingTimeMs = processingTimes.Count > 0 ? processingTimes.Average() : 0;

                // Update progress
                job.Status.State = BatchPredictionState.SavingResults;
                job.Status.Progress = 80;
                job.Status.UpdatedAt = DateTime.UtcNow;
                await _metadataRepository.SaveBatchPredictionJobAsync(job);

                // Save predictions to output location
                await _storageService.SaveDataAsync(predictions, job.Request.OutputDataLocation, "predictions.json");

                // Track model usage statistics
                stopwatch.Stop();
                await _modelManagementService.UpdateModelUsageStatsAsync(
                    job.Request.ModelName,
                    metadata.Version,
                    inputData.Count,
                    processingTimes.Count > 0 ? processingTimes.Average() : 0,
                    errorCount);

                // Update job status
                job.Status.State = BatchPredictionState.Completed;
                job.Status.CompletedAt = DateTime.UtcNow;
                job.Status.UpdatedAt = DateTime.UtcNow;
                job.Status.Progress = 100;
                await _metadataRepository.SaveBatchPredictionJobAsync(job);

                _logger.LogInformation("Batch prediction job {JobId} completed. Processed {TotalRecords} records with {FailedRecords} failures",
                    job.Id, job.Status.Stats.TotalRecords, job.Status.Stats.FailedRecords);

                return job;
            }
            catch (Exception ex)
            {
                // Update job status on error
                job.Status.State = BatchPredictionState.Failed;
                job.Status.UpdatedAt = DateTime.UtcNow;
                job.Status.ErrorMessage = ex.Message;
                await _metadataRepository.SaveBatchPredictionJobAsync(job);

                _logger.LogError(ex, "Error executing batch prediction job {JobId} for model {ModelName}",
                    job.Id, job.Request.ModelName);

                throw;
            }
        }

        /// <summary>
        /// Gets the next batch prediction job to process from the queue
        /// </summary>
        public async Task<BatchPredictionJob?> GetNextBatchPredictionJobAsync()
        {
            await _queueLock.WaitAsync();
            try
            {
                if (_jobQueue.Count > 0)
                {
                    return _jobQueue.Dequeue();
                }

                return null;
            }
            finally
            {
                _queueLock.Release();
            }
        }

        /// <summary>
        /// Loads a model by name and version
        /// </summary>
        private async Task<(ITransformer Model, ModelMetadata Metadata)> LoadModelAsync(string modelName, string? modelVersion = null)
        {
            // Create a cache key
            string cacheKey = $"{modelName}:{modelVersion ?? "latest"}";

            // Check the cache
            await _modelCacheLock.WaitAsync();
            try
            {
                if (_modelCache.TryGetValue(cacheKey, out var cached))
                {
                    // Check if the cached model is still valid
                    if (DateTime.UtcNow - cached.LoadedAt < _cacheExpirationTime)
                    {
                        _logger.LogDebug("Using cached model {ModelName} version {ModelVersion}",
                            modelName, cached.Metadata.Version);

                        return (cached.Model, cached.Metadata);
                    }

                    // Cached model is expired, remove it
                    _modelCache.Remove(cacheKey);
                }
            }
            finally
            {
                _modelCacheLock.Release();
            }

            // Get the model metadata
            var metadata = await _modelManagementService.GetModelAsync(modelName, modelVersion);
            if (metadata == null)
            {
                throw new InvalidOperationException($"Model {modelName} version {modelVersion ?? "latest"} not found");
            }

            // Load the model from the repository
            var modelBytes = await _modelRepository.LoadModelBytesAsync(metadata.ModelPath);

            // Deserialize the model
            var model = _mlContext.Model.Load(new MemoryStream(modelBytes), out var _);

            // Cache the model
            await _modelCacheLock.WaitAsync();
            try
            {
                _modelCache[cacheKey] = (model, DateTime.UtcNow, metadata);
            }
            finally
            {
                _modelCacheLock.Release();
            }

            _logger.LogInformation("Loaded model {ModelName} version {ModelVersion}",
                modelName, metadata.Version);

            return (model, metadata);
        }

        /// <summary>
        /// Loads active jobs from the repository on startup
        /// </summary>
        private async Task RestoreActiveJobsAsync()
        {
            try
            {
                _logger.LogInformation("Restoring active batch prediction jobs");

                // Get all jobs that are not completed, failed, or cancelled
                var activeJobs = await _metadataRepository.GetActiveBatchPredictionJobsAsync();

                await _jobsLock.WaitAsync();
                try
                {
                    foreach (var job in activeJobs)
                    {
                        _jobs[job.Id] = job;
                    }
                }
                finally
                {
                    _jobsLock.Release();
                }

                // Re-queue jobs that are still in the queue
                await _queueLock.WaitAsync();
                try
                {
                    foreach (var job in activeJobs.Where(j => j.Status.State == BatchPredictionState.Queued))
                    {
                        _jobQueue.Enqueue(job);
                    }
                }
                finally
                {
                    _queueLock.Release();
                }

                _logger.LogInformation("Restored {Count} active batch prediction jobs", activeJobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring active batch prediction jobs");
            }
        }
    }

    /// <summary>
    /// Background service for processing batch prediction jobs
    /// </summary>
    public class BatchPredictionJobProcessor : BackgroundService
    {
        private readonly IPredictionService _predictionService;
        private readonly ILogger<BatchPredictionJobProcessor> _logger;

        public BatchPredictionJobProcessor(
            IPredictionService predictionService,
            ILogger<BatchPredictionJobProcessor> logger)
        {
            _predictionService = predictionService ?? throw new ArgumentNullException(nameof(predictionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Batch prediction job processor starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get the next job from the queue
                    var job = await _predictionService.GetNextBatchPredictionJobAsync();

                    if (job != null)
                    {
                        _logger.LogInformation("Processing batch prediction job {JobId}", job.Id);

                        // Execute the job
                        await _predictionService.ExecuteBatchPredictionJobAsync(job);
                    }
                    else
                    {
                        // No jobs in the queue, wait a bit
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch prediction job");

                    // Wait a bit before retrying
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("Batch prediction job processor stopping");
        }
    }
}