using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.ML.Services.Infrastructure
{
    /// <summary>
    /// Service for integrating with MLflow
    /// </summary>
    public class MLflowIntegrationService : IMLflowIntegrationService
    {
        private readonly MLflowOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MLflowIntegrationService> _logger;
        
        public MLflowIntegrationService(
            IOptions<MLflowOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<MLflowIntegrationService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClientFactory?.CreateClient("MLflow") ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_options.TrackingUri);
            
            if (!string.IsNullOrEmpty(_options.AuthToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _options.AuthToken);
            }
        }
        
        /// <summary>
        /// Gets or creates an experiment
        /// </summary>
        public async Task<string> GetOrCreateExperimentAsync(string experimentName)
        {
            try
            {
                _logger.LogInformation("Getting or creating experiment: {ExperimentName}", experimentName);
                
                // Check if experiment exists
                var response = await _httpClient.GetAsync($"/api/2.0/mlflow/experiments/get?experiment_name={experimentName}");
                
                if (response.IsSuccessStatusCode)
                {
                    // Experiment exists, return its ID
                    var content = await response.Content.ReadAsStringAsync();
                    var experimentData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    return experimentData.GetProperty("experiment").GetProperty("experiment_id").GetString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Experiment doesn't exist, create it
                    var requestContent = new
                    {
                        name = experimentName,
                        artifact_location = Path.Combine(_options.ArtifactRoot, experimentName)
                    };
                    
                    var createResponse = await _httpClient.PostAsync(
                        "/api/2.0/mlflow/experiments/create",
                        new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                    
                    createResponse.EnsureSuccessStatusCode();
                    
                    var createContent = await createResponse.Content.ReadAsStringAsync();
                    var createData = JsonSerializer.Deserialize<JsonElement>(createContent);
                    
                    var experimentId = createData.GetProperty("experiment_id").GetString();
                    
                    _logger.LogInformation("Created new experiment {ExperimentName} with ID {ExperimentId}", 
                        experimentName, experimentId);
                    
                    return experimentId;
                }
                else
                {
                    // Other error
                    response.EnsureSuccessStatusCode(); // This will throw if unsuccessful
                    return null; // Will never reach here
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating experiment {ExperimentName}", experimentName);
                throw;
            }
        }
        
        /// <summary>
        /// Creates a new run
        /// </summary>
        public async Task<string> CreateRunAsync(string experimentId, string runName = null)
        {
            try
            {
                _logger.LogInformation("Creating run in experiment {ExperimentId} with name {RunName}", 
                    experimentId, runName ?? "unnamed");
                
                var requestContent = new
                {
                    experiment_id = experimentId,
                    run_name = runName,
                    start_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/runs/create",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var runData = JsonSerializer.Deserialize<JsonElement>(content);
                
                var runId = runData.GetProperty("run").GetProperty("info").GetProperty("run_id").GetString();
                
                _logger.LogInformation("Created new run {RunId} in experiment {ExperimentId}", 
                    runId, experimentId);
                
                return runId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating run in experiment {ExperimentId}", experimentId);
                throw;
            }
        }
        
        /// <summary>
        /// Logs parameters to a run
        /// </summary>
        public async Task LogParametersAsync(string runId, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return;
            }
            
            try
            {
                _logger.LogInformation("Logging {ParameterCount} parameters to run {RunId}", 
                    parameters.Count, runId);
                
                foreach (var param in parameters)
                {
                    var requestContent = new
                    {
                        run_id = runId,
                        key = param.Key,
                        value = param.Value
                    };
                    
                    var response = await _httpClient.PostAsync(
                        "/api/2.0/mlflow/runs/log-parameter",
                        new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                    
                    response.EnsureSuccessStatusCode();
                }
                
                _logger.LogInformation("Logged {ParameterCount} parameters to run {RunId}", 
                    parameters.Count, runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging parameters to run {RunId}", runId);
                throw;
            }
        }
        
        /// <summary>
        /// Logs metrics to a run
        /// </summary>
        public async Task LogMetricsAsync(string runId, Dictionary<string, double> metrics)
        {
            if (metrics == null || metrics.Count == 0)
            {
                return;
            }
            
            try
            {
                _logger.LogInformation("Logging {MetricCount} metrics to run {RunId}", 
                    metrics.Count, runId);
                
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                foreach (var metric in metrics)
                {
                    var requestContent = new
                    {
                        run_id = runId,
                        key = metric.Key,
                        value = metric.Value,
                        timestamp = timestamp,
                        step = 0
                    };
                    
                    var response = await _httpClient.PostAsync(
                        "/api/2.0/mlflow/runs/log-metric",
                        new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                    
                    response.EnsureSuccessStatusCode();
                }
                
                _logger.LogInformation("Logged {MetricCount} metrics to run {RunId}", 
                    metrics.Count, runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging metrics to run {RunId}", runId);
                throw;
            }
        }
        
        /// <summary>
        /// Logs an artifact to a run
        /// </summary>
        public async Task LogArtifactAsync(string runId, string localPath, string artifactPath = null)
        {
            try
            {
                _logger.LogInformation("Logging artifact {LocalPath} to run {RunId}", localPath, runId);
                
                // First, get the artifact URI for the run
                var response = await _httpClient.GetAsync($"/api/2.0/mlflow/runs/get?run_id={runId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var runData = JsonSerializer.Deserialize<JsonElement>(content);
                
                var artifactUri = runData.GetProperty("run").GetProperty("info").GetProperty("artifact_uri").GetString();
                
                // Create the destination path
                var destPath = artifactUri;
                if (!string.IsNullOrEmpty(artifactPath))
                {
                    destPath = Path.Combine(destPath, artifactPath);
                }
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                
                // Copy the file
                var fileName = Path.GetFileName(localPath);
                var destFilePath = Path.Combine(destPath, fileName);
                
                File.Copy(localPath, destFilePath, true);
                
                _logger.LogInformation("Logged artifact {LocalPath} to run {RunId} at {DestPath}", 
                    localPath, runId, destFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging artifact {LocalPath} to run {RunId}", localPath, runId);
                throw;
            }
        }
        
        /// <summary>
        /// Sets a tag on a run
        /// </summary>
        public async Task SetTagAsync(string runId, string key, string value)
        {
            try
            {
                _logger.LogInformation("Setting tag {Key}={Value} on run {RunId}", key, value, runId);
                
                var requestContent = new
                {
                    run_id = runId,
                    key = key,
                    value = value
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/runs/set-tag",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("Set tag {Key}={Value} on run {RunId}", key, value, runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting tag {Key}={Value} on run {RunId}", key, value, runId);
                throw;
            }
        }
        
        /// <summary>
        /// Finishes a run
        /// </summary>
        public async Task FinishRunAsync(string runId, string status = "FINISHED", string endMessage = null)
        {
            try
            {
                _logger.LogInformation("Finishing run {RunId} with status {Status}", runId, status);
                
                var requestContent = new
                {
                    run_id = runId,
                    status = status,
                    end_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/runs/update",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                // If there's an end message, set it as a tag
                if (!string.IsNullOrEmpty(endMessage))
                {
                    await SetTagAsync(runId, "end_message", endMessage);
                }
                
                _logger.LogInformation("Finished run {RunId} with status {Status}", runId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing run {RunId}", runId);
                throw;
            }
        }
        
        /// <summary>
        /// Registers a model
        /// </summary>
        public async Task<string> RegisterModelAsync(string modelName, string modelPath, string runId)
        {
            try
            {
                _logger.LogInformation("Registering model {ModelName} from {ModelPath} for run {RunId}", 
                    modelName, modelPath, runId);
                
                // First, log the model as an artifact
                await LogArtifactAsync(runId, modelPath, "model");
                
                // Then register the model
                var requestContent = new
                {
                    name = modelName,
                    source = $"runs:/{runId}/model",
                    run_id = runId
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/registered-models/create-model-version",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var versionData = JsonSerializer.Deserialize<JsonElement>(content);
                
                var version = versionData.GetProperty("model_version").GetProperty("version").GetString();
                
                _logger.LogInformation("Registered model {ModelName} version {Version} from run {RunId}", 
                    modelName, version, runId);
                
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering model {ModelName} from run {RunId}", modelName, runId);
                throw;
            }
        }
        
        /// <summary>
        /// Gets a registered model
        /// </summary>
        public async Task<object> GetRegisteredModelAsync(string modelName)
        {
            try
            {
                _logger.LogInformation("Getting registered model {ModelName}", modelName);
                
                var response = await _httpClient.GetAsync($"/api/2.0/mlflow/registered-models/get?name={modelName}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var modelData = JsonSerializer.Deserialize<JsonElement>(content);
                
                return modelData.GetProperty("registered_model");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registered model {ModelName}", modelName);
                throw;
            }
        }
        
        /// <summary>
        /// Gets all versions of a model
        /// </summary>
        public async Task<List<object>> GetModelVersionsAsync(string modelName)
        {
            try
            {
                _logger.LogInformation("Getting versions of model {ModelName}", modelName);
                
                var response = await _httpClient.GetAsync($"/api/2.0/mlflow/model-versions/search?name={modelName}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<object>();
                }
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                
                var versions = new List<object>();
                var versionsArray = data.GetProperty("model_versions");
                
                foreach (var version in versionsArray.EnumerateArray())
                {
                    versions.Add(version);
                }
                
                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions of model {ModelName}", modelName);
                throw;
            }
        }
        
        /// <summary>
        /// Transitions a model version to a different stage
        /// </summary>
        public async Task TransitionModelStageAsync(string modelName, string version, string stage)
        {
            try
            {
                _logger.LogInformation("Transitioning model {ModelName} version {Version} to stage {Stage}", 
                    modelName, version, stage);
                
                var requestContent = new
                {
                    name = modelName,
                    version = version,
                    stage = stage,
                    archive_existing_versions = false
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/model-versions/transition-stage",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("Transitioned model {ModelName} version {Version} to stage {Stage}", 
                    modelName, version, stage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning model {ModelName} version {Version} to stage {Stage}", 
                    modelName, version, stage);
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a model version
        /// </summary>
        public async Task DeleteModelVersionAsync(string modelName, string version)
        {
            try
            {
                _logger.LogInformation("Deleting model {ModelName} version {Version}", modelName, version);
                
                var requestContent = new
                {
                    name = modelName,
                    version = version
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/model-versions/delete",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("Deleted model {ModelName} version {Version}", modelName, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model {ModelName} version {Version}", modelName, version);
                throw;
            }
        }
        
        /// <summary>
        /// Sets a tag on a model version
        /// </summary>
        public async Task SetModelTagAsync(string modelName, string version, string key, string value)
        {
            try
            {
                _logger.LogInformation("Setting tag {Key}={Value} on model {ModelName} version {Version}", 
                    key, value, modelName, version);
                
                var requestContent = new
                {
                    name = modelName,
                    version = version,
                    key = key,
                    value = value
                };
                
                var response = await _httpClient.PostAsync(
                    "/api/2.0/mlflow/model-versions/set-tag",
                    new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json"));
                
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("Set tag {Key}={Value} on model {ModelName} version {Version}", 
                    key, value, modelName, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting tag {Key}={Value} on model {ModelName} version {Version}", 
                    key, value, modelName, version);
                throw;
            }
        }
    }
}