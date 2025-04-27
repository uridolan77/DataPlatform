using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Data;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Services.Infrastructure;
using GenericDataPlatform.ML.Training;
using GenericDataPlatform.ML.Utils;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for orchestrating training jobs
    /// </summary>
    public class TrainingOrchestrationService : ITrainingOrchestrationService
    {
        private readonly IMetadataRepository _metadataRepository;
        private readonly IMLflowIntegrationService _mlflowService;
        private readonly IModelTrainer _modelTrainer;
        private readonly IModelManagementService _modelManagementService;
        private readonly IDynamicObjectGenerator _dynamicObjectGenerator;
        private readonly IStorageService _storageService;
        private readonly ILogger<TrainingOrchestrationService> _logger;
        
        // Queue of training jobs
        private readonly Queue<TrainingJob> _jobQueue = new Queue<TrainingJob>();
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);
        
        // In-memory cache of jobs for quick lookup
        private readonly Dictionary<string, TrainingJob> _jobs = new Dictionary<string, TrainingJob>();
        private readonly SemaphoreSlim _jobsLock = new SemaphoreSlim(1, 1);
        
        public TrainingOrchestrationService(
            IMetadataRepository metadataRepository,
            IMLflowIntegrationService mlflowService,
            IModelTrainer modelTrainer,
            IModelManagementService modelManagementService,
            IDynamicObjectGenerator dynamicObjectGenerator,
            IStorageService storageService,
            ILogger<TrainingOrchestrationService> logger)
        {
            _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
            _mlflowService = mlflowService ?? throw new ArgumentNullException(nameof(mlflowService));
            _modelTrainer = modelTrainer ?? throw new ArgumentNullException(nameof(modelTrainer));
            _modelManagementService = modelManagementService ?? throw new ArgumentNullException(nameof(modelManagementService));
            _dynamicObjectGenerator = dynamicObjectGenerator ?? throw new ArgumentNullException(nameof(dynamicObjectGenerator));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Restore active jobs from repository
            Task.Run(async () => await RestoreActiveJobsAsync());
        }
        
        /// <summary>
        /// Submits a new training job
        /// </summary>
        public async Task<string> SubmitTrainingJobAsync(TrainingJobRequest request)
        {
            // Validate the request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            
            if (request.ModelDefinition == null)
            {
                throw new ArgumentException("ModelDefinition is required", nameof(request));
            }
            
            if (string.IsNullOrEmpty(request.DataSourceId))
            {
                throw new ArgumentException("DataSourceId is required", nameof(request));
            }
            
            // Create a new job
            var job = TrainingJob.FromRequest(request);
            
            // Add the job to the repository
            await _metadataRepository.SaveTrainingJobAsync(job);
            
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
            
            _logger.LogInformation("Training job {JobId} submitted for model {ModelName}",
                job.Id, request.ModelDefinition.Name);
            
            return job.Id;
        }
        
        /// <summary>
        /// Gets the status of a training job
        /// </summary>
        public async Task<TrainingJobStatus> GetTrainingJobStatusAsync(string jobId)
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
            var persistedJob = await _metadataRepository.GetTrainingJobAsync(jobId);
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
        /// Gets all training jobs
        /// </summary>
        public async Task<List<TrainingJobStatus>> GetTrainingJobsAsync(string filter = null, int skip = 0, int take = 20)
        {
            var jobs = await _metadataRepository.GetTrainingJobsAsync(filter, skip, take);
            return jobs.Select(j => j.Status).ToList();
        }
        
        /// <summary>
        /// Cancels a training job
        /// </summary>
        public async Task<bool> CancelTrainingJobAsync(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentException("Job ID is required", nameof(jobId));
            }
            
            // Get the job
            TrainingJob job = null;
            
            await _jobsLock.WaitAsync();
            try
            {
                if (_jobs.TryGetValue(jobId, out job))
                {
                    // Can only cancel jobs that are not completed, failed, or already cancelled
                    if (job.Status.State == TrainingJobState.Completed ||
                        job.Status.State == TrainingJobState.Failed ||
                        job.Status.State == TrainingJobState.Cancelled)
                    {
                        return false;
                    }
                    
                    // Update the job status
                    job.Status.State = TrainingJobState.Cancelled;
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
                job = await _metadataRepository.GetTrainingJobAsync(jobId);
                if (job == null)
                {
                    return false;
                }
                
                // Can only cancel jobs that are not completed, failed, or already cancelled
                if (job.Status.State == TrainingJobState.Completed ||
                    job.Status.State == TrainingJobState.Failed ||
                    job.Status.State == TrainingJobState.Cancelled)
                {
                    return false;
                }
                
                // Update the job status
                job.Status.State = TrainingJobState.Cancelled;
                job.Status.UpdatedAt = DateTime.UtcNow;
            }
            
            // Update in the repository
            await _metadataRepository.SaveTrainingJobAsync(job);
            
            _logger.LogInformation("Training job {JobId} cancelled", jobId);
            
            return true;
        }
        
        /// <summary>
        /// Executes a training job
        /// </summary>
        public async Task<TrainingJob> ExecuteTrainingJobAsync(TrainingJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }
            
            try
            {
                _logger.LogInformation("Starting execution of training job {JobId} for model {ModelName}",
                    job.Id, job.Request.ModelDefinition.Name);
                
                // Update job status
                job.Status.State = TrainingJobState.PreparingData;
                job.Status.StartedAt = DateTime.UtcNow;
                job.Status.UpdatedAt = DateTime.UtcNow;
                job.Status.Progress = 10;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Initialize MLflow run
                var experimentId = job.Request.ExperimentId;
                if (string.IsNullOrEmpty(experimentId))
                {
                    // Use default experiment
                    experimentId = await _mlflowService.GetOrCreateExperimentAsync("Default");
                }
                
                var runName = job.Request.RunName ?? $"Training run {job.Id}";
                var runId = await _mlflowService.CreateRunAsync(experimentId, runName);
                
                job.Status.RunId = runId;
                job.Status.ExperimentId = experimentId;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Log parameters to MLflow
                await _mlflowService.LogParametersAsync(runId, job.Request.Parameters);
                
                // Load training data
                var trainingData = await LoadDataAsync(job.Request.DataSourceId);
                
                // Update progress
                job.Status.State = TrainingJobState.Training;
                job.Status.Progress = 30;
                job.Status.UpdatedAt = DateTime.UtcNow;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Load validation data if specified
                List<Dictionary<string, object>> validationData = null;
                if (!string.IsNullOrEmpty(job.Request.ValidationDataSourceId))
                {
                    validationData = await LoadDataAsync(job.Request.ValidationDataSourceId);
                }
                
                // Train the model
                var trainedModel = await _modelTrainer.TrainModelAsync(
                    job.Request.ModelDefinition, 
                    trainingData, 
                    validationData,
                    new TrainingContext
                    {
                        JobId = job.Id,
                        RunId = runId,
                        Parameters = job.Request.Parameters
                    });
                
                // Update progress
                job.Status.State = TrainingJobState.Evaluating;
                job.Status.Progress = 70;
                job.Status.UpdatedAt = DateTime.UtcNow;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Log metrics to MLflow
                await _mlflowService.LogMetricsAsync(runId, trainedModel.Metrics);
                
                // Save model metrics to job status
                job.Status.Metrics = trainedModel.Metrics;
                
                // Update progress
                job.Status.State = TrainingJobState.RegisteringModel;
                job.Status.Progress = 90;
                job.Status.UpdatedAt = DateTime.UtcNow;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Register the model in MLflow and the model registry
                var modelInfo = await _modelManagementService.RegisterModelAsync(
                    job.Request.ModelDefinition.Name,
                    trainedModel.ModelPath,
                    job.Request.ModelDefinition,
                    runId,
                    experimentId,
                    trainedModel.Metrics);
                
                // Update the job status with the registered model info
                job.Status.State = TrainingJobState.Completed;
                job.Status.CompletedAt = DateTime.UtcNow;
                job.Status.UpdatedAt = DateTime.UtcNow;
                job.Status.Progress = 100;
                job.Status.ModelId = modelInfo.Name;
                job.Status.ModelVersion = modelInfo.Version;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Complete the MLflow run
                await _mlflowService.FinishRunAsync(runId);
                
                _logger.LogInformation("Training job {JobId} completed successfully. Model {ModelName} version {ModelVersion} registered", 
                    job.Id, modelInfo.Name, modelInfo.Version);
                
                return job;
            }
            catch (Exception ex)
            {
                // Update job status on error
                job.Status.State = TrainingJobState.Failed;
                job.Status.UpdatedAt = DateTime.UtcNow;
                job.Status.ErrorMessage = ex.Message;
                await _metadataRepository.SaveTrainingJobAsync(job);
                
                // Try to finish the MLflow run if it was started
                if (!string.IsNullOrEmpty(job.Status.RunId))
                {
                    try
                    {
                        await _mlflowService.FinishRunAsync(job.Status.RunId, "FAILED", ex.Message);
                    }
                    catch (Exception mlflowEx)
                    {
                        _logger.LogWarning(mlflowEx, "Error finishing MLflow run {RunId} for failed job {JobId}", 
                            job.Status.RunId, job.Id);
                    }
                }
                
                _logger.LogError(ex, "Error executing training job {JobId} for model {ModelName}", 
                    job.Id, job.Request.ModelDefinition.Name);
                
                throw;
            }
        }
        
        /// <summary>
        /// Updates the status of a training job
        /// </summary>
        public async Task<TrainingJobStatus> UpdateJobStatusAsync(string jobId, TrainingJobState state, int progress = 0, string message = null)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentException("Job ID is required", nameof(jobId));
            }
            
            // Get the job
            TrainingJob job = null;
            
            await _jobsLock.WaitAsync();
            try
            {
                if (_jobs.TryGetValue(jobId, out job))
                {
                    // Update the job status
                    job.Status.State = state;
                    job.Status.Progress = progress;
                    job.Status.UpdatedAt = DateTime.UtcNow;
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        job.Status.ErrorMessage = message;
                    }
                    
                    if (state == TrainingJobState.Completed)
                    {
                        job.Status.CompletedAt = DateTime.UtcNow;
                    }
                }
            }
            finally
            {
                _jobsLock.Release();
            }
            
            if (job == null)
            {
                // Try to get from repository
                job = await _metadataRepository.GetTrainingJobAsync(jobId);
                if (job == null)
                {
                    return null;
                }
                
                // Update the job status
                job.Status.State = state;
                job.Status.Progress = progress;
                job.Status.UpdatedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(message))
                {
                    job.Status.ErrorMessage = message;
                }
                
                if (state == TrainingJobState.Completed)
                {
                    job.Status.CompletedAt = DateTime.UtcNow;
                }
            }
            
            // Update in the repository
            await _metadataRepository.SaveTrainingJobAsync(job);
            
            _logger.LogInformation("Training job {JobId} status updated to {State} with progress {Progress}",
                jobId, state, progress);
            
            return job.Status;
        }
        
        /// <summary>
        /// Gets the next job to process from the queue
        /// </summary>
        public async Task<TrainingJob> GetNextJobAsync()
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
        /// Loads active jobs from the repository on startup
        /// </summary>
        private async Task RestoreActiveJobsAsync()
        {
            try
            {
                _logger.LogInformation("Restoring active training jobs");
                
                // Get all jobs that are not completed, failed, or cancelled
                var activeJobs = await _metadataRepository.GetActiveTrainingJobsAsync();
                
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
                    foreach (var job in activeJobs.Where(j => j.Status.State == TrainingJobState.Queued))
                    {
                        _jobQueue.Enqueue(job);
                    }
                }
                finally
                {
                    _queueLock.Release();
                }
                
                _logger.LogInformation("Restored {Count} active training jobs", activeJobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring active training jobs");
            }
        }
        
        /// <summary>
        /// Loads data from a data source
        /// </summary>
        private async Task<List<Dictionary<string, object>>> LoadDataAsync(string dataSourceId)
        {
            _logger.LogInformation("Loading data from data source {DataSourceId}", dataSourceId);
            
            var data = await _storageService.GetDataAsync(dataSourceId);
            
            _logger.LogInformation("Loaded {Count} records from data source {DataSourceId}", 
                data?.Count ?? 0, dataSourceId);
            
            return data;
        }
    }
    
    /// <summary>
    /// Background service for processing training jobs
    /// </summary>
    public class TrainingJobProcessor : BackgroundService
    {
        private readonly ITrainingOrchestrationService _trainingService;
        private readonly ILogger<TrainingJobProcessor> _logger;
        
        public TrainingJobProcessor(
            ITrainingOrchestrationService trainingService,
            ILogger<TrainingJobProcessor> logger)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Training job processor starting");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get the next job from the queue
                    var job = await _trainingService.GetNextJobAsync();
                    
                    if (job != null)
                    {
                        _logger.LogInformation("Processing training job {JobId}", job.Id);
                        
                        // Execute the job
                        await _trainingService.ExecuteTrainingJobAsync(job);
                    }
                    else
                    {
                        // No jobs in the queue, wait a bit
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing training job");
                    
                    // Wait a bit before retrying
                    await Task.Delay(5000, stoppingToken);
                }
            }
            
            _logger.LogInformation("Training job processor stopping");
        }
    }
}