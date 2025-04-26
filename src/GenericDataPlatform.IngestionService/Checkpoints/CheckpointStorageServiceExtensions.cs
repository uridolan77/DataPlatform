using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenericDataPlatform.IngestionService.Checkpoints
{
    /// <summary>
    /// Extension methods for registering checkpoint storage services
    /// </summary>
    public static class CheckpointStorageServiceExtensions
    {
        /// <summary>
        /// Adds checkpoint storage services to the service collection
        /// </summary>
        public static IServiceCollection AddCheckpointStorage(this IServiceCollection services, IConfiguration configuration)
        {
            // Register options
            services.Configure<FileCheckpointStorageOptions>(
                configuration.GetSection("CheckpointStorage:File"));
            
            services.Configure<DatabaseCheckpointStorageOptions>(
                configuration.GetSection("CheckpointStorage:Database"));
            
            services.Configure<RedisCheckpointStorageOptions>(
                configuration.GetSection("CheckpointStorage:Redis"));
            
            // Register implementations
            services.AddSingleton<FileCheckpointStorage>();
            services.AddSingleton<DatabaseCheckpointStorage>();
            services.AddSingleton<RedisCheckpointStorage>();
            
            // Register factory
            services.AddSingleton<CheckpointStorageFactory>();
            
            return services;
        }
    }
}
