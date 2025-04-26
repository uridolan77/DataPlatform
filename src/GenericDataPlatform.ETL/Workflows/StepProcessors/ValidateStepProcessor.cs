using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Validators;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class ValidateStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<ValidateStepProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IValidator> _validators;
        
        public string StepType => WorkflowStepType.Validate.ToString();
        
        public ValidateStepProcessor(
            ILogger<ValidateStepProcessor> logger,
            IServiceProvider serviceProvider,
            IEnumerable<IValidator> validators)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _validators = new Dictionary<string, IValidator>();
            
            foreach (var validator in validators)
            {
                _validators[validator.Type] = validator;
            }
        }
        
        public async Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Processing validate step {StepId}", step.Id);
                
                // Get validator type
                if (!step.Configuration.TryGetValue("validatorType", out var validatorTypeObj))
                {
                    throw new ArgumentException($"Validator type not specified for step {step.Id}");
                }
                
                var validatorType = validatorTypeObj.ToString();
                
                // Get validator
                if (!_validators.TryGetValue(validatorType, out var validator))
                {
                    throw new ArgumentException($"Validator of type {validatorType} not found");
                }
                
                // Get input data from dependent steps
                if (!step.DependsOn.Any())
                {
                    throw new ArgumentException($"Validate step {step.Id} must depend on at least one other step");
                }
                
                var inputStepId = step.DependsOn.First();
                if (!context.StepOutputs.TryGetValue(inputStepId, out var inputData))
                {
                    throw new ArgumentException($"Input data not found for step {step.Id} from dependent step {inputStepId}");
                }
                
                // Execute validator
                var result = await validator.ValidateAsync(inputData, step.Configuration, context.Source);
                
                // Check if validation failed and fail-on-error is enabled
                if (!result.IsValid && step.Configuration.TryGetValue("failOnError", out var failOnErrorObj) && 
                    failOnErrorObj is bool failOnError && failOnError)
                {
                    throw new ValidationException($"Validation failed with {result.Errors.Count} errors", result);
                }
                
                _logger.LogInformation("Validate step {StepId} completed with {ErrorCount} errors", 
                    step.Id, result.Errors.Count);
                
                return result;
            }
            catch (ValidationException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing validate step {StepId}", step.Id);
                throw;
            }
        }
        
        public async Task<bool> ValidateStepConfigurationAsync(WorkflowStep step)
        {
            if (!step.Configuration.TryGetValue("validatorType", out var validatorTypeObj))
            {
                return false;
            }
            
            var validatorType = validatorTypeObj.ToString();
            
            if (!_validators.ContainsKey(validatorType))
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
