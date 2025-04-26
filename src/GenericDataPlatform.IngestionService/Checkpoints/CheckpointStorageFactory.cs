using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Checkpoints
{
    /// <summary>
    /// Factory for creating checkpoint storage instances
    /// </summary>
    public class CheckpointStorageFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CheckpointStorageFactory> _logger;

        public CheckpointStorageFactory(IServiceProvider serviceProvider, ILogger<CheckpointStorageFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a checkpoint storage instance based on the specified type
        /// </summary>
        /// <param name="storageType">The type of storage to create</param>
        /// <returns>The checkpoint storage instance</returns>
        public ICheckpointStorage CreateStorage(CheckpointStorageType storageType)
        {
            try
            {
                switch (storageType)
                {
                    case CheckpointStorageType.File:
                        return _serviceProvider.GetRequiredService<FileCheckpointStorage>();
                    
                    case CheckpointStorageType.Database:
                        return _serviceProvider.GetRequiredService<DatabaseCheckpointStorage>();
                    
                    case CheckpointStorageType.Redis:
                        return _serviceProvider.GetRequiredService<RedisCheckpointStorage>();
                    
                    default:
                        _logger.LogWarning("Unknown checkpoint storage type: {StorageType}. Using file storage as fallback.", storageType);
                        return _serviceProvider.GetRequiredService<FileCheckpointStorage>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkpoint storage of type {StorageType}", storageType);
                throw;
            }
        }
    }

    /// <summary>
    /// Types of checkpoint storage
    /// </summary>
    public enum CheckpointStorageType
    {
        /// <summary>
        /// File-based storage
        /// </summary>
        File,
        
        /// <summary>
        /// Database-based storage
        /// </summary>
        Database,
        
        /// <summary>
        /// Redis-based storage
        /// </summary>
        Redis
    }
}
