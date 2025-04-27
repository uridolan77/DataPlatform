using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for orchestrating training jobs
    /// </summary>
    public interface ITrainingOrchestrationService
    {
        /// <summary>
        /// Creates a new training job
        /// </summary>
        /// <param name="modelDefinition">Model definition</param>
        /// <param name="dataSourceId">Data source ID</param>
        /// <param name="dataQuery">Optional data query</param>
        /// <returns>Created training job</returns>
        Task<TrainingJob> CreateTrainingJobAsync(ModelDefinition modelDefinition, string dataSourceId, string? dataQuery = null);

        /// <summary>
        /// Submits a new training job
        /// </summary>
        /// <param name="request">Training job request</param>
        /// <returns>ID of the submitted job</returns>
        Task<string> SubmitTrainingJobAsync(TrainingJobRequest request);

        /// <summary>
        /// Gets the status of a training job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Status of the job, or null if not found</returns>
        Task<TrainingJobStatus?> GetTrainingJobStatusAsync(string jobId);

        /// <summary>
        /// Gets all training jobs
        /// </summary>
        /// <param name="filter">Optional filter</param>
        /// <param name="skip">Number of jobs to skip</param>
        /// <param name="take">Number of jobs to take</param>
        /// <returns>List of training jobs</returns>
        Task<List<TrainingJobStatus>> GetTrainingJobsAsync(string? filter = null, int skip = 0, int take = 20);

        /// <summary>
        /// Gets a training job by ID
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Training job, or null if not found</returns>
        Task<TrainingJob?> GetTrainingJobAsync(string jobId);

        /// <summary>
        /// Lists all training jobs
        /// </summary>
        /// <param name="limit">Maximum number of jobs to return</param>
        /// <param name="offset">Number of jobs to skip</param>
        /// <returns>List of training jobs</returns>
        Task<List<TrainingJob>> ListTrainingJobsAsync(int limit = 100, int offset = 0);

        /// <summary>
        /// Starts a training job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Updated job</returns>
        Task<TrainingJob> StartTrainingJobAsync(string jobId);

        /// <summary>
        /// Cancels a training job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CancelTrainingJobAsync(string jobId);

        /// <summary>
        /// Executes a training job
        /// </summary>
        /// <param name="job">Training job to execute</param>
        /// <returns>Updated job</returns>
        Task<TrainingJob> ExecuteTrainingJobAsync(TrainingJob job);

        /// <summary>
        /// Updates the status of a training job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <param name="state">New state</param>
        /// <param name="progress">Current progress (0-100)</param>
        /// <param name="message">Optional message</param>
        /// <returns>Updated job status, or null if job not found</returns>
        Task<TrainingJobStatus?> UpdateJobStatusAsync(string jobId, TrainingJobState state, int progress = 0, string? message = null);

        /// <summary>
        /// Gets the next job to process from the queue
        /// </summary>
        /// <returns>The next job, or null if the queue is empty</returns>
        Task<TrainingJob?> GetNextJobAsync();
    }
}