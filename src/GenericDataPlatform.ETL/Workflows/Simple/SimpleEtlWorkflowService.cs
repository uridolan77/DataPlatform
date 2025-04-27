using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Simple
{
    /// <summary>
    /// A simple implementation of IEtlWorkflowService
    /// </summary>
    public class SimpleEtlWorkflowService : IEtlWorkflowService
    {
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IWorkflowRepository _repository;
        private readonly ILogger<SimpleEtlWorkflowService> _logger;

        public SimpleEtlWorkflowService(
            IWorkflowEngine workflowEngine,
            IWorkflowRepository repository,
            ILogger<SimpleEtlWorkflowService> logger)
        {
            _workflowEngine = workflowEngine;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new ETL workflow
        /// </summary>
        public async Task<string> CreateWorkflowAsync(EtlWorkflowDefinition etlWorkflow)
        {
            _logger.LogInformation("Creating ETL workflow {WorkflowName}", etlWorkflow.Name);

            // Convert to a workflow definition
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
                }
            };

            // Save the workflow definition
            await _repository.SaveWorkflowAsync(workflowDefinition);

            return workflowDefinition.Id;
        }

        /// <summary>
        /// Updates an existing ETL workflow
        /// </summary>
        public async Task<bool> UpdateWorkflowAsync(string workflowId, EtlWorkflowDefinition etlWorkflow)
        {
            _logger.LogInformation("Updating ETL workflow {WorkflowId}", workflowId);

            // Get the existing workflow
            var existingWorkflow = await _repository.GetWorkflowByIdAsync(workflowId);

            if (existingWorkflow == null)
            {
                _logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
                return false;
            }

            // Update the workflow
            existingWorkflow.Name = etlWorkflow.Name;
            existingWorkflow.DisplayName = etlWorkflow.DisplayName;
            existingWorkflow.Description = etlWorkflow.Description;
            existingWorkflow.LastModifiedAt = DateTime.UtcNow;
            existingWorkflow.Tags = etlWorkflow.Tags;
            existingWorkflow.Variables["etlDefinition"] = etlWorkflow;

            // Save the updated workflow
            await _repository.SaveWorkflowAsync(existingWorkflow);

            return true;
        }

        /// <summary>
        /// Gets an ETL workflow by ID
        /// </summary>
        public async Task<EtlWorkflowDefinition> GetWorkflowAsync(string workflowId)
        {
            _logger.LogInformation("Getting ETL workflow {WorkflowId}", workflowId);

            // Get the workflow definition
            var workflowDefinition = await _repository.GetWorkflowByIdAsync(workflowId);

            if (workflowDefinition == null)
            {
                _logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
                return null;
            }

            // Extract the ETL workflow definition from the variables
            if (workflowDefinition.Variables.TryGetValue("etlDefinition", out var etlDefinitionObj) &&
                etlDefinitionObj is EtlWorkflowDefinition etlDefinition)
            {
                return etlDefinition;
            }

            // Create a default ETL workflow definition if not found
            return new EtlWorkflowDefinition
            {
                Name = workflowDefinition.Name,
                DisplayName = workflowDefinition.DisplayName,
                Description = workflowDefinition.Description,
                Tags = workflowDefinition.Tags,
                ExtractSteps = new List<ExtractStepDefinition>(),
                TransformSteps = new List<TransformStepDefinition>(),
                LoadSteps = new List<LoadStepDefinition>()
            };
        }

        /// <summary>
        /// Executes an ETL workflow
        /// </summary>
        public async Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters = null)
        {
            _logger.LogInformation("Executing ETL workflow {WorkflowId}", workflowId);

            // Execute the workflow
            return await _workflowEngine.ExecuteWorkflowAsync(workflowId, parameters);
        }

        /// <summary>
        /// Gets a list of ETL workflows
        /// </summary>
        public async Task<List<EtlWorkflowSummary>> GetWorkflowsAsync(int skip = 0, int take = 100)
        {
            _logger.LogInformation("Getting ETL workflows");

            // Get workflow definitions
            var workflowDefinitions = await _repository.GetWorkflowsAsync(skip, take);

            // Convert to ETL workflow summaries
            var summaries = new List<EtlWorkflowSummary>();

            foreach (var workflow in workflowDefinitions)
            {
                summaries.Add(new EtlWorkflowSummary
                {
                    Id = workflow.Id,
                    Name = workflow.Name,
                    DisplayName = workflow.DisplayName,
                    Description = workflow.Description,
                    Version = workflow.Version,
                    IsPublished = workflow.IsPublished,
                    CreatedAt = workflow.CreatedAt,
                    LastModifiedAt = workflow.LastModifiedAt,
                    Tags = workflow.Tags
                });
            }

            return summaries;
        }

        /// <summary>
        /// Deletes an ETL workflow
        /// </summary>
        public async Task<bool> DeleteWorkflowAsync(string workflowId)
        {
            _logger.LogInformation("Deleting ETL workflow {WorkflowId}", workflowId);

            // Delete the workflow
            return await _repository.DeleteWorkflowAsync(workflowId);
        }
    }
}
