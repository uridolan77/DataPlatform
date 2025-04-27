using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;

namespace GenericDataPlatform.ML.Services.Core
{
    /// <summary>
    /// Service for making predictions with ML models
    /// </summary>
    public interface IPredictionService
    {
        /// <summary>
        /// Makes predictions using a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="modelVersion">Version of the model (null for latest)</param>
        /// <param name="instances">Instances to make predictions for</param>
        /// <returns>Prediction response</returns>
        Task<PredictionResponse> PredictAsync(
            string modelName, 
            string modelVersion, 
            List<Dictionary<string, object>> instances);
        
        /// <summary>
        /// Submits a batch prediction job
        /// </summary>
        /// <param name="request">Batch prediction request</param>
        /// <returns>ID of the submitted job</returns>
        Task<string> SubmitBatchPredictionJobAsync(BatchPredictionRequest request);
        
        /// <summary>
        /// Gets the status of a batch prediction job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Status of the job, or null if not found</returns>
        Task<BatchPredictionStatus> GetBatchPredictionStatusAsync(string jobId);
        
        /// <summary>
        /// Gets all batch prediction jobs
        /// </summary>
        /// <param name="filter">Optional filter</param>
        /// <param name="skip">Number of jobs to skip</param>
        /// <param name="take">Number of jobs to take</param>
        /// <returns>List of batch prediction jobs</returns>
        Task<List<BatchPredictionStatus>> GetBatchPredictionJobsAsync(string filter = null, int skip = 0, int take = 20);
        
        /// <summary>
        /// Cancels a batch prediction job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CancelBatchPredictionJobAsync(string jobId);
        
        /// <summary>
        /// Executes a batch prediction job
        /// </summary>
        /// <param name="job">Batch prediction job to execute</param>
        /// <returns>Updated job</returns>
        Task<BatchPredictionJob> ExecuteBatchPredictionJobAsync(BatchPredictionJob job);
        
        /// <summary>
        /// Gets the next batch prediction job to process from the queue
        /// </summary>
        /// <returns>The next job, or null if the queue is empty</returns>
        Task<BatchPredictionJob> GetNextBatchPredictionJobAsync();
    }
}