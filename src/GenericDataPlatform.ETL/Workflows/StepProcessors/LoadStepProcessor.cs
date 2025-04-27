using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Tracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class LoadStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<LoadStepProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ILoader> _loaders;
        private readonly ILineageTracker _lineageTracker;

        public string StepType => WorkflowStepType.Load.ToString();

        public LoadStepProcessor(
            ILogger<LoadStepProcessor> logger,
            IServiceProvider serviceProvider,
            IEnumerable<ILoader> loaders,
            ILineageTracker lineageTracker)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _loaders = new Dictionary<string, ILoader>();
            _lineageTracker = lineageTracker;

            foreach (var loader in loaders)
            {
                _loaders[loader.Type] = loader;
            }
        }

        public async Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Processing load step {StepId}", step.Id);

                // Get loader type
                if (!step.Configuration.TryGetValue("loaderType", out var loaderTypeObj))
                {
                    throw new ArgumentException($"Loader type not specified for step {step.Id}");
                }

                var loaderType = loaderTypeObj.ToString();

                // Get loader
                if (!_loaders.TryGetValue(loaderType, out var loader))
                {
                    throw new ArgumentException($"Loader of type {loaderType} not found");
                }

                // Get input data from dependent steps
                if (!step.DependsOn.Any())
                {
                    throw new ArgumentException($"Load step {step.Id} must depend on at least one other step");
                }

                var inputStepId = step.DependsOn.First();
                if (!context.StepOutputs.TryGetValue(inputStepId, out var inputData))
                {
                    throw new ArgumentException($"Input data not found for step {step.Id} from dependent step {inputStepId}");
                }

                // Get target location from configuration
                string targetLocation = GetTargetLocation(step.Configuration, context.Source);

                // Create a DataSourceDefinition if context.Source is a string
                var source = context.Source;
                if (source is string sourceId)
                {
                    source = new DataSourceDefinition { Id = sourceId };
                }

                // Execute loader
                var result = await loader.LoadAsync(inputData, step.Configuration, source);

                // Track lineage
                await _lineageTracker.TrackLoadingAsync(step, context, inputData, targetLocation);

                _logger.LogInformation("Load step {StepId} completed successfully", step.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing load step {StepId}", step.Id);
                throw;
            }
        }

        public async Task<bool> ValidateStepConfigurationAsync(WorkflowStep step)
        {
            if (!step.Configuration.TryGetValue("loaderType", out var loaderTypeObj))
            {
                return false;
            }

            var loaderType = loaderTypeObj.ToString();

            if (!_loaders.ContainsKey(loaderType))
            {
                return false;
            }

            if (!step.DependsOn.Any())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the target location from the step configuration
        /// </summary>
        private string GetTargetLocation(Dictionary<string, object> configuration, string defaultSource)
        {
            // Try to get target from configuration
            if (configuration.TryGetValue("target", out var targetObj) && targetObj != null)
            {
                return targetObj.ToString();
            }

            // Try to get connection string from configuration
            if (configuration.TryGetValue("connectionString", out var connectionStringObj) && connectionStringObj != null)
            {
                return connectionStringObj.ToString();
            }

            // Try to get destination from configuration
            if (configuration.TryGetValue("destination", out var destinationObj) && destinationObj != null)
            {
                return destinationObj.ToString();
            }

            // Try to get table name from configuration
            if (configuration.TryGetValue("tableName", out var tableNameObj) && tableNameObj != null)
            {
                return $"table://{tableNameObj}";
            }

            // Try to get file path from configuration
            if (configuration.TryGetValue("filePath", out var filePathObj) && filePathObj != null)
            {
                return filePathObj.ToString();
            }

            // Use default source if nothing else is available
            return defaultSource ?? "unknown";
        }
    }
}
