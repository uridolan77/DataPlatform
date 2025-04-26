using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Services
{
    /// <summary>
    /// Implementation of ETL workflow service
    /// </summary>
    public class EtlWorkflowService : IEtlWorkflowService
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowDefinitionBuilder _workflowDefinitionBuilder;
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly ILogger<EtlWorkflowService> _logger;

        public EtlWorkflowService(
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowRegistry workflowRegistry,
            IWorkflowDefinitionBuilder workflowDefinitionBuilder,
            IWorkflowPublisher workflowPublisher,
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceManager workflowInstanceManager,
            ILogger<EtlWorkflowService> logger)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowRegistry = workflowRegistry;
            _workflowDefinitionBuilder = workflowDefinitionBuilder;
            _workflowPublisher = workflowPublisher;
            _workflowLaunchpad = workflowLaunchpad;
            _workflowInstanceManager = workflowInstanceManager;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new workflow definition
        /// </summary>
        public async Task<string> CreateWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating workflow definition: {WorkflowId}", workflowDefinition.Id);

                // Build the workflow blueprint
                var workflowBlueprint = await _workflowDefinitionBuilder.BuildWorkflowBlueprintAsync(workflowDefinition, cancellationToken);

                // Publish the workflow
                var definition = await _workflowPublisher.PublishAsync(workflowBlueprint, cancellationToken);

                _logger.LogInformation("Workflow definition created: {WorkflowId}", workflowDefinition.Id);

                return definition.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow definition: {WorkflowId}", workflowDefinition.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing workflow definition
        /// </summary>
        public async Task<string> UpdateWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating workflow definition: {WorkflowId}", workflowDefinition.Id);

                // Build the workflow blueprint
                var workflowBlueprint = await _workflowDefinitionBuilder.BuildWorkflowBlueprintAsync(workflowDefinition, cancellationToken);

                // Publish the workflow
                var definition = await _workflowPublisher.PublishAsync(workflowBlueprint, cancellationToken);

                _logger.LogInformation("Workflow definition updated: {WorkflowId}", workflowDefinition.Id);

                return definition.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow definition: {WorkflowId}", workflowDefinition.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets a workflow definition by ID
        /// </summary>
        public async Task<WorkflowDefinition> GetWorkflowDefinitionAsync(string id, string version = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting workflow definition: {WorkflowId}, version: {Version}", id, version);

                // Get the workflow definition
                WorkflowDefinition workflowDefinition = null;
                IWorkflowBlueprint workflowBlueprint = null;

                if (string.IsNullOrEmpty(version))
                {
                    // Get the latest version
                    var specification = new LatestWorkflowDefinitionIdSpecification(id);
                    var definition = await _workflowDefinitionStore.FindAsync(specification, cancellationToken);
                    if (definition != null)
                    {
                        workflowBlueprint = await _workflowRegistry.GetWorkflowBlueprintAsync(definition.DefinitionId, cancellationToken);
                    }
                }
                else
                {
                    // Get the specific version
                    var versionNumber = int.TryParse(version, out var v) ? v : 1;
                    var specification = new WorkflowDefinitionVersionSpecification(id, versionNumber);
                    var definition = await _workflowDefinitionStore.FindAsync(specification, cancellationToken);
                    if (definition != null)
                    {
                        workflowBlueprint = await _workflowRegistry.GetWorkflowBlueprintAsync(definition.DefinitionId, definition.Version, cancellationToken);
                    }
                }

                if (workflowBlueprint != null)
                {
                    workflowDefinition = await _workflowDefinitionBuilder.ConvertToWorkflowDefinitionAsync(workflowBlueprint, cancellationToken);
                }

                _logger.LogInformation("Workflow definition retrieved: {WorkflowId}, version: {Version}", id, version);

                return workflowDefinition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow definition: {WorkflowId}, version: {Version}", id, version);
                throw;
            }
        }

        /// <summary>
        /// Gets all workflow definitions
        /// </summary>
        public async Task<IEnumerable<WorkflowDefinition>> GetWorkflowDefinitionsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting workflow definitions, skip: {Skip}, take: {Take}", skip, take);

                // Get all workflow definitions
                var definitions = await _workflowDefinitionStore.FindManyAsync(
                    new WorkflowDefinitionsPagingSpecification(skip, take),
                    cancellationToken);

                var workflowDefinitions = new List<WorkflowDefinition>();

                foreach (var definition in definitions)
                {
                    var workflowBlueprint = await _workflowRegistry.GetWorkflowBlueprintAsync(definition.DefinitionId, definition.Version, cancellationToken);
                    if (workflowBlueprint != null)
                    {
                        var workflowDefinition = await _workflowDefinitionBuilder.ConvertToWorkflowDefinitionAsync(workflowBlueprint, cancellationToken);
                        workflowDefinitions.Add(workflowDefinition);
                    }
                }

                _logger.LogInformation("Workflow definitions retrieved, count: {Count}", workflowDefinitions.Count);

                return workflowDefinitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow definitions");
                throw;
            }
        }

        /// <summary>
        /// Deletes a workflow definition
        /// </summary>
        public async Task<bool> DeleteWorkflowDefinitionAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting workflow definition: {WorkflowId}", id);

                // Delete the workflow definition
                var specification = new WorkflowDefinitionIdSpecification(id);
                await _workflowDefinitionStore.DeleteAsync(specification, cancellationToken);

                _logger.LogInformation("Workflow definition deleted: {WorkflowId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow definition: {WorkflowId}", id);
                throw;
            }
        }

        /// <summary>
        /// Executes a workflow
        /// </summary>
        public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> input = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Executing workflow: {WorkflowId}", workflowId);

                // Get the workflow blueprint
                var workflowBlueprint = await _workflowRegistry.GetWorkflowBlueprintAsync(workflowId, cancellationToken);
                if (workflowBlueprint == null)
                {
                    throw new Exception($"Workflow with ID {workflowId} not found");
                }

                // Execute the workflow
                var startWorkflowResult = await _workflowLaunchpad.ExecuteWorkflowAsync(
                    workflowBlueprint,
                    input: input,
                    cancellationToken: cancellationToken);

                // Get the workflow instance
                var workflowInstance = startWorkflowResult.WorkflowInstance;

                // Create the execution result
                var executionResult = new WorkflowExecutionResult
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowDefinitionId = workflowInstance.DefinitionId,
                    WorkflowDefinitionVersion = workflowInstance.Version,
                    Status = workflowInstance.WorkflowStatus,
                    CorrelationId = workflowInstance.CorrelationId,
                    Input = workflowInstance.Input,
                    Output = workflowInstance.Output,
                    StartTime = workflowInstance.CreatedAt,
                    EndTime = workflowInstance.FinishedAt,
                    LastExecutedActivity = workflowInstance.LastExecutedActivityId
                };

                _logger.LogInformation("Workflow execution started: {WorkflowId}, instance: {InstanceId}", workflowId, workflowInstance.Id);

                return executionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow: {WorkflowId}", workflowId);
                throw;
            }
        }

        /// <summary>
        /// Gets a workflow execution by ID
        /// </summary>
        public async Task<WorkflowExecutionResult> GetWorkflowExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting workflow execution: {ExecutionId}", executionId);

                // Get the workflow instance
                var workflowInstance = await _workflowInstanceStore.FindAsync(new WorkflowInstanceIdSpecification(executionId), cancellationToken);
                if (workflowInstance == null)
                {
                    throw new Exception($"Workflow instance with ID {executionId} not found");
                }

                // Create the execution result
                var executionResult = new WorkflowExecutionResult
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowDefinitionId = workflowInstance.DefinitionId,
                    WorkflowDefinitionVersion = workflowInstance.Version,
                    Status = workflowInstance.WorkflowStatus,
                    CorrelationId = workflowInstance.CorrelationId,
                    Input = workflowInstance.Input,
                    Output = workflowInstance.Output,
                    StartTime = workflowInstance.CreatedAt,
                    EndTime = workflowInstance.FinishedAt,
                    LastExecutedActivity = workflowInstance.LastExecutedActivityId
                };

                _logger.LogInformation("Workflow execution retrieved: {ExecutionId}", executionId);

                return executionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow execution: {ExecutionId}", executionId);
                throw;
            }
        }

        /// <summary>
        /// Gets workflow execution history
        /// </summary>
        public async Task<IEnumerable<WorkflowExecutionResult>> GetWorkflowExecutionHistoryAsync(string workflowId, int skip = 0, int take = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting workflow execution history: {WorkflowId}, skip: {Skip}, take: {Take}", workflowId, skip, take);

                // Get the workflow instances
                var workflowInstances = await _workflowInstanceStore.FindManyAsync(
                    new WorkflowInstancesForDefinitionSpecification(workflowId, skip, take),
                    cancellationToken);

                // Create the execution results
                var executionResults = workflowInstances.Select(workflowInstance => new WorkflowExecutionResult
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowDefinitionId = workflowInstance.DefinitionId,
                    WorkflowDefinitionVersion = workflowInstance.Version,
                    Status = workflowInstance.WorkflowStatus,
                    CorrelationId = workflowInstance.CorrelationId,
                    Input = workflowInstance.Input,
                    Output = workflowInstance.Output,
                    StartTime = workflowInstance.CreatedAt,
                    EndTime = workflowInstance.FinishedAt,
                    LastExecutedActivity = workflowInstance.LastExecutedActivityId
                }).ToList();

                _logger.LogInformation("Workflow execution history retrieved: {WorkflowId}, count: {Count}", workflowId, executionResults.Count);

                return executionResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow execution history: {WorkflowId}", workflowId);
                throw;
            }
        }

        /// <summary>
        /// Cancels a workflow execution
        /// </summary>
        public async Task<bool> CancelWorkflowExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Cancelling workflow execution: {ExecutionId}", executionId);

                // Cancel the workflow instance
                await _workflowInstanceManager.CancelWorkflowInstanceAsync(executionId, cancellationToken);

                _logger.LogInformation("Workflow execution cancelled: {ExecutionId}", executionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling workflow execution: {ExecutionId}", executionId);
                throw;
            }
        }
    }
}
