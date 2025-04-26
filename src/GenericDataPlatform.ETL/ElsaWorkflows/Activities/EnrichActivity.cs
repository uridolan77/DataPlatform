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
using GenericDataPlatform.ETL.Enrichers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Activities
{
    [Activity(
        Category = "ETL",
        DisplayName = "Enrich Data",
        Description = "Enriches data using the specified enricher.",
        Outcomes = new[] { OutcomeNames.Done, "Error" }
    )]
    public class EnrichActivity : Activity
    {
        private readonly ILogger<EnrichActivity> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EnrichActivity(ILogger<EnrichActivity> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [ActivityInput(
            Label = "Enricher Type",
            Hint = "The type of enricher to use.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "Data", "Lookup" }
        )]
        public string EnricherType { get; set; } = "Data";

        [ActivityInput(
            Label = "Input",
            Hint = "The input data to enrich.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Input { get; set; }

        [ActivityInput(
            Label = "Configuration",
            Hint = "The configuration for the enricher.",
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
                _logger.LogInformation("Executing Enrich activity with enricher type: {EnricherType}", EnricherType);

                // Get all enrichers
                var enrichers = _serviceProvider.GetServices<IEnricher>();
                
                // Find the enricher with the matching type
                var enricher = FindEnricher(enrichers, EnricherType);
                
                if (enricher == null)
                {
                    _logger.LogError("Enricher of type {EnricherType} not found", EnricherType);
                    return Outcome("Error", new { Error = $"Enricher of type {EnricherType} not found" });
                }

                // Convert configuration to dictionary
                var configDict = ConvertToDictionary(Configuration);

                // Execute the enricher
                var result = await enricher.EnrichAsync(Input, configDict, Source);
                
                // Store the result
                Output = result;
                
                _logger.LogInformation("Enrich activity completed successfully");
                
                // Return the result
                return Done(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Enrich activity");
                return Outcome("Error", new { Error = ex.Message, Exception = ex });
            }
        }

        private IEnricher FindEnricher(IEnumerable<IEnricher> enrichers, string enricherType)
        {
            foreach (var enricher in enrichers)
            {
                if (string.Equals(enricher.Type, enricherType, StringComparison.OrdinalIgnoreCase))
                {
                    return enricher;
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
