using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Request for making predictions
    /// </summary>
    public class PredictionRequest
    {
        /// <summary>
        /// Instances to make predictions for
        /// </summary>
        public List<Dictionary<string, object>> Instances { get; set; } = new List<Dictionary<string, object>>();
    }

    /// <summary>
    /// Result of making predictions
    /// </summary>
    public class PredictionResponse
    {
        /// <summary>
        /// Prediction results
        /// </summary>
        public List<Dictionary<string, object>> Predictions { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// Model information used for the predictions
        /// </summary>
        public ModelInfo ModelInfo { get; set; }

        /// <summary>
        /// Timestamp of the predictions
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Model information
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// Name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Version of the model
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Type of the model
        /// </summary>
        public ModelType Type { get; set; }

        /// <summary>
        /// Input schema of the model
        /// </summary>
        public List<FeatureDefinition> InputSchema { get; set; }

        /// <summary>
        /// Output schema of the model
        /// </summary>
        public List<LabelDefinition> OutputSchema { get; set; }

        /// <summary>
        /// Whether the model supports online learning
        /// </summary>
        public bool SupportsOnlineLearning { get; set; }
    }

    /// <summary>
    /// Request for batch predictions
    /// </summary>
    public class BatchPredictionRequest
    {
        /// <summary>
        /// Name of the model to use
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Version of the model to use (null for latest)
        /// </summary>
        public string ModelVersion { get; set; }

        /// <summary>
        /// Location of the input data
        /// </summary>
        public string InputDataLocation { get; set; }

        /// <summary>
        /// Location to store the output data
        /// </summary>
        public string OutputDataLocation { get; set; }

        /// <summary>
        /// Optional parameters for the batch prediction
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Status of a batch prediction job
    /// </summary>
    public class BatchPredictionStatus
    {
        /// <summary>
        /// ID of the batch prediction job
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Current status of the job
        /// </summary>
        public BatchPredictionState State { get; set; }

        /// <summary>
        /// Optional error message if the job failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the job was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the job was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Timestamp when the job was started
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Timestamp when the job was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Name of the model used
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Version of the model used
        /// </summary>
        public string ModelVersion { get; set; }

        /// <summary>
        /// Location of the input data
        /// </summary>
        public string InputDataLocation { get; set; }

        /// <summary>
        /// Location of the output data
        /// </summary>
        public string OutputDataLocation { get; set; }

        /// <summary>
        /// Current progress (0-100)
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Statistics about the batch job
        /// </summary>
        public BatchPredictionStats Stats { get; set; } = new BatchPredictionStats();
    }

    /// <summary>
    /// Statistics about a batch prediction job
    /// </summary>
    public class BatchPredictionStats
    {
        /// <summary>
        /// Total number of records processed
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Number of records successfully processed
        /// </summary>
        public int SuccessfulRecords { get; set; }

        /// <summary>
        /// Number of records that failed processing
        /// </summary>
        public int FailedRecords { get; set; }

        /// <summary>
        /// Average processing time per record (ms)
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// State of a batch prediction job
    /// </summary>
    public enum BatchPredictionState
    {
        /// <summary>
        /// Job is queued
        /// </summary>
        Queued,

        /// <summary>
        /// Job is loading data
        /// </summary>
        LoadingData,

        /// <summary>
        /// Job is processing predictions
        /// </summary>
        Processing,

        /// <summary>
        /// Job is saving results
        /// </summary>
        SavingResults,

        /// <summary>
        /// Job is completed
        /// </summary>
        Completed,

        /// <summary>
        /// Job failed
        /// </summary>
        Failed,

        /// <summary>
        /// Job is cancelled
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Internal representation of a batch prediction job
    /// </summary>
    public class BatchPredictionJob
    {
        /// <summary>
        /// ID of the batch prediction job
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Original request for the job
        /// </summary>
        public BatchPredictionRequest Request { get; set; }

        /// <summary>
        /// Current status of the job
        /// </summary>
        public BatchPredictionStatus Status { get; set; }

        /// <summary>
        /// Data for the job (populated during execution)
        /// </summary>
        public Dictionary<string, object> JobData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new batch prediction job from a request
        /// </summary>
        public static BatchPredictionJob FromRequest(BatchPredictionRequest request)
        {
            var jobId = Guid.NewGuid().ToString();

            return new BatchPredictionJob
            {
                Id = jobId,
                Request = request,
                Status = new BatchPredictionStatus
                {
                    JobId = jobId,
                    State = BatchPredictionState.Queued,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ModelName = request.ModelName,
                    ModelVersion = request.ModelVersion,
                    InputDataLocation = request.InputDataLocation,
                    OutputDataLocation = request.OutputDataLocation
                }
            };
        }
    }
}