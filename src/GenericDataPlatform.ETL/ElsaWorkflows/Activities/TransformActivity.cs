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
using GenericDataPlatform.ETL.Transformers.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Activities
{
    [Activity(
        Category = "ETL",
        DisplayName = "Transform Data",
        Description = "Transforms data using the specified transformer.",
        Outcomes = new[] { OutcomeNames.Done, "Error" }
    )]
    public class TransformActivity : Activity
    {
        private readonly ILogger<TransformActivity> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TransformActivity(ILogger<TransformActivity> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [ActivityInput(
            Label = "Transformer Type",
            Hint = "The type of transformer to use.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "Json", "Csv", "Xml" }
        )]
        public string TransformerType { get; set; } = "Json";

        [ActivityInput(
            Label = "Input",
            Hint = "The input data to transform.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Input { get; set; }

        [ActivityInput(
            Label = "Configuration",
            Hint = "The configuration for the transformer.",
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

        [ActivityOutput]
        public object Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Executing Transform activity with transformer type: {TransformerType}", TransformerType);

                // Get all transformers
                var transformers = _serviceProvider.GetServices<ITransformer>();
                
                // Find the transformer with the matching type
                var transformer = FindTransformer(transformers, TransformerType);
                
                if (transformer == null)
                {
                    _logger.LogError("Transformer of type {TransformerType} not found", TransformerType);
                    return Outcome("Error", new { Error = $"Transformer of type {TransformerType} not found" });
                }

                // Convert configuration to dictionary
                var configDict = ConvertToDictionary(Configuration);

                // Execute the transformer
                var result = await transformer.TransformAsync(Input, configDict, Source);
                
                // Store the result
                Output = result;
                
                _logger.LogInformation("Transform activity completed successfully");
                
                // Return the result
                return Done(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Transform activity");
                return Outcome("Error", new { Error = ex.Message, Exception = ex });
            }
        }

        private ITransformer FindTransformer(IEnumerable<ITransformer> transformers, string transformerType)
        {
            foreach (var transformer in transformers)
            {
                if (string.Equals(transformer.Type, transformerType, StringComparison.OrdinalIgnoreCase))
                {
                    return transformer;
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
