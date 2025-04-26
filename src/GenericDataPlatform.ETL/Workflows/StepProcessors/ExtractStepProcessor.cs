using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Extractors.Base;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class ExtractStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<ExtractStepProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IExtractor> _extractors;
        
        public string StepType => WorkflowStepType.Extract.ToString();
        
        public ExtractStepProcessor(
            ILogger<ExtractStepProcessor> logger,
            IServiceProvider serviceProvider,
            IEnumerable<IExtractor> extractors)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _extractors = new Dictionary<string, IExtractor>();
            
            foreach (var extractor in extractors)
            {
                _extractors[extractor.Type] = extractor;
            }
        }
        
        public async Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Processing extract step {StepId}", step.Id);
                
                // Get extractor type
                if (!step.Configuration.TryGetValue("extractorType", out var extractorTypeObj))
                {
                    throw new ArgumentException($"Extractor type not specified for step {step.Id}");
                }
                
                var extractorType = extractorTypeObj.ToString();
                
                // Get extractor
                if (!_extractors.TryGetValue(extractorType, out var extractor))
                {
                    throw new ArgumentException($"Extractor of type {extractorType} not found");
                }
                
                // Execute extractor
                var result = await extractor.ExtractAsync(step.Configuration, context.Source, context.Parameters);
                
                _logger.LogInformation("Extract step {StepId} completed successfully", step.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing extract step {StepId}", step.Id);
                throw;
            }
        }
        
        public async Task<bool> ValidateStepConfigurationAsync(WorkflowStep step)
        {
            if (!step.Configuration.TryGetValue("extractorType", out var extractorTypeObj))
            {
                return false;
            }
            
            var extractorType = extractorTypeObj.ToString();
            
            return _extractors.ContainsKey(extractorType);
        }
    }
}
