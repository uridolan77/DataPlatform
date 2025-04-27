using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Request for a training job
    /// </summary>
    public class TrainingJobRequest
    {
        /// <summary>
        /// Definition of the model to train
        /// </summary>
        public ModelDefinition ModelDefinition { get; set; }
        
        /// <summary>
        /// ID of the data source to use for training
        /// </summary>
        public string DataSourceId { get; set; }
        
        /// <summary>
        /// Optional validation data source ID
        /// </summary>
        public string ValidationDataSourceId { get; set; }
        
        /// <summary>
        /// Optional test data source ID
        /// </summary>
        public string TestDataSourceId { get; set; }
        
        /// <summary>
        /// Optional experiment ID in MLflow
        /// </summary>
        public string ExperimentId { get; set; }
        
        /// <summary>
        /// Optional run name in MLflow
        /// </summary>
        public string RunName { get; set; }
        
        /// <summary>
        /// Optional training parameters
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Status of a training job
    /// </summary>
    public class TrainingJobStatus
    {
        /// <summary>
        /// ID of the training job
        /// </summary>
        public string JobId { get; set; }
        
        /// <summary>
        /// Current status of the job
        /// </summary>
        public TrainingJobState State { get; set; }
        
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
        /// ID of the MLflow run
        /// </summary>
        public string RunId { get; set; }
        
        /// <summary>
        /// ID of the experiment in MLflow
        /// </summary>
        public string ExperimentId { get; set; }
        
        /// <summary>
        /// Definition of the model being trained
        /// </summary>
        public ModelDefinition ModelDefinition { get; set; }
        
        /// <summary>
        /// Metrics from the training
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Optional model ID if training was successful
        /// </summary>
        public string ModelId { get; set; }
        
        /// <summary>
        /// Optional model version if training was successful
        /// </summary>
        public string ModelVersion { get; set; }
        
        /// <summary>
        /// Current progress (0-100)
        /// </summary>
        public int Progress { get; set; }
    }
    
    /// <summary>
    /// State of a training job
    /// </summary>
    public enum TrainingJobState
    {
        /// <summary>
        /// Job is queued
        /// </summary>
        Queued,
        
        /// <summary>
        /// Job is preparing data
        /// </summary>
        PreparingData,
        
        /// <summary>
        /// Job is training
        /// </summary>
        Training,
        
        /// <summary>
        /// Job is evaluating
        /// </summary>
        Evaluating,
        
        /// <summary>
        /// Job is registering model
        /// </summary>
        RegisteringModel,
        
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
    /// Internal representation of a training job
    /// </summary>
    public class TrainingJob
    {
        /// <summary>
        /// ID of the training job
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Original request for the job
        /// </summary>
        public TrainingJobRequest Request { get; set; }
        
        /// <summary>
        /// Current status of the job
        /// </summary>
        public TrainingJobStatus Status { get; set; }
        
        /// <summary>
        /// Data for the job (populated during execution)
        /// </summary>
        public Dictionary<string, object> JobData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a new training job from a request
        /// </summary>
        public static TrainingJob FromRequest(TrainingJobRequest request)
        {
            var jobId = Guid.NewGuid().ToString();
            
            return new TrainingJob
            {
                Id = jobId,
                Request = request,
                Status = new TrainingJobStatus
                {
                    JobId = jobId,
                    State = TrainingJobState.Queued,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ModelDefinition = request.ModelDefinition,
                    ExperimentId = request.ExperimentId
                }
            };
        }
    }
    
    /// <summary>
    /// Request to transition a model to a different stage
    /// </summary>
    public class ModelStageTransitionRequest
    {
        /// <summary>
        /// Version of the model to transition (null for latest)
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Stage to transition to (e.g., "Staging", "Production", "Archived")
        /// </summary>
        public string Stage { get; set; }
        
        /// <summary>
        /// Optional reason for the transition
        /// </summary>
        public string Reason { get; set; }
    }
}