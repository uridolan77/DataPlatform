using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Exceptions;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Monitoring;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Execution
{
    /// <summary>
    /// Executes workflows
    /// </summary>
    public class WorkflowExecutor
    {
        private readonly ILogger<WorkflowExecutor> _logger;
        private readonly IWorkflowRepository _repository;
        private readonly IWorkflowMonitor _monitor;
        private readonly WorkflowStepExecutor _stepExecutor;
        private readonly WorkflowOptions _options;

        public WorkflowExecutor(
            ILogger<WorkflowExecutor> logger,
            IWorkflowRepository repository,
            IWorkflowMonitor monitor,
            WorkflowStepExecutor stepExecutor,
            WorkflowOptions options)
        {
            _logger = logger;
            _repository = repository;
            _monitor = monitor;
            _stepExecutor = stepExecutor;
            _options = options;
        }

        /// <summary>
        /// Executes a workflow
        /// </summary>
        public async Task ExecuteWorkflowInternalAsync(WorkflowExecution execution, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Starting workflow execution {ExecutionId} for workflow {WorkflowId}",
                    execution.Id, execution.WorkflowId);

                // Initialize step statuses
                var stepStatuses = new Dictionary<string, WorkflowStepExecutionStatus>();
                foreach (var step in context.Workflow.Steps)
                {
                    stepStatuses[step.Id] = WorkflowStepExecutionStatus.NotStarted;

                    // Create step execution record
                    var stepExecution = new WorkflowStepExecution
                    {
                        Id = Guid.NewGuid().ToString(),
                        StepId = step.Id,
                        Status = WorkflowStepExecutionStatus.NotStarted,
                        StartTime = DateTime.MinValue,
                        Errors = new List<WorkflowExecutionError>()
                    };

                    execution.StepExecutions.Add(stepExecution);
                }

                // Save initial execution state
                await _repository.SaveExecutionAsync(execution);

                // Execute steps in order, respecting dependencies
                bool progress;
                do
                {
                    progress = false;

                    // Check for cancellation
                    context.CancellationToken.ThrowIfCancellationRequested();

                    // Check if execution is paused
                    if (execution.Status == WorkflowExecutionStatus.Paused)
                    {
                        // Wait for resume
                        await Task.Delay(1000, context.CancellationToken);
                        progress = true;
                        continue;
                    }

                    // Find steps that can be executed
                    var executableSteps = context.Workflow.Steps
                        .Where(s => stepStatuses[s.Id] == WorkflowStepExecutionStatus.NotStarted &&
                                   _stepExecutor.AreDependenciesCompleted(s, stepStatuses))
                        .ToList();

                    if (executableSteps.Any())
                    {
                        progress = true;

                        // Execute each step
                        foreach (var step in executableSteps)
                        {
                            await _stepExecutor.ExecuteStepAsync(step, execution, context, stepStatuses);

                            // Update execution in repository after each step
                            await _repository.SaveExecutionAsync(execution);
                        }
                    }

                    // Check if there are any running steps
                    if (!progress && stepStatuses.Values.Any(s => s == WorkflowStepExecutionStatus.Running))
                    {
                        // Wait for running steps to complete
                        await Task.Delay(100, context.CancellationToken);
                        progress = true;
                    }
                }
                while (progress && !context.CancellationToken.IsCancellationRequested);

                // Check if all steps are completed
                if (stepStatuses.Values.All(s => s == WorkflowStepExecutionStatus.Completed || s == WorkflowStepExecutionStatus.Skipped))
                {
                    execution.Status = WorkflowExecutionStatus.Completed;

                    // Record workflow completed event
                    await _monitor.RecordTimelineEventAsync(
                        execution.Id,
                        null,
                        WorkflowTimelineEventTypes.WorkflowCompleted,
                        new Dictionary<string, object>
                        {
                            ["completedSteps"] = stepStatuses.Count(s => s.Value == WorkflowStepExecutionStatus.Completed),
                            ["skippedSteps"] = stepStatuses.Count(s => s.Value == WorkflowStepExecutionStatus.Skipped)
                        },
                        _logger);

                    // Send notification if enabled
                    if (_options.EnableNotifications)
                    {
                        await WorkflowMonitoringExtensions.SendNotificationAsync(
                            _options.NotificationServiceUrl,
                            execution,
                            "Workflow Completed",
                            $"Workflow {context.Workflow.Name} completed successfully",
                            _logger);
                    }
                }
                else if (context.CancellationToken.IsCancellationRequested)
                {
                    execution.Status = WorkflowExecutionStatus.Cancelled;

                    // Record workflow cancelled event
                    await _monitor.RecordTimelineEventAsync(
                        execution.Id,
                        null,
                        WorkflowTimelineEventTypes.WorkflowCancelled,
                        new Dictionary<string, object>
                        {
                            ["cancelledBy"] = "System",
                            ["reason"] = "Cancellation requested"
                        },
                        _logger);

                    // Send notification if enabled
                    if (_options.EnableNotifications)
                    {
                        await WorkflowMonitoringExtensions.SendNotificationAsync(
                            _options.NotificationServiceUrl,
                            execution,
                            "Workflow Cancelled",
                            $"Workflow {context.Workflow.Name} was cancelled",
                            _logger);
                    }
                }
                else
                {
                    execution.Status = WorkflowExecutionStatus.Failed;

                    // Record workflow failed event
                    await _monitor.RecordTimelineEventAsync(
                        execution.Id,
                        null,
                        WorkflowTimelineEventTypes.WorkflowFailed,
                        new Dictionary<string, object>
                        {
                            ["reason"] = "Not all steps completed"
                        },
                        _logger);

                    // Send notification if enabled
                    if (_options.EnableNotifications)
                    {
                        await WorkflowMonitoringExtensions.SendNotificationAsync(
                            _options.NotificationServiceUrl,
                            execution,
                            "Workflow Failed",
                            $"Workflow {context.Workflow.Name} failed: Not all steps completed",
                            _logger);
                    }
                }

                execution.EndTime = DateTime.UtcNow;

                // Update execution in repository
                await _repository.SaveExecutionAsync(execution);

                // Update workflow metrics
                if (_options.EnableMetricsCollection && _monitor != null)
                {
                    await _monitor.UpdateWorkflowMetricsAsync(execution);
                }

                _logger.LogInformation("Workflow execution {ExecutionId} completed with status {Status}",
                    execution.Id, execution.Status);
            }
            catch (OperationCanceledException)
            {
                execution.Status = WorkflowExecutionStatus.Cancelled;
                execution.EndTime = DateTime.UtcNow;

                // Record workflow cancelled event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    null,
                    WorkflowTimelineEventTypes.WorkflowCancelled,
                    new Dictionary<string, object>
                    {
                        ["cancelledBy"] = "System",
                        ["reason"] = "Operation cancelled"
                    },
                    _logger);

                // Update execution in repository
                await _repository.SaveExecutionAsync(execution);

                // Send notification if enabled
                if (_options.EnableNotifications)
                {
                    await WorkflowMonitoringExtensions.SendNotificationAsync(
                        _options.NotificationServiceUrl,
                        execution,
                        "Workflow Cancelled",
                        $"Workflow {context.Workflow.Name} was cancelled",
                        _logger);
                }

                _logger.LogInformation("Workflow execution {ExecutionId} was cancelled", execution.Id);
            }
            catch (Exception ex)
            {
                execution.Status = WorkflowExecutionStatus.Failed;
                execution.EndTime = DateTime.UtcNow;
                execution.Errors.Add(new WorkflowExecutionError
                {
                    Id = Guid.NewGuid().ToString(),
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                });

                // Record workflow failed event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    null,
                    WorkflowTimelineEventTypes.WorkflowFailed,
                    new Dictionary<string, object>
                    {
                        ["errorType"] = ex.GetType().Name,
                        ["errorMessage"] = ex.Message
                    },
                    _logger);

                // Update execution in repository
                await _repository.SaveExecutionAsync(execution);

                // Send notification if enabled
                if (_options.EnableNotifications)
                {
                    await WorkflowMonitoringExtensions.SendNotificationAsync(
                        _options.NotificationServiceUrl,
                        execution,
                        "Workflow Failed",
                        $"Workflow {context.Workflow.Name} failed: {ex.Message}",
                        _logger);
                }

                _logger.LogError(ex, "Error executing workflow {WorkflowId}", execution.WorkflowId);
            }
        }
    }
}
