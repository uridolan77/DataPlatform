using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericDataPlatform.ML.Services.Infrastructure
{
    /// <summary>
    /// Service for integrating with MLflow
    /// </summary>
    public interface IMLflowIntegrationService
    {
        /// <summary>
        /// Gets or creates an experiment
        /// </summary>
        /// <param name="experimentName">Name of the experiment</param>
        /// <returns>ID of the experiment</returns>
        Task<string> GetOrCreateExperimentAsync(string experimentName);
        
        /// <summary>
        /// Creates a new run
        /// </summary>
        /// <param name="experimentId">ID of the experiment</param>
        /// <param name="runName">Name of the run</param>
        /// <returns>ID of the run</returns>
        Task<string> CreateRunAsync(string experimentId, string runName = null);
        
        /// <summary>
        /// Logs parameters to a run
        /// </summary>
        /// <param name="runId">ID of the run</param>
        /// <param name="parameters">Parameters to log</param>
        Task LogParametersAsync(string runId, Dictionary<string, string> parameters);
        
        /// <summary>
        /// Logs metrics to a run
        /// </summary>
        /// <param name="runId">ID of the run</param>
        /// <param name="metrics">Metrics to log</param>
        Task LogMetricsAsync(string runId, Dictionary<string, double> metrics);
        
        /// <summary>
        /// Logs an artifact to a run
        /// </summary>
        /// <param name="runId">ID of the run</param>
        /// <param name="localPath">Local path to the artifact</param>
        /// <param name="artifactPath">Optional artifact path</param>
        Task LogArtifactAsync(string runId, string localPath, string artifactPath = null);
        
        /// <summary>
        /// Sets a tag on a run
        /// </summary>
        /// <param name="runId">ID of the run</param>
        /// <param name="key">Tag key</param>
        /// <param name="value">Tag value</param>
        Task SetTagAsync(string runId, string key, string value);
        
        /// <summary>
        /// Finishes a run
        /// </summary>
        /// <param name="runId">ID of the run</param>
        /// <param name="status">Status of the run (e.g., "FINISHED", "FAILED", "KILLED")</param>
        /// <param name="endTime">Optional end time (milliseconds since epoch)</param>
        Task FinishRunAsync(string runId, string status = "FINISHED", string endMessage = null);
        
        /// <summary>
        /// Registers a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="modelPath">Path to the model file</param>
        /// <param name="runId">ID of the run that created the model</param>
        /// <returns>Version of the registered model</returns>
        Task<string> RegisterModelAsync(string modelName, string modelPath, string runId);
        
        /// <summary>
        /// Gets a registered model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>Registered model</returns>
        Task<object> GetRegisteredModelAsync(string modelName);
        
        /// <summary>
        /// Gets all versions of a model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>List of model versions</returns>
        Task<List<object>> GetModelVersionsAsync(string modelName);
        
        /// <summary>
        /// Transitions a model version to a different stage
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model</param>
        /// <param name="stage">Stage to transition to</param>
        Task TransitionModelStageAsync(string modelName, string version, string stage);
        
        /// <summary>
        /// Deletes a model version
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model</param>
        Task DeleteModelVersionAsync(string modelName, string version);
        
        /// <summary>
        /// Sets a tag on a model version
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model</param>
        /// <param name="key">Tag key</param>
        /// <param name="value">Tag value</param>
        Task SetModelTagAsync(string modelName, string version, string key, string value);
    }
    
    /// <summary>
    /// Options for MLflow integration
    /// </summary>
    public class MLflowOptions
    {
        /// <summary>
        /// MLflow tracking URI
        /// </summary>
        public string TrackingUri { get; set; } = "http://localhost:5000";
        
        /// <summary>
        /// Artifact root location
        /// </summary>
        public string ArtifactRoot { get; set; } = "artifacts";
        
        /// <summary>
        /// Default experiment name
        /// </summary>
        public string DefaultExperiment { get; set; } = "Default";
        
        /// <summary>
        /// Authentication token (if needed)
        /// </summary>
        public string AuthToken { get; set; }
    }
}