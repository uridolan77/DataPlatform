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
using GenericDataPlatform.ETL.Extractors.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Activities
{
    [Activity(
        Category = "ETL",
        DisplayName = "Extract Data",
        Description = "Extracts data from a source using the specified extractor.",
        Outcomes = new[] { OutcomeNames.Done, "Error" }
    )]
    public class ExtractActivity : Activity
    {
        private readonly ILogger<ExtractActivity> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ExtractActivity(ILogger<ExtractActivity> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [ActivityInput(
            Label = "Extractor Type",
            Hint = "The type of extractor to use.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "Rest", "Database", "FileSystem" }
        )]
        public string ExtractorType { get; set; } = "Rest";

        [ActivityInput(
            Label = "Configuration",
            Hint = "The configuration for the extractor.",
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
            Label = "Parameters",
            Hint = "Additional parameters for the extraction.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Parameters { get; set; }

        [ActivityOutput]
        public object Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Executing Extract activity with extractor type: {ExtractorType}", ExtractorType);

                // Get all extractors
                var extractors = _serviceProvider.GetServices<IExtractor>();
                
                // Find the extractor with the matching type
                var extractor = FindExtractor(extractors, ExtractorType);
                
                if (extractor == null)
                {
                    _logger.LogError("Extractor of type {ExtractorType} not found", ExtractorType);
                    return Outcome("Error", new { Error = $"Extractor of type {ExtractorType} not found" });
                }

                // Convert configuration to dictionary
                var configDict = ConvertToDictionary(Configuration);
                
                // Convert parameters to dictionary
                var paramsDict = ConvertToDictionary(Parameters);

                // Execute the extractor
                var result = await extractor.ExtractAsync(configDict, Source, paramsDict);
                
                // Store the result
                Output = result;
                
                _logger.LogInformation("Extract activity completed successfully");
                
                // Return the result
                return Done(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Extract activity");
                return Outcome("Error", new { Error = ex.Message, Exception = ex });
            }
        }

        private IExtractor FindExtractor(IEnumerable<IExtractor> extractors, string extractorType)
        {
            foreach (var extractor in extractors)
            {
                if (string.Equals(extractor.Type, extractorType, StringComparison.OrdinalIgnoreCase))
                {
                    return extractor;
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
