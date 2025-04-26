using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class LoadStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<LoadStepProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ILoader> _loaders;
        
        public string StepType => WorkflowStepType.Load.ToString();
        
        public LoadStepProcessor(
            ILogger<LoadStepProcessor> logger,
            IServiceProvider serviceProvider,
            IEnumerable<ILoader> loaders)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _loaders = new Dictionary<string, ILoader>();
            
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
                
                // Execute loader
                var result = await loader.LoadAsync(inputData, step.Configuration, context.Source);
                
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
    }
}
