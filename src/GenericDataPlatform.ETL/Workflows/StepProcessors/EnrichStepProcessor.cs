using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Enrichers;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class EnrichStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<EnrichStepProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IEnricher> _enrichers;

        public string StepType => WorkflowStepType.Enrich.ToString();

        public EnrichStepProcessor(
            ILogger<EnrichStepProcessor> logger,
            IServiceProvider serviceProvider,
            IEnumerable<IEnricher> enrichers)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _enrichers = new Dictionary<string, IEnricher>();

            foreach (var enricher in enrichers)
            {
                _enrichers[enricher.Type] = enricher;
            }
        }

        public async Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Processing enrich step {StepId}", step.Id);

                // Get enricher type
                if (!step.Configuration.TryGetValue("enricherType", out var enricherTypeObj))
                {
                    throw new ArgumentException($"Enricher type not specified for step {step.Id}");
                }

                var enricherType = enricherTypeObj.ToString();

                // Get enricher
                if (!_enrichers.TryGetValue(enricherType, out var enricher))
                {
                    throw new ArgumentException($"Enricher of type {enricherType} not found");
                }

                // Get input data from dependent steps
                if (!step.DependsOn.Any())
                {
                    throw new ArgumentException($"Enrich step {step.Id} must depend on at least one other step");
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

                // Execute enricher
                var result = await enricher.EnrichAsync(inputData, step.Configuration, source);

                _logger.LogInformation("Enrich step {StepId} completed successfully", step.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enrich step {StepId}", step.Id);
                throw;
            }
        }

        public async Task<bool> ValidateStepConfigurationAsync(WorkflowStep step)
        {
            if (!step.Configuration.TryGetValue("enricherType", out var enricherTypeObj))
            {
                return false;
            }

            var enricherType = enricherTypeObj.ToString();

            if (!_enrichers.ContainsKey(enricherType))
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
