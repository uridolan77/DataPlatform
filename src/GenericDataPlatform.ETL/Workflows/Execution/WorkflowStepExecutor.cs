using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Conditions;
using GenericDataPlatform.ETL.Workflows.Exceptions;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Monitoring;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Execution
{
    /// <summary>
    /// Executes workflow steps
    /// </summary>
    public class WorkflowStepExecutor
    {
        private readonly ILogger<WorkflowStepExecutor> _logger;
        private readonly Dictionary<string, IWorkflowStepProcessor> _stepProcessors;
        private readonly IWorkflowRepository _repository;
        private readonly IWorkflowMonitor _monitor;
        private readonly WorkflowConditionEvaluator _conditionEvaluator;
        private readonly WorkflowOptions _options;

        public WorkflowStepExecutor(
            ILogger<WorkflowStepExecutor> logger,
            Dictionary<string, IWorkflowStepProcessor> stepProcessors,
            IWorkflowRepository repository,
            IWorkflowMonitor monitor,
            WorkflowConditionEvaluator conditionEvaluator,
            WorkflowOptions options)
        {
            _logger = logger;
            _stepProcessors = stepProcessors;
            _repository = repository;
            _monitor = monitor;
            _conditionEvaluator = conditionEvaluator;
            _options = options;
        }

        /// <summary>
        /// Executes a workflow step
        /// </summary>
        public async Task ExecuteStepAsync(
            WorkflowStep step,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses)
        {
            // Get step execution record
            var stepExecution = execution.StepExecutions.First(s => s.StepId == step.Id);

            try
            {
                // Check conditions
                if (step.Conditions.Any() && !await _conditionEvaluator.EvaluateConditionsAsync(step.Conditions, context))
                {
                    _logger.LogInformation("Skipping step {StepId} because conditions are not met", step.Id);

                    stepStatuses[step.Id] = WorkflowStepExecutionStatus.Skipped;
                    stepExecution.Status = WorkflowStepExecutionStatus.Skipped;
                    stepExecution.StartTime = DateTime.UtcNow;
                    stepExecution.EndTime = DateTime.UtcNow;

                    return;
                }

                // Update status
                stepStatuses[step.Id] = WorkflowStepExecutionStatus.Running;
                stepExecution.Status = WorkflowStepExecutionStatus.Running;
                stepExecution.StartTime = DateTime.UtcNow;

                // Record step started event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    step.Id,
                    WorkflowTimelineEventTypes.StepStarted,
                    new Dictionary<string, object>
                    {
                        ["stepName"] = step.Name,
                        ["stepType"] = step.Type.ToString()
                    },
                    _logger);

                // Update execution in repository
                await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

                _logger.LogInformation("Executing step {StepId} of type {StepType}", step.Id, step.Type);

                // Get step processor
                if (!_stepProcessors.TryGetValue(step.Type.ToString(), out var processor))
                {
                    throw new InvalidOperationException($"No processor found for step type {step.Type}");
                }

                // Collect input from dependent steps
                var stepInput = new Dictionary<string, object>();
                if (step.DependsOn != null && step.DependsOn.Any())
                {
                    foreach (var dependencyId in step.DependsOn)
                    {
                        if (context.StepOutputs.TryGetValue(dependencyId, out var dependencyOutput))
                        {
                            stepInput[dependencyId] = dependencyOutput;
                        }
                    }
                }

                stepExecution.Input = stepInput;

                // Execute step
                var output = await processor.ProcessStepAsync(step, context);

                // Store output
                context.StepOutputs[step.Id] = output;
                stepExecution.Output = new Dictionary<string, object> { ["result"] = output };

                // Update status
                stepStatuses[step.Id] = WorkflowStepExecutionStatus.Completed;
                stepExecution.Status = WorkflowStepExecutionStatus.Completed;
                stepExecution.EndTime = DateTime.UtcNow;

                // Record step completed event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    step.Id,
                    WorkflowTimelineEventTypes.StepCompleted,
                    new Dictionary<string, object>
                    {
                        ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds,
                        ["retryCount"] = stepExecution.RetryCount
                    },
                    _logger);

                // Update execution in repository
                await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

                _logger.LogInformation("Step {StepId} completed successfully", step.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing step {StepId}", step.Id);

                // Record error
                var error = new WorkflowExecutionError
                {
                    Id = Guid.NewGuid().ToString(),
                    StepId = step.Id,
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                };

                stepExecution.Errors.Add(error);
                execution.Errors.Add(error);

                // Record error event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    step.Id,
                    WorkflowTimelineEventTypes.ErrorOccurred,
                    new Dictionary<string, object>
                    {
                        ["errorType"] = ex.GetType().Name,
                        ["errorMessage"] = ex.Message,
                        ["stackTrace"] = ex.StackTrace ?? string.Empty
                    },
                    _logger);

                // Handle error based on step configuration
                await HandleStepErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
            }
        }

        /// <summary>
        /// Handles a step error
        /// </summary>
        private async Task HandleStepErrorAsync(
            WorkflowStep step,
            WorkflowStepExecution stepExecution,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses,
            Exception ex)
        {
            switch (step.ErrorHandling.OnError)
            {
                case WorkflowErrorAction.StopWorkflow:
                    await HandleStopWorkflowErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
                    break;

                case WorkflowErrorAction.ContinueWorkflow:
                    await HandleContinueWorkflowErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
                    break;

                case WorkflowErrorAction.RetryStep:
                    await HandleRetryStepErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
                    break;

                case WorkflowErrorAction.SkipStep:
                    await HandleSkipStepErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
                    break;

                case WorkflowErrorAction.ExecuteFallback:
                    await HandleExecuteFallbackErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
                    break;

                default:
                    await HandleStopWorkflowErrorAsync(step, stepExecution, execution, context, stepStatuses, ex);
                    break;
            }
        }

        private async Task HandleStopWorkflowErrorAsync(
            WorkflowStep step,
            WorkflowStepExecution stepExecution,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses,
            Exception ex)
        {
            stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
            stepExecution.Status = WorkflowStepExecutionStatus.Failed;
            stepExecution.EndTime = DateTime.UtcNow;

            // Record step failed event
            await _monitor.RecordTimelineEventAsync(
                execution.Id,
                step.Id,
                WorkflowTimelineEventTypes.StepFailed,
                new Dictionary<string, object>
                {
                    ["errorType"] = ex.GetType().Name,
                    ["errorMessage"] = ex.Message,
                    ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds
                },
                _logger);

            // Update execution in repository
            await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

            throw new WorkflowStepFailedException($"Step {step.Id} failed and is configured to stop the workflow", ex);
        }

        private async Task HandleContinueWorkflowErrorAsync(
            WorkflowStep step,
            WorkflowStepExecution stepExecution,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses,
            Exception ex)
        {
            stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
            stepExecution.Status = WorkflowStepExecutionStatus.Failed;
            stepExecution.EndTime = DateTime.UtcNow;

            // Record step failed event
            await _monitor.RecordTimelineEventAsync(
                execution.Id,
                step.Id,
                WorkflowTimelineEventTypes.StepFailed,
                new Dictionary<string, object>
                {
                    ["errorType"] = ex.GetType().Name,
                    ["errorMessage"] = ex.Message,
                    ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds,
                    ["continueWorkflow"] = true
                },
                _logger);

            // Update execution in repository
            await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

            _logger.LogWarning("Step {StepId} failed but workflow will continue", step.Id);
        }

        private async Task HandleRetryStepErrorAsync(
            WorkflowStep step,
            WorkflowStepExecution stepExecution,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses,
            Exception ex)
        {
            if (stepExecution.RetryCount < step.RetryCount)
            {
                stepExecution.RetryCount++;
                _logger.LogInformation("Retrying step {StepId} (attempt {RetryCount} of {MaxRetries})",
                    step.Id, stepExecution.RetryCount, step.RetryCount);

                // Record retry event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    step.Id,
                    WorkflowTimelineEventTypes.StepRetry,
                    new Dictionary<string, object>
                    {
                        ["retryCount"] = stepExecution.RetryCount,
                        ["maxRetries"] = step.RetryCount,
                        ["errorType"] = ex.GetType().Name,
                        ["errorMessage"] = ex.Message
                    },
                    _logger);

                // Update execution in repository
                await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

                // Wait before retrying
                await Task.Delay(step.RetryInterval);

                // Reset status to not started
                stepStatuses[step.Id] = WorkflowStepExecutionStatus.NotStarted;
                stepExecution.Status = WorkflowStepExecutionStatus.NotStarted;
            }
            else
            {
                stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                stepExecution.EndTime = DateTime.UtcNow;

                // Record step failed event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    step.Id,
                    WorkflowTimelineEventTypes.StepFailed,
                    new Dictionary<string, object>
                    {
                        ["errorType"] = ex.GetType().Name,
                        ["errorMessage"] = ex.Message,
                        ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds,
                        ["retryCount"] = stepExecution.RetryCount,
                        ["maxRetries"] = step.RetryCount
                    },
                    _logger);

                // Update execution in repository
                await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

                _logger.LogError("Step {StepId} failed after {RetryCount} retries", step.Id, step.RetryCount);

                if (context.Workflow.ErrorHandling.DefaultAction == WorkflowErrorAction.StopWorkflow)
                {
                    throw new WorkflowStepFailedException($"Step {step.Id} failed after {step.RetryCount} retries", ex);
                }
            }
        }

        private async Task HandleSkipStepErrorAsync(
            WorkflowStep step,
            WorkflowStepExecution stepExecution,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses,
            Exception ex)
        {
            stepStatuses[step.Id] = WorkflowStepExecutionStatus.Skipped;
            stepExecution.Status = WorkflowStepExecutionStatus.Skipped;
            stepExecution.EndTime = DateTime.UtcNow;

            // Record step skipped event
            await _monitor.RecordTimelineEventAsync(
                execution.Id,
                step.Id,
                WorkflowTimelineEventTypes.StepSkipped,
                new Dictionary<string, object>
                {
                    ["reason"] = "Error occurred and step is configured to be skipped",
                    ["errorType"] = ex.GetType().Name,
                    ["errorMessage"] = ex.Message
                },
                _logger);

            // Update execution in repository
            await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

            _logger.LogWarning("Step {StepId} failed and will be skipped", step.Id);
        }

        private async Task HandleExecuteFallbackErrorAsync(
            WorkflowStep step,
            WorkflowStepExecution stepExecution,
            WorkflowExecution execution,
            WorkflowContext context,
            Dictionary<string, WorkflowStepExecutionStatus> stepStatuses,
            Exception ex)
        {
            if (!string.IsNullOrEmpty(step.ErrorHandling.FallbackStepId))
            {
                _logger.LogInformation("Step {StepId} failed, executing fallback step {FallbackStepId}",
                    step.Id, step.ErrorHandling.FallbackStepId);

                // Mark current step as failed
                stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                stepExecution.EndTime = DateTime.UtcNow;

                // Find fallback step
                var fallbackStep = context.Workflow.Steps.FirstOrDefault(s => s.Id == step.ErrorHandling.FallbackStepId);
                if (fallbackStep != null)
                {
                    // Reset fallback step status
                    stepStatuses[fallbackStep.Id] = WorkflowStepExecutionStatus.NotStarted;
                    var fallbackExecution = execution.StepExecutions.First(s => s.StepId == fallbackStep.Id);
                    fallbackExecution.Status = WorkflowStepExecutionStatus.NotStarted;
                }
                else
                {
                    _logger.LogError("Fallback step {FallbackStepId} not found", step.ErrorHandling.FallbackStepId);
                    throw new WorkflowStepFailedException($"Step {step.Id} failed and fallback step {step.ErrorHandling.FallbackStepId} not found", ex);
                }
            }
            else
            {
                _logger.LogError("Step {StepId} failed and no fallback step is configured", step.Id);
                stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                stepExecution.EndTime = DateTime.UtcNow;
                throw new WorkflowStepFailedException($"Step {step.Id} failed and no fallback step is configured", ex);
            }
        }

        /// <summary>
        /// Checks if all dependencies for a step are completed
        /// </summary>
        public bool AreDependenciesCompleted(WorkflowStep step, Dictionary<string, WorkflowStepExecutionStatus> stepStatuses)
        {
            if (step.DependsOn == null || !step.DependsOn.Any())
            {
                return true;
            }

            return step.DependsOn.All(dependencyId =>
                stepStatuses.TryGetValue(dependencyId, out var status) &&
                (status == WorkflowStepExecutionStatus.Completed || status == WorkflowStepExecutionStatus.Skipped));
        }
    }
}
