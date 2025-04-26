using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Builders;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.ElsaWorkflows.Activities;
using NodaTime;

namespace GenericDataPlatform.ETL.ElsaWorkflows.SampleWorkflows
{
    /// <summary>
    /// A sample data pipeline workflow that demonstrates the ETL capabilities
    /// </summary>
    public class SampleDataPipeline : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Sample Data Pipeline")
                .WithDescription("A sample data pipeline that extracts data from a REST API, transforms it, and loads it into a database.")
                .WithVersion(1)
                .WithPersistenceBehavior(WorkflowPersistenceBehavior.WorkflowBurst)
                .WithDeleteCompletedInstances(false);

            // Extract data from a REST API
            builder
                .StartWith<ExtractActivity>(activity => activity
                    .WithId("Extract")
                    .WithName("Extract Data")
                    .WithDisplayName("Extract Data from REST API")
                    .WithDescription("Extracts data from a sample REST API")
                    .WithExtractorType("Rest")
                    .WithConfiguration(new
                    {
                        url = "https://jsonplaceholder.typicode.com/posts",
                        method = "GET",
                        headers = new
                        {
                            Accept = "application/json"
                        }
                    })
                    .WithSource(new DataSourceDefinition
                    {
                        Id = "sample-rest-api",
                        Name = "Sample REST API",
                        Type = DataSourceType.Rest,
                        ConnectionProperties = new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["baseUrl"] = "https://jsonplaceholder.typicode.com"
                        }
                    }))
                .Then<WriteLine>(activity => activity
                    .WithText(context => $"Extracted {((System.Collections.Generic.IEnumerable<dynamic>)context.GetActivityOutput("Extract"))?.Count() ?? 0} records"))
                
                // Transform the data
                .Then<TransformActivity>(activity => activity
                    .WithId("Transform")
                    .WithName("Transform Data")
                    .WithDisplayName("Transform JSON Data")
                    .WithDescription("Transforms the extracted JSON data")
                    .WithTransformerType("Json")
                    .WithInput(context => context.GetActivityOutput("Extract"))
                    .WithConfiguration(new
                    {
                        transformationType = "map",
                        mappings = new[]
                        {
                            new { source = "id", target = "PostId" },
                            new { source = "userId", target = "UserId" },
                            new { source = "title", target = "Title" },
                            new { source = "body", target = "Content" }
                        }
                    }))
                .Then<WriteLine>(activity => activity
                    .WithText(context => $"Transformed {((System.Collections.Generic.IEnumerable<dynamic>)context.GetActivityOutput("Transform"))?.Count() ?? 0} records"))
                
                // Validate the data
                .Then<ValidateActivity>(activity => activity
                    .WithId("Validate")
                    .WithName("Validate Data")
                    .WithDisplayName("Validate Data")
                    .WithDescription("Validates the transformed data")
                    .WithValidatorType("Schema")
                    .WithInput(context => context.GetActivityOutput("Transform"))
                    .WithConfiguration(new
                    {
                        schema = new
                        {
                            type = "object",
                            required = new[] { "PostId", "UserId", "Title", "Content" },
                            properties = new
                            {
                                PostId = new { type = "integer" },
                                UserId = new { type = "integer" },
                                Title = new { type = "string" },
                                Content = new { type = "string" }
                            }
                        },
                        failOnError = false
                    }))
                
                // Branch based on validation result
                .Then<If>(activity => activity
                    .WithId("ValidationBranch")
                    .WithCondition(context =>
                    {
                        var validationResult = context.GetActivityOutput<ValidationResult>("Validate");
                        return validationResult?.IsValid ?? false;
                    })
                    .When(OutcomeNames.True)
                        // If validation passed, enrich the data
                        .Then<EnrichActivity>(activity => activity
                            .WithId("Enrich")
                            .WithName("Enrich Data")
                            .WithDisplayName("Enrich Data")
                            .WithDescription("Enriches the validated data")
                            .WithEnricherType("Data")
                            .WithInput(context => context.GetActivityOutput("Transform"))
                            .WithConfiguration(new
                            {
                                enrichments = new[]
                                {
                                    new { target = "ProcessedAt", value = DateTime.UtcNow.ToString("o") },
                                    new { target = "Source", value = "Sample Pipeline" }
                                }
                            }))
                        .Then<WriteLine>(activity => activity
                            .WithText(context => $"Enriched {((System.Collections.Generic.IEnumerable<dynamic>)context.GetActivityOutput("Enrich"))?.Count() ?? 0} records"))
                        
                        // Load the data
                        .Then<LoadActivity>(activity => activity
                            .WithId("Load")
                            .WithName("Load Data")
                            .WithDisplayName("Load Data to Database")
                            .WithDescription("Loads the enriched data to a database")
                            .WithLoaderType("Database")
                            .WithInput(context => context.GetActivityOutput("Enrich"))
                            .WithConfiguration(new
                            {
                                databaseServiceUrl = "http://localhost:5000/api/database",
                                tableName = "Posts",
                                batchSize = 100
                            }))
                        .Then<WriteLine>(activity => activity
                            .WithText(context => "Data loaded successfully"))
                    .When(OutcomeNames.False)
                        // If validation failed, log the error
                        .Then<WriteLine>(activity => activity
                            .WithText(context =>
                            {
                                var validationResult = context.GetActivityOutput<ValidationResult>("Validate");
                                return $"Validation failed with {validationResult?.Errors?.Count ?? 0} errors";
                            }));
        }
    }
}
