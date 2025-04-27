using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Simple
{
    /// <summary>
    /// A simple in-memory implementation of IWorkflowRepository
    /// </summary>
    public class InMemoryWorkflowRepository : IWorkflowRepository
    {
        private readonly ILogger<InMemoryWorkflowRepository> _logger;
        private readonly List<WorkflowDefinition> _workflows;
        private readonly List<WorkflowExecution> _executions;

        public InMemoryWorkflowRepository(ILogger<InMemoryWorkflowRepository> logger)
        {
            _logger = logger;

            // Initialize with some sample data
            _workflows = new List<WorkflowDefinition>
            {
                new WorkflowDefinition
                {
                    Id = "sample-data-pipeline",
                    Name = "Sample Data Pipeline",
                    DisplayName = "Sample Data Pipeline",
                    Description = "A sample data pipeline that extracts data from a REST API, transforms it, and loads it into a database.",
                    Version = "1.0.0",
                    IsPublished = true,
                    IsLatest = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    LastModifiedAt = DateTime.UtcNow.AddDays(-1),
                    Tags = new List<string> { "ETL", "Sample" }
                },
                new WorkflowDefinition
                {
                    Id = "customer-data-pipeline",
                    Name = "Customer Data Pipeline",
                    DisplayName = "Customer Data Pipeline",
                    Description = "A data pipeline that processes customer data.",
                    Version = "1.0.0",
                    IsPublished = true,
                    IsLatest = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    LastModifiedAt = DateTime.UtcNow.AddDays(-2),
                    Tags = new List<string> { "ETL", "Customer" }
                }
            };

            _executions = new List<WorkflowExecution>();

            // Add some sample executions
            for (int i = 0; i < 10; i++)
            {
                _executions.Add(new WorkflowExecution
                {
                    Id = Guid.NewGuid().ToString(),
                    WorkflowId = "sample-data-pipeline",
                    Status = WorkflowStatus.Completed,
                    StartedAt = DateTime.UtcNow.AddDays(-i),
                    CompletedAt = DateTime.UtcNow.AddDays(-i).AddMinutes(1),
                    Output = new Dictionary<string, object>
                    {
                        ["result"] = $"Execution {i + 1} completed successfully"
                    }
                });
            }
        }

        /// <summary>
        /// Gets a list of workflow definitions
        /// </summary>
        public Task<List<WorkflowDefinition>> GetWorkflowsAsync(int skip = 0, int take = 100)
        {
            var workflows = _workflows
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult(workflows);
        }

        /// <summary>
        /// Gets a workflow definition by ID
        /// </summary>
        public Task<WorkflowDefinition> GetWorkflowByIdAsync(string id, string version = null)
        {
            var workflow = _workflows.FirstOrDefault(w => w.Id == id && (version == null || w.Version == version));
            return Task.FromResult(workflow);
        }

        /// <summary>
        /// Gets workflow versions
        /// </summary>
        public Task<List<string>> GetWorkflowVersionsAsync(string id)
        {
            var versions = _workflows
                .Where(w => w.Id == id)
                .Select(w => w.Version)
                .ToList();

            return Task.FromResult(versions);
        }

        /// <summary>
        /// Gets recent workflow executions
        /// </summary>
        public Task<List<WorkflowExecution>> GetRecentExecutionsAsync(int limit = 10)
        {
            var executions = _executions
                .OrderByDescending(e => e.StartedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(executions);
        }

        /// <summary>
        /// Gets a workflow execution by ID
        /// </summary>
        public Task<WorkflowExecution> GetExecutionByIdAsync(string id)
        {
            var execution = _executions.FirstOrDefault(e => e.Id == id);
            return Task.FromResult(execution);
        }

        /// <summary>
        /// Gets workflow execution history
        /// </summary>
        public Task<List<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int limit = 10)
        {
            var executions = _executions
                .Where(e => e.WorkflowId == workflowId)
                .OrderByDescending(e => e.StartedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(executions);
        }

        /// <summary>
        /// Gets execution summaries for a workflow
        /// </summary>
        public Task<List<WorkflowExecutionSummary>> GetExecutionSummariesAsync(string workflowId, int limit = 10)
        {
            var summaries = _executions
                .Where(e => e.WorkflowId == workflowId)
                .OrderByDescending(e => e.StartedAt)
                .Take(limit)
                .Select(e => new WorkflowExecutionSummary
                {
                    Id = e.Id,
                    WorkflowId = e.WorkflowId,
                    Status = e.Status,
                    StartedAt = e.StartedAt,
                    CompletedAt = e.CompletedAt,
                    Duration = e.CompletedAt != default && e.StartedAt != default
                        ? (e.CompletedAt - e.StartedAt).TotalMilliseconds
                        : 0
                })
                .ToList();

            return Task.FromResult(summaries);
        }

        /// <summary>
        /// Saves a workflow definition
        /// </summary>
        public Task<string> SaveWorkflowAsync(WorkflowDefinition workflow)
        {
            var existingWorkflow = _workflows.FirstOrDefault(w => w.Id == workflow.Id && w.Version == workflow.Version);

            if (existingWorkflow != null)
            {
                // Update existing workflow
                var index = _workflows.IndexOf(existingWorkflow);
                _workflows[index] = workflow;
            }
            else
            {
                // Add new workflow
                _workflows.Add(workflow);
            }

            return Task.FromResult(workflow.Id);
        }

        /// <summary>
        /// Saves a workflow execution
        /// </summary>
        public Task<string> SaveExecutionAsync(WorkflowExecution execution)
        {
            var existingExecution = _executions.FirstOrDefault(e => e.Id == execution.Id);

            if (existingExecution != null)
            {
                // Update existing execution
                var index = _executions.IndexOf(existingExecution);
                _executions[index] = execution;
            }
            else
            {
                // Add new execution
                _executions.Add(execution);
            }

            return Task.FromResult(execution.Id);
        }

        /// <summary>
        /// Deletes a workflow definition
        /// </summary>
        public Task<bool> DeleteWorkflowAsync(string id, string version = null)
        {
            var workflowsToRemove = _workflows
                .Where(w => w.Id == id && (version == null || w.Version == version))
                .ToList();

            foreach (var workflow in workflowsToRemove)
            {
                _workflows.Remove(workflow);
            }

            return Task.FromResult(workflowsToRemove.Any());
        }

        /// <summary>
        /// Deletes a workflow execution
        /// </summary>
        public Task<bool> DeleteExecutionAsync(string id)
        {
            var execution = _executions.FirstOrDefault(e => e.Id == id);

            if (execution != null)
            {
                _executions.Remove(execution);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Updates the status of a workflow execution
        /// </summary>
        public Task<bool> UpdateExecutionStatusAsync(string id, WorkflowExecutionStatus status)
        {
            _logger.LogInformation("Updating execution status for {ExecutionId} to {Status}", id, status);

            var execution = _executions.FirstOrDefault(e => e.Id == id);

            if (execution != null)
            {
                execution.Status = status;

                if (status == WorkflowExecutionStatus.Completed ||
                    status == WorkflowExecutionStatus.Failed ||
                    status == WorkflowExecutionStatus.Cancelled)
                {
                    execution.EndTime = DateTime.UtcNow;
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Updates a workflow step execution
        /// </summary>
        public Task<bool> UpdateStepExecutionAsync(string executionId, WorkflowStepExecution stepExecution)
        {
            _logger.LogInformation("Updating step execution for {ExecutionId}, step {StepId}", executionId, stepExecution.StepId);

            var execution = _executions.FirstOrDefault(e => e.Id == executionId);

            if (execution != null)
            {
                var existingStep = execution.StepExecutions.FirstOrDefault(s => s.Id == stepExecution.Id);

                if (existingStep != null)
                {
                    // Update existing step
                    var index = execution.StepExecutions.IndexOf(existingStep);
                    execution.StepExecutions[index] = stepExecution;
                }
                else
                {
                    // Add new step
                    execution.StepExecutions.Add(stepExecution);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets workflow metrics
        /// </summary>
        public Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId)
        {
            _logger.LogInformation("Getting metrics for workflow {WorkflowId}", workflowId);

            var executions = _executions
                .Where(e => e.WorkflowId == workflowId)
                .ToList();

            var metrics = new WorkflowMetrics
            {
                WorkflowId = workflowId,
                TotalExecutions = executions.Count,
                SuccessfulExecutions = executions.Count(e => e.Status == WorkflowExecutionStatus.Completed),
                FailedExecutions = executions.Count(e => e.Status == WorkflowExecutionStatus.Failed)
            };

            if (executions.Any())
            {
                metrics.LastExecutionTime = executions.Max(e => e.StartTime);

                // Calculate average duration for completed executions
                var completedExecutions = executions
                    .Where(e => e.Status == WorkflowExecutionStatus.Completed && e.EndTime != default)
                    .ToList();

                if (completedExecutions.Any())
                {
                    var totalDuration = completedExecutions.Sum(e => (e.EndTime - e.StartTime).TotalMilliseconds);
                    metrics.AverageDuration = totalDuration / completedExecutions.Count;
                }

                // Calculate activity metrics
                var allStepExecutions = executions.SelectMany(e => e.StepExecutions).ToList();
                var stepGroups = allStepExecutions.GroupBy(s => s.StepId);

                foreach (var group in stepGroups)
                {
                    var stepId = group.Key;
                    var steps = group.ToList();
                    var firstStep = steps.First();

                    var activityMetrics = new ActivityMetrics
                    {
                        ActivityName = firstStep.StepName,
                        ExecutionCount = steps.Count,
                        SuccessRate = (double)steps.Count(s => s.Status == WorkflowStepExecutionStatus.Completed) / steps.Count,
                        ErrorRate = (double)steps.Count(s => s.Status == WorkflowStepExecutionStatus.Failed) / steps.Count
                    };

                    // Calculate average duration for completed steps
                    var completedSteps = steps
                        .Where(s => s.Status == WorkflowStepExecutionStatus.Completed && s.EndTime != default)
                        .ToList();

                    if (completedSteps.Any())
                    {
                        var totalDuration = completedSteps.Sum(s => (s.EndTime - s.StartTime).TotalMilliseconds);
                        activityMetrics.AverageDuration = totalDuration / completedSteps.Count;
                    }

                    metrics.ActivityMetrics[stepId] = activityMetrics;
                }
            }

            return Task.FromResult(metrics);
        }
    }
}
