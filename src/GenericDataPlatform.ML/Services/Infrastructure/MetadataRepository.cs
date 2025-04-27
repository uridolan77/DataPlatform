using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.ML.Services.Infrastructure
{
    /// <summary>
    /// Repository for ML metadata (jobs, etc.)
    /// </summary>
    public class MetadataRepository : IMetadataRepository
    {
        private readonly ILogger<MetadataRepository> _logger;
        private readonly MetadataRepositoryOptions _options;
        
        public MetadataRepository(
            IOptions<MetadataRepositoryOptions> options,
            ILogger<MetadataRepository> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Create repositories directories if they don't exist
            Directory.CreateDirectory(_options.TrainingJobsDirectory);
            Directory.CreateDirectory(_options.BatchPredictionJobsDirectory);
        }
        
        #region Training Jobs
        
        /// <summary>
        /// Saves a training job to the repository
        /// </summary>
        public async Task SaveTrainingJobAsync(TrainingJob job)
        {
            try
            {
                if (job == null)
                {
                    throw new ArgumentNullException(nameof(job));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.TrainingJobsDirectory, $"{job.Id}.json");
                
                // Serialize job
                var json = JsonSerializer.Serialize(job, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("Saved training job {JobId} to {FilePath}", job.Id, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving training job {JobId}", job?.Id);
                throw;
            }
        }
        
        /// <summary>
        /// Gets a training job from the repository
        /// </summary>
        public async Task<TrainingJob> GetTrainingJobAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentException("Job ID is required", nameof(jobId));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.TrainingJobsDirectory, $"{jobId}.json");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Training job file not found for job {JobId} at {FilePath}", 
                        jobId, filePath);
                    return null;
                }
                
                // Read and deserialize the file
                var json = await File.ReadAllTextAsync(filePath);
                var job = JsonSerializer.Deserialize<TrainingJob>(json);
                
                _logger.LogInformation("Loaded training job {JobId} from {FilePath}", jobId, filePath);
                
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training job {JobId}", jobId);
                throw;
            }
        }
        
        /// <summary>
        /// Gets all training jobs from the repository
        /// </summary>
        public async Task<List<TrainingJob>> GetTrainingJobsAsync(string filter = null, int skip = 0, int take = 20)
        {
            try
            {
                var result = new List<TrainingJob>();
                
                // Get all job files
                var jobFiles = Directory.GetFiles(_options.TrainingJobsDirectory, "*.json");
                
                // Apply filter if specified
                if (!string.IsNullOrEmpty(filter))
                {
                    jobFiles = jobFiles.Where(f => Path.GetFileNameWithoutExtension(f).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                
                // Skip and take
                jobFiles = jobFiles.Skip(skip).Take(take).ToArray();
                
                // Load each job
                foreach (var jobFile in jobFiles)
                {
                    var jobId = Path.GetFileNameWithoutExtension(jobFile);
                    var job = await GetTrainingJobAsync(jobId);
                    
                    if (job != null)
                    {
                        result.Add(job);
                    }
                }
                
                // Sort by creation date (newest first)
                result = result
                    .OrderByDescending(j => j.Status.CreatedAt)
                    .ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training jobs with filter {Filter}, skip {Skip}, take {Take}", 
                    filter, skip, take);
                throw;
            }
        }
        
        /// <summary>
        /// Gets active training jobs (not completed, failed, or cancelled)
        /// </summary>
        public async Task<List<TrainingJob>> GetActiveTrainingJobsAsync()
        {
            try
            {
                var result = new List<TrainingJob>();
                
                // Get all job files
                var jobFiles = Directory.GetFiles(_options.TrainingJobsDirectory, "*.json");
                
                // Load each job
                foreach (var jobFile in jobFiles)
                {
                    var jobId = Path.GetFileNameWithoutExtension(jobFile);
                    var job = await GetTrainingJobAsync(jobId);
                    
                    if (job != null && 
                        job.Status.State != TrainingJobState.Completed &&
                        job.Status.State != TrainingJobState.Failed &&
                        job.Status.State != TrainingJobState.Cancelled)
                    {
                        result.Add(job);
                    }
                }
                
                // Sort by creation date (oldest first)
                result = result
                    .OrderBy(j => j.Status.CreatedAt)
                    .ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active training jobs");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a training job from the repository
        /// </summary>
        public Task DeleteTrainingJobAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentException("Job ID is required", nameof(jobId));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.TrainingJobsDirectory, $"{jobId}.json");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Training job file not found for job {JobId} at {FilePath}", 
                        jobId, filePath);
                    return Task.CompletedTask;
                }
                
                // Delete the file
                File.Delete(filePath);
                
                _logger.LogInformation("Deleted training job {JobId} from {FilePath}", jobId, filePath);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting training job {JobId}", jobId);
                throw;
            }
        }
        
        #endregion
        
        #region Batch Prediction Jobs
        
        /// <summary>
        /// Saves a batch prediction job to the repository
        /// </summary>
        public async Task SaveBatchPredictionJobAsync(BatchPredictionJob job)
        {
            try
            {
                if (job == null)
                {
                    throw new ArgumentNullException(nameof(job));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.BatchPredictionJobsDirectory, $"{job.Id}.json");
                
                // Serialize job
                var json = JsonSerializer.Serialize(job, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("Saved batch prediction job {JobId} to {FilePath}", job.Id, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving batch prediction job {JobId}", job?.Id);
                throw;
            }
        }
        
        /// <summary>
        /// Gets a batch prediction job from the repository
        /// </summary>
        public async Task<BatchPredictionJob> GetBatchPredictionJobAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentException("Job ID is required", nameof(jobId));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.BatchPredictionJobsDirectory, $"{jobId}.json");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Batch prediction job file not found for job {JobId} at {FilePath}", 
                        jobId, filePath);
                    return null;
                }
                
                // Read and deserialize the file
                var json = await File.ReadAllTextAsync(filePath);
                var job = JsonSerializer.Deserialize<BatchPredictionJob>(json);
                
                _logger.LogInformation("Loaded batch prediction job {JobId} from {FilePath}", jobId, filePath);
                
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch prediction job {JobId}", jobId);
                throw;
            }
        }
        
        /// <summary>
        /// Gets all batch prediction jobs from the repository
        /// </summary>
        public async Task<List<BatchPredictionJob>> GetBatchPredictionJobsAsync(string filter = null, int skip = 0, int take = 20)
        {
            try
            {
                var result = new List<BatchPredictionJob>();
                
                // Get all job files
                var jobFiles = Directory.GetFiles(_options.BatchPredictionJobsDirectory, "*.json");
                
                // Apply filter if specified
                if (!string.IsNullOrEmpty(filter))
                {
                    jobFiles = jobFiles.Where(f => Path.GetFileNameWithoutExtension(f).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                
                // Skip and take
                jobFiles = jobFiles.Skip(skip).Take(take).ToArray();
                
                // Load each job
                foreach (var jobFile in jobFiles)
                {
                    var jobId = Path.GetFileNameWithoutExtension(jobFile);
                    var job = await GetBatchPredictionJobAsync(jobId);
                    
                    if (job != null)
                    {
                        result.Add(job);
                    }
                }
                
                // Sort by creation date (newest first)
                result = result
                    .OrderByDescending(j => j.Status.CreatedAt)
                    .ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch prediction jobs with filter {Filter}, skip {Skip}, take {Take}", 
                    filter, skip, take);
                throw;
            }
        }
        
        /// <summary>
        /// Gets active batch prediction jobs (not completed, failed, or cancelled)
        /// </summary>
        public async Task<List<BatchPredictionJob>> GetActiveBatchPredictionJobsAsync()
        {
            try
            {
                var result = new List<BatchPredictionJob>();
                
                // Get all job files
                var jobFiles = Directory.GetFiles(_options.BatchPredictionJobsDirectory, "*.json");
                
                // Load each job
                foreach (var jobFile in jobFiles)
                {
                    var jobId = Path.GetFileNameWithoutExtension(jobFile);
                    var job = await GetBatchPredictionJobAsync(jobId);
                    
                    if (job != null && 
                        job.Status.State != BatchPredictionState.Completed &&
                        job.Status.State != BatchPredictionState.Failed &&
                        job.Status.State != BatchPredictionState.Cancelled)
                    {
                        result.Add(job);
                    }
                }
                
                // Sort by creation date (oldest first)
                result = result
                    .OrderBy(j => j.Status.CreatedAt)
                    .ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active batch prediction jobs");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a batch prediction job from the repository
        /// </summary>
        public Task DeleteBatchPredictionJobAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentException("Job ID is required", nameof(jobId));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.BatchPredictionJobsDirectory, $"{jobId}.json");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Batch prediction job file not found for job {JobId} at {FilePath}", 
                        jobId, filePath);
                    return Task.CompletedTask;
                }
                
                // Delete the file
                File.Delete(filePath);
                
                _logger.LogInformation("Deleted batch prediction job {JobId} from {FilePath}", jobId, filePath);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting batch prediction job {JobId}", jobId);
                throw;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Options for metadata repository
    /// </summary>
    public class MetadataRepositoryOptions
    {
        /// <summary>
        /// Directory for training jobs
        /// </summary>
        public string TrainingJobsDirectory { get; set; } = "data/jobs/training";
        
        /// <summary>
        /// Directory for batch prediction jobs
        /// </summary>
        public string BatchPredictionJobsDirectory { get; set; } = "data/jobs/prediction";
    }
}