using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Activities
{
    [Activity(
        Category = "ETL",
        DisplayName = "Validate Data",
        Description = "Validates data using the specified validator.",
        Outcomes = new[] { OutcomeNames.Done, "Valid", "Invalid", "Error" }
    )]
    public class ValidateActivity : Activity
    {
        private readonly ILogger<ValidateActivity> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ValidateActivity(ILogger<ValidateActivity> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [ActivityInput(
            Label = "Validator Type",
            Hint = "The type of validator to use.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "Schema", "DataQuality" }
        )]
        public string ValidatorType { get; set; } = "Schema";

        [ActivityInput(
            Label = "Input",
            Hint = "The input data to validate.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Input { get; set; }

        [ActivityInput(
            Label = "Configuration",
            Hint = "The configuration for the validator.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Configuration { get; set; }

        [ActivityInput(
            Label = "Data Source",
            Hint = "The data source definition.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public DataSourceDefinition Source { get; set; }

        [ActivityInput(
            Label = "Fail On Invalid",
            Hint = "Whether to fail the workflow if validation fails.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Checkbox
        )]
        public bool FailOnInvalid { get; set; } = false;

        [ActivityOutput]
        public ValidationResult Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Executing Validate activity with validator type: {ValidatorType}", ValidatorType);

                // Get all validators
                var validators = _serviceProvider.GetServices<IValidator>();
                
                // Find the validator with the matching type
                var validator = FindValidator(validators, ValidatorType);
                
                if (validator == null)
                {
                    _logger.LogError("Validator of type {ValidatorType} not found", ValidatorType);
                    return Outcome("Error", new { Error = $"Validator of type {ValidatorType} not found" });
                }

                // Convert configuration to dictionary
                var configDict = ConvertToDictionary(Configuration);

                // Execute the validator
                var result = await validator.ValidateAsync(Input, configDict, Source);
                
                // Store the result
                Output = result;
                
                _logger.LogInformation("Validate activity completed with result: {IsValid}", result.IsValid);
                
                // Return the appropriate outcome
                if (result.IsValid)
                {
                    return Outcome("Valid", result);
                }
                else if (FailOnInvalid)
                {
                    return Outcome("Error", new { Error = "Validation failed", ValidationResult = result });
                }
                else
                {
                    return Outcome("Invalid", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Validate activity");
                return Outcome("Error", new { Error = ex.Message, Exception = ex });
            }
        }

        private IValidator FindValidator(IEnumerable<IValidator> validators, string validatorType)
        {
            foreach (var validator in validators)
            {
                if (string.Equals(validator.Type, validatorType, StringComparison.OrdinalIgnoreCase))
                {
                    return validator;
                }
            }
            
            return null;
        }

        private Dictionary<string, object> ConvertToDictionary(object obj)
        {
            if (obj == null)
            {
                return new Dictionary<string, object>();
            }
            
            if (obj is Dictionary<string, object> dict)
            {
                return dict;
            }
            
            try
            {
                // Try to convert from JSON or other formats
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    System.Text.Json.JsonSerializer.Serialize(obj));
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
    }
}
