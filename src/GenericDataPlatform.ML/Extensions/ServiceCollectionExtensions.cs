using GenericDataPlatform.ML.Services.Core;
using GenericDataPlatform.ML.Services.Infrastructure;
using GenericDataPlatform.ML.Training;
using GenericDataPlatform.ML.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;

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
        public static IServiceCollection AddMLServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register MLContext as singleton
            services.AddSingleton<MLContext>(new MLContext(seed: 42));

            // Register utility services
            services.AddSingleton<IDynamicObjectGenerator, DynamicObjectGenerator>();
            services.AddSingleton<ISchemaConverter, SchemaConverter>();

            // Register training services
            services.AddSingleton<IModelTrainer, ModelTrainer>();
            services.AddSingleton<IModelEvaluator, ModelEvaluator>();
            services.AddSingleton<IFeatureSelectionService, FeatureSelectionService>();
            services.AddSingleton<IHyperparameterTuningService, HyperparameterTuningService>();
            services.AddSingleton<IConceptDriftDetector, ConceptDriftDetector>();

            // Register core services
            services.AddSingleton<IMLService, MLService>();
            services.AddSingleton<ITrainingOrchestrationService, TrainingOrchestrationService>();
            services.AddSingleton<IModelManagementService, ModelManagementService>();
            services.AddSingleton<IPredictionService, PredictionService>();
            services.AddSingleton<IAutoMLService, AutoMLService>();
            services.AddSingleton<IModelExplainerService, ModelExplainerService>();
            services.AddSingleton<IOnlineLearningService, OnlineLearningService>();

            // Register infrastructure services
            services.AddMLFlowIntegration(configuration);
            services.AddSingleton<IModelRepository, ModelRepository>();
            services.AddSingleton<IMetadataRepository, MetadataRepository>();

            // Register background services for job processing
            services.AddHostedService<TrainingJobProcessor>();
            services.AddHostedService<BatchPredictionJobProcessor>();

            return services;
        }

        /// <summary>
        /// Adds MLflow integration to the service collection
        /// </summary>
        public static IServiceCollection AddMLFlowIntegration(this IServiceCollection services, IConfiguration configuration)
        {
            // Register MLflow options
            services.Configure<MLflowOptions>(configuration.GetSection("MLflow"));

            // Register MLflow integration service
            services.AddSingleton<IMLflowIntegrationService, MLflowIntegrationService>();

            return services;
        }

        /// <summary>
        /// Adds ML controllers to the service collection
        /// </summary>
        public static IServiceCollection AddMLControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly)
                .AddControllersAsServices();

            return services;
        }
    }
}