using GenericDataPlatform.ML.Services;
using GenericDataPlatform.ML.Training;
using Microsoft.Extensions.DependencyInjection;

namespace GenericDataPlatform.ML.Extensions
{
    /// <summary>
    /// Extensions for registering ML services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ML services to the service collection
        /// </summary>
        public static IServiceCollection AddMLServices(this IServiceCollection services)
        {
            // Register model trainer
            services.AddSingleton<IModelTrainer, ModelTrainer>();
            
            // Register ML service
            services.AddSingleton<IMLService, MLService>();
            
            return services;
        }
    }
}
