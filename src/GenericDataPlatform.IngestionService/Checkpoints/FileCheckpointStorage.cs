using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.IngestionService.Checkpoints
{
    /// <summary>
    /// File-based implementation of checkpoint storage
    /// </summary>
    public class FileCheckpointStorage : ICheckpointStorage
    {
        private readonly FileCheckpointStorageOptions _options;
        private readonly ILogger<FileCheckpointStorage> _logger;

        public FileCheckpointStorage(IOptions<FileCheckpointStorageOptions> options, ILogger<FileCheckpointStorage> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Ensure the checkpoint directory exists
            EnsureDirectoryExists();
        }

        public async Task<string> GetValueAsync(string key)
        {
            try
            {
                var filePath = GetFilePath(key);

                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Checkpoint file not found: {FilePath}", filePath);
                    return null;
                }

                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checkpoint for key {Key}", key);
                return null;
            }
        }

        public async Task SetValueAsync(string key, string value)
        {
            try
            {
                var filePath = GetFilePath(key);
                
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the checkpoint to the file
                await File.WriteAllTextAsync(filePath, value);
                
                _logger.LogDebug("Checkpoint saved to file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving checkpoint for key {Key}", key);
                throw;
            }
        }

        private string GetFilePath(string key)
        {
            // Sanitize the key to make it a valid filename
            var sanitizedKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            
            return Path.Combine(_options.CheckpointDirectory, $"{sanitizedKey}.json");
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_options.CheckpointDirectory))
                {
                    Directory.CreateDirectory(_options.CheckpointDirectory);
                    _logger.LogInformation("Created checkpoint directory: {Directory}", _options.CheckpointDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkpoint directory: {Directory}", _options.CheckpointDirectory);
                throw;
            }
        }
    }

    /// <summary>
    /// Options for file-based checkpoint storage
    /// </summary>
    public class FileCheckpointStorageOptions
    {
        /// <summary>
        /// Directory where checkpoint files will be stored
        /// </summary>
        public string CheckpointDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Checkpoints");
    }
}
