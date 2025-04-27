using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Infrastructure
{
    /// <summary>
    /// Repository for ML metadata (jobs, etc.)
    /// </summary>
    public interface IMetadataRepository
    {
        #region Training Jobs
        
        /// <summary>
        /// Saves a training job to the repository
        /// </summary>
        /// <param name="job">Training job to save</param>
        Task SaveTrainingJobAsync(TrainingJob job);
        
        /// <summary>
        /// Gets a training job from the repository
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Training job, or null if not found</returns>
        Task<TrainingJob> GetTrainingJobAsync(string jobId);
        
        /// <summary>
        /// Gets all training jobs from the repository
        /// </summary>
        /// <param name="filter">Optional filter</param>
        /// <param name="skip">Number of jobs to skip</param>
        /// <param name="take">Number of jobs to take</param>
        /// <returns>List of training jobs</returns>
        Task<List<TrainingJob>> GetTrainingJobsAsync(string filter = null, int skip = 0, int take = 20);
        
        /// <summary>
        /// Gets active training jobs (not completed, failed, or cancelled)
        /// </summary>
        /// <returns>List of active training jobs</returns>
        Task<List<TrainingJob>> GetActiveTrainingJobsAsync();
        
        /// <summary>
        /// Deletes a training job from the repository
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        Task DeleteTrainingJobAsync(string jobId);
        
        #endregion
        
        #region Batch Prediction Jobs
        
        /// <summary>
        /// Saves a batch prediction job to the repository
        /// </summary>
        /// <param name="job">Batch prediction job to save</param>
        Task SaveBatchPredictionJobAsync(BatchPredictionJob job);
        
        /// <summary>
        /// Gets a batch prediction job from the repository
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Batch prediction job, or null if not found</returns>
        Task<BatchPredictionJob> GetBatchPredictionJobAsync(string jobId);
        
        /// <summary>
        /// Gets all batch prediction jobs from the repository
        /// </summary>
        /// <param name="filter">Optional filter</param>
        /// <param name="skip">Number of jobs to skip</param>
        /// <param name="take">Number of jobs to take</param>
        /// <returns>List of batch prediction jobs</returns>
        Task<List<BatchPredictionJob>> GetBatchPredictionJobsAsync(string filter = null, int skip = 0, int take = 20);
        
        /// <summary>
        /// Gets active batch prediction jobs (not completed, failed, or cancelled)
        /// </summary>
        /// <returns>List of active batch prediction jobs</returns>
        Task<List<BatchPredictionJob>> GetActiveBatchPredictionJobsAsync();
        
        /// <summary>
        /// Deletes a batch prediction job from the repository
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        Task DeleteBatchPredictionJobAsync(string jobId);
        
        #endregion
    }
}