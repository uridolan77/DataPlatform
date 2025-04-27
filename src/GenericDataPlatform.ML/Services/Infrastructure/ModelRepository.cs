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
    /// Repository for ML models
    /// </summary>
    public class ModelRepository : IModelRepository
    {
        private readonly ILogger<ModelRepository> _logger;
        private readonly ModelRepositoryOptions _options;
        
        public ModelRepository(
            IOptions<ModelRepositoryOptions> options,
            ILogger<ModelRepository> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Create repositories directories if they don't exist
            Directory.CreateDirectory(_options.MetadataDirectory);
            Directory.CreateDirectory(_options.ModelsDirectory);
        }
        
        /// <summary>
        /// Saves model metadata to the repository
        /// </summary>
        public async Task SaveModelMetadataAsync(ModelMetadata metadata)
        {
            try
            {
                if (metadata == null)
                {
                    throw new ArgumentNullException(nameof(metadata));
                }
                
                // Ensure directory exists
                var modelDir = Path.Combine(_options.MetadataDirectory, metadata.Name);
                Directory.CreateDirectory(modelDir);
                
                // Create file path
                var filePath = Path.Combine(modelDir, $"{metadata.Version}.json");
                
                // Serialize metadata
                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json);
                
                // Update latest version reference
                await UpdateLatestVersionAsync(metadata.Name, metadata.Version);
                
                _logger.LogInformation("Saved metadata for model {ModelName} version {Version} to {FilePath}", 
                    metadata.Name, metadata.Version, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving metadata for model {ModelName} version {Version}", 
                    metadata?.Name, metadata?.Version);
                throw;
            }
        }
        
        /// <summary>
        /// Gets model metadata from the repository
        /// </summary>
        public async Task<ModelMetadata> GetModelMetadataAsync(string modelName, string version = null)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(modelName))
                {
                    throw new ArgumentException("Model name is required", nameof(modelName));
                }
                
                // Get the version to load
                string versionToLoad = version;
                if (string.IsNullOrEmpty(versionToLoad))
                {
                    // Load latest version
                    versionToLoad = await GetLatestVersionAsync(modelName);
                    if (string.IsNullOrEmpty(versionToLoad))
                    {
                        _logger.LogWarning("No versions found for model {ModelName}", modelName);
                        return null;
                    }
                }
                
                // Create file path
                var filePath = Path.Combine(_options.MetadataDirectory, modelName, $"{versionToLoad}.json");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Metadata file not found for model {ModelName} version {Version} at {FilePath}", 
                        modelName, versionToLoad, filePath);
                    return null;
                }
                
                // Read and deserialize the file
                var json = await File.ReadAllTextAsync(filePath);
                var metadata = JsonSerializer.Deserialize<ModelMetadata>(json);
                
                _logger.LogInformation("Loaded metadata for model {ModelName} version {Version} from {FilePath}", 
                    modelName, versionToLoad, filePath);
                
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for model {ModelName} version {Version}", 
                    modelName, version ?? "latest");
                throw;
            }
        }
        
        /// <summary>
        /// Lists model metadata from the repository
        /// </summary>
        public async Task<List<ModelMetadata>> ListModelMetadataAsync(string filter = null, int skip = 0, int take = 20)
        {
            try
            {
                var result = new List<ModelMetadata>();
                
                // Get all model directories
                var modelDirs = Directory.GetDirectories(_options.MetadataDirectory);
                
                // Apply filter if specified
                if (!string.IsNullOrEmpty(filter))
                {
                    modelDirs = modelDirs.Where(d => Path.GetFileName(d).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                
                // Skip and take
                modelDirs = modelDirs.Skip(skip).Take(take).ToArray();
                
                // Load latest version for each model
                foreach (var modelDir in modelDirs)
                {
                    var modelName = Path.GetFileName(modelDir);
                    var metadata = await GetModelMetadataAsync(modelName);
                    
                    if (metadata != null)
                    {
                        result.Add(metadata);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing model metadata with filter {Filter}, skip {Skip}, take {Take}", 
                    filter, skip, take);
                throw;
            }
        }
        
        /// <summary>
        /// Gets all versions of a model
        /// </summary>
        public async Task<List<ModelMetadata>> GetModelVersionsAsync(string modelName)
        {
            try
            {
                var result = new List<ModelMetadata>();
                
                // Validate inputs
                if (string.IsNullOrEmpty(modelName))
                {
                    throw new ArgumentException("Model name is required", nameof(modelName));
                }
                
                // Get directory for the model
                var modelDir = Path.Combine(_options.MetadataDirectory, modelName);
                
                // Check if directory exists
                if (!Directory.Exists(modelDir))
                {
                    _logger.LogWarning("Directory not found for model {ModelName} at {ModelDir}", 
                        modelName, modelDir);
                    return result;
                }
                
                // Get all version files
                var versionFiles = Directory.GetFiles(modelDir, "*.json");
                
                // Load metadata for each version
                foreach (var versionFile in versionFiles)
                {
                    // Skip latest.json
                    if (Path.GetFileName(versionFile) == "latest.json")
                    {
                        continue;
                    }
                    
                    // Parse version
                    var version = Path.GetFileNameWithoutExtension(versionFile);
                    
                    // Load metadata
                    var metadata = await GetModelMetadataAsync(modelName, version);
                    
                    if (metadata != null)
                    {
                        result.Add(metadata);
                    }
                }
                
                // Sort by version
                result = result
                    .OrderByDescending(m => int.TryParse(m.Version, out var v) ? v : 0)
                    .ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions for model {ModelName}", modelName);
                throw;
            }
        }
        
        /// <summary>
        /// Gets models by stage
        /// </summary>
        public async Task<List<ModelMetadata>> GetModelsByStageAsync(string modelName, string stage)
        {
            try
            {
                // Get all versions of the model
                var versions = await GetModelVersionsAsync(modelName);
                
                // Filter by stage
                var result = versions.Where(v => v.Stage == stage).ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models {ModelName} in stage {Stage}", modelName, stage);
                throw;
            }
        }
        
        /// <summary>
        /// Deletes model metadata from the repository
        /// </summary>
        public async Task DeleteModelMetadataAsync(string modelName, string version)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(modelName))
                {
                    throw new ArgumentException("Model name is required", nameof(modelName));
                }
                
                if (string.IsNullOrEmpty(version))
                {
                    throw new ArgumentException("Version is required", nameof(version));
                }
                
                // Create file path
                var filePath = Path.Combine(_options.MetadataDirectory, modelName, $"{version}.json");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Metadata file not found for model {ModelName} version {Version} at {FilePath}", 
                        modelName, version, filePath);
                    return;
                }
                
                // Delete the file
                File.Delete(filePath);
                
                // Check if this was the latest version
                var latestVersion = await GetLatestVersionAsync(modelName);
                if (latestVersion == version)
                {
                    // Update latest version to the next available version
                    var versions = await GetModelVersionsAsync(modelName);
                    if (versions.Count > 0)
                    {
                        await UpdateLatestVersionAsync(modelName, versions[0].Version);
                    }
                    else
                    {
                        // No more versions, delete the latest file
                        var latestFilePath = Path.Combine(_options.MetadataDirectory, modelName, "latest.json");
                        if (File.Exists(latestFilePath))
                        {
                            File.Delete(latestFilePath);
                        }
                    }
                }
                
                _logger.LogInformation("Deleted metadata for model {ModelName} version {Version}", 
                    modelName, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting metadata for model {ModelName} version {Version}", 
                    modelName, version);
                throw;
            }
        }
        
        /// <summary>
        /// Saves a model to the repository
        /// </summary>
        public async Task SaveModelBytesAsync(string modelPath, byte[] modelBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    throw new ArgumentException("Model path is required", nameof(modelPath));
                }
                
                if (modelBytes == null || modelBytes.Length == 0)
                {
                    throw new ArgumentException("Model bytes are required", nameof(modelBytes));
                }
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write model bytes to file
                await File.WriteAllBytesAsync(modelPath, modelBytes);
                
                _logger.LogInformation("Saved model bytes to {ModelPath} ({Size} bytes)", 
                    modelPath, modelBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving model bytes to {ModelPath}", modelPath);
                throw;
            }
        }
        
        /// <summary>
        /// Loads a model from the repository
        /// </summary>
        public async Task<byte[]> LoadModelBytesAsync(string modelPath)
        {
            try
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    throw new ArgumentException("Model path is required", nameof(modelPath));
                }
                
                // Check if file exists
                if (!File.Exists(modelPath))
                {
                    _logger.LogWarning("Model file not found at {ModelPath}", modelPath);
                    return null;
                }
                
                // Read model bytes from file
                var modelBytes = await File.ReadAllBytesAsync(modelPath);
                
                _logger.LogInformation("Loaded model bytes from {ModelPath} ({Size} bytes)", 
                    modelPath, modelBytes.Length);
                
                return modelBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model bytes from {ModelPath}", modelPath);
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a model from the repository
        /// </summary>
        public Task DeleteModelBytesAsync(string modelPath)
        {
            try
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    throw new ArgumentException("Model path is required", nameof(modelPath));
                }
                
                // Check if file exists
                if (!File.Exists(modelPath))
                {
                    _logger.LogWarning("Model file not found at {ModelPath}", modelPath);
                    return Task.CompletedTask;
                }
                
                // Delete the file
                File.Delete(modelPath);
                
                _logger.LogInformation("Deleted model file at {ModelPath}", modelPath);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model file at {ModelPath}", modelPath);
                throw;
            }
        }
        
        #region Private Methods
        
        private async Task<string> GetLatestVersionAsync(string modelName)
        {
            // Create path to latest.json
            var latestFilePath = Path.Combine(_options.MetadataDirectory, modelName, "latest.json");
            
            // Check if file exists
            if (!File.Exists(latestFilePath))
            {
                _logger.LogWarning("Latest version file not found for model {ModelName} at {FilePath}", 
                    modelName, latestFilePath);
                return null;
            }
            
            // Read and deserialize the file
            try
            {
                var json = await File.ReadAllTextAsync(latestFilePath);
                var latestInfo = JsonSerializer.Deserialize<LatestVersionInfo>(json);
                
                return latestInfo.Version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading latest version file for model {ModelName}", modelName);
                return null;
            }
        }
        
        private async Task UpdateLatestVersionAsync(string modelName, string version)
        {
            // Create path to latest.json
            var latestFilePath = Path.Combine(_options.MetadataDirectory, modelName, "latest.json");
            
            // Create latest version info
            var latestInfo = new LatestVersionInfo
            {
                Version = version,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Serialize and write to file
            try
            {
                var json = JsonSerializer.Serialize(latestInfo, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(latestFilePath, json);
                
                _logger.LogInformation("Updated latest version for model {ModelName} to {Version}", 
                    modelName, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating latest version file for model {ModelName}", modelName);
                throw;
            }
        }
        
        #endregion
        
        private class LatestVersionInfo
        {
            public string Version { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
    
    /// <summary>
    /// Options for model repository
    /// </summary>
    public class ModelRepositoryOptions
    {
        /// <summary>
        /// Directory for model metadata
        /// </summary>
        public string MetadataDirectory { get; set; } = "data/models/metadata";
        
        /// <summary>
        /// Directory for model files
        /// </summary>
        public string ModelsDirectory { get; set; } = "data/models/files";
    }
}