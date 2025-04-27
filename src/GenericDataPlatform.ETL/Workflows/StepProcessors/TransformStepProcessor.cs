using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Transformers.Base;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Tracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class TransformStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<TransformStepProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ITransformer> _transformers;
        private readonly ILineageTracker _lineageTracker;

        public string StepType => WorkflowStepType.Transform.ToString();

        public TransformStepProcessor(
            ILogger<TransformStepProcessor> logger,
            IServiceProvider serviceProvider,
            IEnumerable<ITransformer> transformers,
            ILineageTracker lineageTracker)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _transformers = new Dictionary<string, ITransformer>();
            _lineageTracker = lineageTracker;

            foreach (var transformer in transformers)
            {
                _transformers[transformer.Type] = transformer;
            }
        }

        public async Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Processing transform step {StepId}", step.Id);

                // Get transformer type
                if (!step.Configuration.TryGetValue("transformerType", out var transformerTypeObj))
                {
                    throw new ArgumentException($"Transformer type not specified for step {step.Id}");
                }

                var transformerType = transformerTypeObj.ToString();

                // Get transformer
                if (!_transformers.TryGetValue(transformerType, out var transformer))
                {
                    throw new ArgumentException($"Transformer of type {transformerType} not found");
                }

                // Get input data from dependent steps
                if (!step.DependsOn.Any())
                {
                    throw new ArgumentException($"Transform step {step.Id} must depend on at least one other step");
                }

                var inputStepId = step.DependsOn.First();
                if (!context.StepOutputs.TryGetValue(inputStepId, out var inputData))
                {
                    throw new ArgumentException($"Input data not found for step {step.Id} from dependent step {inputStepId}");
                }

                // Create a DataSourceDefinition if context.Source is a string
                var source = context.Source;
                if (source is string sourceId)
                {
                    source = new DataSourceDefinition { Id = sourceId };
                }

                // Execute transformer
                var result = await transformer.TransformAsync(inputData, step.Configuration, source);

                // Track lineage
                await _lineageTracker.TrackTransformationAsync(step, context, inputData, result);

                _logger.LogInformation("Transform step {StepId} completed successfully", step.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transform step {StepId}", step.Id);
                throw;
            }
        }

        public async Task<bool> ValidateStepConfigurationAsync(WorkflowStep step)
        {
            if (!step.Configuration.TryGetValue("transformerType", out var transformerTypeObj))
            {
                return false;
            }

            var transformerType = transformerTypeObj.ToString();

            if (!_transformers.ContainsKey(transformerType))
            {
                return false;
            }

            if (!step.DependsOn.Any())
            {
                return false;
            }

            return true;
        }
    }
}
