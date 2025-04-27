using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Simple
{
    /// <summary>
    /// A simple implementation of IWorkflowDefinitionBuilder
    /// </summary>
    public class SimpleWorkflowDefinitionBuilder : IWorkflowDefinitionBuilder
    {
        private readonly ILogger<SimpleWorkflowDefinitionBuilder> _logger;
        
        public SimpleWorkflowDefinitionBuilder(ILogger<SimpleWorkflowDefinitionBuilder> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Builds a workflow definition from an ETL workflow definition
        /// </summary>
        public Task<WorkflowDefinition> BuildWorkflowDefinitionAsync(EtlWorkflowDefinition etlWorkflow)
        {
            _logger.LogInformation("Building workflow definition for ETL workflow {WorkflowName}", etlWorkflow.Name);
            
            // Create a workflow definition
            var workflowDefinition = new WorkflowDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = etlWorkflow.Name,
                DisplayName = etlWorkflow.DisplayName,
                Description = etlWorkflow.Description,
                Version = "1.0.0",
                IsPublished = true,
                IsLatest = true,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Tags = etlWorkflow.Tags,
                Variables = new Dictionary<string, object>
                {
                    ["etlDefinition"] = etlWorkflow
                },
                Activities = BuildActivities(etlWorkflow),
                Connections = BuildConnections(etlWorkflow)
            };
            
            return Task.FromResult(workflowDefinition);
        }
        
        /// <summary>
        /// Builds activities from an ETL workflow definition
        /// </summary>
        private List<ActivityDefinition> BuildActivities(EtlWorkflowDefinition etlWorkflow)
        {
            var activities = new List<ActivityDefinition>();
            
            // Add extract activities
            foreach (var extractStep in etlWorkflow.ExtractSteps)
            {
                activities.Add(new ActivityDefinition
                {
                    Id = extractStep.Id,
                    Type = "Extract",
                    Name = extractStep.Name,
                    DisplayName = extractStep.DisplayName,
                    Description = extractStep.Description,
                    Properties = new Dictionary<string, object>
                    {
                        ["extractorType"] = extractStep.ExtractorType,
                        ["source"] = extractStep.Source,
                        ["configuration"] = extractStep.Configuration
                    }
                });
            }
            
            // Add transform activities
            foreach (var transformStep in etlWorkflow.TransformSteps)
            {
                activities.Add(new ActivityDefinition
                {
                    Id = transformStep.Id,
                    Type = "Transform",
                    Name = transformStep.Name,
                    DisplayName = transformStep.DisplayName,
                    Description = transformStep.Description,
                    Properties = new Dictionary<string, object>
                    {
                        ["transformerType"] = transformStep.TransformerType,
                        ["configuration"] = transformStep.Configuration
                    }
                });
            }
            
            // Add load activities
            foreach (var loadStep in etlWorkflow.LoadSteps)
            {
                activities.Add(new ActivityDefinition
                {
                    Id = loadStep.Id,
                    Type = "Load",
                    Name = loadStep.Name,
                    DisplayName = loadStep.DisplayName,
                    Description = loadStep.Description,
                    Properties = new Dictionary<string, object>
                    {
                        ["loaderType"] = loadStep.LoaderType,
                        ["destination"] = loadStep.Destination,
                        ["configuration"] = loadStep.Configuration
                    }
                });
            }
            
            return activities;
        }
        
        /// <summary>
        /// Builds connections from an ETL workflow definition
        /// </summary>
        private List<ConnectionDefinition> BuildConnections(EtlWorkflowDefinition etlWorkflow)
        {
            var connections = new List<ConnectionDefinition>();
            
            // Connect extract to transform steps
            for (int i = 0; i < etlWorkflow.ExtractSteps.Count; i++)
            {
                var extractStep = etlWorkflow.ExtractSteps[i];
                
                if (i < etlWorkflow.TransformSteps.Count)
                {
                    var transformStep = etlWorkflow.TransformSteps[i];
                    
                    connections.Add(new ConnectionDefinition
                    {
                        SourceActivityId = extractStep.Id,
                        TargetActivityId = transformStep.Id,
                        Outcome = "Done"
                    });
                }
            }
            
            // Connect transform to load steps
            for (int i = 0; i < etlWorkflow.TransformSteps.Count; i++)
            {
                var transformStep = etlWorkflow.TransformSteps[i];
                
                if (i < etlWorkflow.LoadSteps.Count)
                {
                    var loadStep = etlWorkflow.LoadSteps[i];
                    
                    connections.Add(new ConnectionDefinition
                    {
                        SourceActivityId = transformStep.Id,
                        TargetActivityId = loadStep.Id,
                        Outcome = "Done"
                    });
                }
            }
            
            return connections;
        }
    }
}
