using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenericDataPlatform.Common.Configuration
{
    /// <summary>
    /// Extensions for registering configuration services
    /// </summary>
    public static class ConfigurationServiceExtensions
    {
        /// <summary>
        /// Adds the centralized configuration service to the service collection
        /// </summary>
        public static IServiceCollection AddCentralizedConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure options
            services.Configure<ConfigurationOptions>(configuration.GetSection("Configuration"));
            
            // Register configuration service
            services.AddSingleton<IConfigurationService, CentralizedConfigurationService>();
            
            return services;
        }
    }
}
