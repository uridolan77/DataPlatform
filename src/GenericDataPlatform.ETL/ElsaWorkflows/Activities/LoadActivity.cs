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
using GenericDataPlatform.ETL.Loaders.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Activities
{
    [Activity(
        Category = "ETL",
        DisplayName = "Load Data",
        Description = "Loads data to a destination using the specified loader.",
        Outcomes = new[] { OutcomeNames.Done, "Error" }
    )]
    public class LoadActivity : Activity
    {
        private readonly ILogger<LoadActivity> _logger;
        private readonly IServiceProvider _serviceProvider;

        public LoadActivity(ILogger<LoadActivity> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [ActivityInput(
            Label = "Loader Type",
            Hint = "The type of loader to use.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "Database", "FileSystem", "Rest" }
        )]
        public string LoaderType { get; set; } = "Database";

        [ActivityInput(
            Label = "Input",
            Hint = "The input data to load.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Input { get; set; }

        [ActivityInput(
            Label = "Configuration",
            Hint = "The configuration for the loader.",
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
                _logger.LogInformation("Executing Load activity with loader type: {LoaderType}", LoaderType);

                // Get all loaders
                var loaders = _serviceProvider.GetServices<ILoader>();
                
                // Find the loader with the matching type
                var loader = FindLoader(loaders, LoaderType);
                
                if (loader == null)
                {
                    _logger.LogError("Loader of type {LoaderType} not found", LoaderType);
                    return Outcome("Error", new { Error = $"Loader of type {LoaderType} not found" });
                }

                // Convert configuration to dictionary
                var configDict = ConvertToDictionary(Configuration);

                // Execute the loader
                var result = await loader.LoadAsync(Input, configDict, Source);
                
                // Store the result
                Output = result;
                
                _logger.LogInformation("Load activity completed successfully");
                
                // Return the result
                return Done(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Load activity");
                return Outcome("Error", new { Error = ex.Message, Exception = ex });
            }
        }

        private ILoader FindLoader(IEnumerable<ILoader> loaders, string loaderType)
        {
            foreach (var loader in loaders)
            {
                if (string.Equals(loader.Type, loaderType, StringComparison.OrdinalIgnoreCase))
                {
                    return loader;
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
