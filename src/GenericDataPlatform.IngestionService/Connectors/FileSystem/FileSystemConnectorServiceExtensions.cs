using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenericDataPlatform.IngestionService.Connectors.FileSystem
{
    /// <summary>
    /// Extension methods for registering file system connector services
    /// </summary>
    public static class FileSystemConnectorServiceExtensions
    {
        /// <summary>
        /// Adds file system connector services to the service collection
        /// </summary>
        public static IServiceCollection AddFileSystemConnectors(this IServiceCollection services, IConfiguration configuration)
        {
            // Register options
            services.Configure<FileSystemConnectorOptions>(
                configuration.GetSection("FileSystemConnector"));
            
            // Register implementations
            services.AddTransient<LocalFileSystemConnector>();
            services.AddTransient<SftpConnector>();
            
            return services;
        }
    }
}
