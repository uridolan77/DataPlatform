using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows
{
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly ILogger<WorkflowEngine> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IWorkflowStepProcessor> _stepProcessors;
        private readonly Dictionary<string, WorkflowExecution> _executions = new Dictionary<string, WorkflowExecution>();
        private readonly Dictionary<string, CancellationTokenSource> _executionCancellationTokens = new Dictionary<string, CancellationTokenSource>();

        public WorkflowEngine(
            ILogger<WorkflowEngine> logger,
            IServiceProvider serviceProvider,
            IEnumerable<IWorkflowStepProcessor> stepProcessors)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _stepProcessors = stepProcessors.ToDictionary(p => p.StepType, p => p);
        }

        public async Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters = null)
        {
            // In a real implementation, this would load the workflow from a repository
            throw new NotImplementedException("Loading workflows by ID is not implemented");
        }

        public async Task<WorkflowExecution> ExecuteWorkflowAsync(WorkflowDefinition workflow, Dictionary<string, object> parameters = null)
        {
            if (workflow == null)
            {
                throw new ArgumentNullException(nameof(workflow));
            }

            // Create execution record
            var execution = new WorkflowExecution
            {
                Id = Guid.NewGuid().ToString(),
                WorkflowId = workflow.Id,
                Status = WorkflowExecutionStatus.Running,
                StartTime = DateTime.UtcNow,
                Parameters = parameters ?? new Dictionary<string, object>(),
                StepExecutions = new List<WorkflowStepExecution>(),
                Errors = new List<WorkflowExecutionError>(),
                TriggerType = "Manual"
            };

            // Create cancellation token
            var cts = new CancellationTokenSource();
            _executionCancellationTokens[execution.Id] = cts;

            // Store execution
            _executions[execution.Id] = execution;

            // Create workflow context
            var context = new WorkflowContext
            {
                ExecutionId = execution.Id,
                Workflow = workflow,
                Parameters = execution.Parameters,
                Variables = new Dictionary<string, object>(),
                StepOutputs = new Dictionary<string, object>(),
                CancellationToken = cts.Token
            };

            // Execute workflow asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteWorkflowInternalAsync(execution, context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflow.Id);
                    
                    execution.Status = WorkflowExecutionStatus.Failed;
                    execution.EndTime = DateTime.UtcNow;
                    execution.Errors.Add(new WorkflowExecutionError
                    {
                        Id = Guid.NewGuid().ToString(),
                        ErrorType = ex.GetType().Name,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        Timestamp = DateTime.UtcNow
                    });
                }
                finally
                {
                    // Clean up
                    _executionCancellationTokens.Remove(execution.Id);
                    cts.Dispose();
                }
            });

            return execution;
        }

        public async Task<WorkflowExecution> GetExecutionStatusAsync(string executionId)
        {
            if (_executions.TryGetValue(executionId, out var execution))
            {
                return execution;
            }

            // In a real implementation, this would load the execution from a repository
            throw new KeyNotFoundException($"Execution {executionId} not found");
        }

        public async Task<bool> CancelExecutionAsync(string executionId)
        {
            if (_executionCancellationTokens.TryGetValue(executionId, out var cts))
            {
                cts.Cancel();
                
                if (_executions.TryGetValue(executionId, out var execution))
                {
                    execution.Status = WorkflowExecutionStatus.Cancelled;
                    execution.EndTime = DateTime.UtcNow;
                }
                
                return true;
            }
            
            return false;
        }

        public async Task<bool> PauseExecutionAsync(string executionId)
        {
            // Pausing is not implemented in this version
            return false;
        }

        public async Task<bool> ResumeExecutionAsync(string executionId)
        {
            // Resuming is not implemented in this version
            return false;
        }

        public async Task<List<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int limit = 10)
        {
            // In a real implementation, this would load the execution history from a repository
            return _executions.Values
                .Where(e => e.WorkflowId == workflowId)
                .OrderByDescending(e => e.StartTime)
                .Take(limit)
                .ToList();
        }

        private async Task ExecuteWorkflowInternalAsync(WorkflowExecution execution, WorkflowContext context)
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

                // Execute steps in order, respecting dependencies
                bool progress;
                do
                {
                    progress = false;
                    
                    // Check for cancellation
                    context.CancellationToken.ThrowIfCancellationRequested();
                    
                    // Find steps that can be executed
                    var executableSteps = context.Workflow.Steps
                        .Where(s => stepStatuses[s.Id] == WorkflowStepExecutionStatus.NotStarted && 
                                   AreDependenciesCompleted(s, stepStatuses))
                        .ToList();
                    
                    if (executableSteps.Any())
                    {
                        progress = true;
                        
                        // Execute each step
                        foreach (var step in executableSteps)
                        {
                            await ExecuteStepAsync(step, execution, context, stepStatuses);
                        }
                    }
                    
                    // Check if there are any running steps
                    if (!progress && stepStatuses.Values.Any(s => s == WorkflowStepExecutionStatus.Running))
                    {
                        // Wait for running steps to complete
                        await Task.Delay(100);
                        progress = true;
                    }
                }
                while (progress && !context.CancellationToken.IsCancellationRequested);

                // Check if all steps are completed
                if (stepStatuses.Values.All(s => s == WorkflowStepExecutionStatus.Completed || s == WorkflowStepExecutionStatus.Skipped))
                {
                    execution.Status = WorkflowExecutionStatus.Completed;
                }
                else if (context.CancellationToken.IsCancellationRequested)
                {
                    execution.Status = WorkflowExecutionStatus.Cancelled;
                }
                else
                {
                    execution.Status = WorkflowExecutionStatus.Failed;
                }
                
                execution.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation("Workflow execution {ExecutionId} completed with status {Status}", 
                    execution.Id, execution.Status);
            }
            catch (OperationCanceledException)
            {
                execution.Status = WorkflowExecutionStatus.Cancelled;
                execution.EndTime = DateTime.UtcNow;
                
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
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", execution.WorkflowId);
            }
        }

        private async Task ExecuteStepAsync(
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
                if (step.Conditions.Any() && !await EvaluateConditionsAsync(step.Conditions, context))
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
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                };
                
                stepExecution.Errors.Add(error);
                execution.Errors.Add(error);
                
                // Handle error based on step configuration
                switch (step.ErrorHandling.OnError)
                {
                    case WorkflowErrorAction.StopWorkflow:
                        stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                        stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                        stepExecution.EndTime = DateTime.UtcNow;
                        throw new WorkflowStepFailedException($"Step {step.Id} failed and is configured to stop the workflow", ex);
                    
                    case WorkflowErrorAction.ContinueWorkflow:
                        stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                        stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                        stepExecution.EndTime = DateTime.UtcNow;
                        _logger.LogWarning("Step {StepId} failed but workflow will continue", step.Id);
                        break;
                    
                    case WorkflowErrorAction.RetryStep:
                        if (stepExecution.RetryCount < step.RetryCount)
                        {
                            stepExecution.RetryCount++;
                            _logger.LogInformation("Retrying step {StepId} (attempt {RetryCount} of {MaxRetries})", 
                                step.Id, stepExecution.RetryCount, step.RetryCount);
                            
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
                            _logger.LogError("Step {StepId} failed after {RetryCount} retries", step.Id, step.RetryCount);
                            
                            if (context.Workflow.ErrorHandling.DefaultAction == WorkflowErrorAction.StopWorkflow)
                            {
                                throw new WorkflowStepFailedException($"Step {step.Id} failed after {step.RetryCount} retries", ex);
                            }
                        }
                        break;
                    
                    case WorkflowErrorAction.SkipStep:
                        stepStatuses[step.Id] = WorkflowStepExecutionStatus.Skipped;
                        stepExecution.Status = WorkflowStepExecutionStatus.Skipped;
                        stepExecution.EndTime = DateTime.UtcNow;
                        _logger.LogWarning("Step {StepId} failed and will be skipped", step.Id);
                        break;
                    
                    case WorkflowErrorAction.ExecuteFallback:
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
                        break;
                }
            }
        }

        private bool AreDependenciesCompleted(WorkflowStep step, Dictionary<string, WorkflowStepExecutionStatus> stepStatuses)
        {
            if (step.DependsOn == null || !step.DependsOn.Any())
            {
                return true;
            }
            
            return step.DependsOn.All(dependencyId => 
                stepStatuses.TryGetValue(dependencyId, out var status) && 
                (status == WorkflowStepExecutionStatus.Completed || status == WorkflowStepExecutionStatus.Skipped));
        }

        private async Task<bool> EvaluateConditionsAsync(List<WorkflowCondition> conditions, WorkflowContext context)
        {
            if (conditions == null || !conditions.Any())
            {
                return true;
            }
            
            foreach (var condition in conditions)
            {
                if (!await EvaluateConditionAsync(condition, context))
                {
                    return false;
                }
            }
            
            return true;
        }

        private async Task<bool> EvaluateConditionAsync(WorkflowCondition condition, WorkflowContext context)
        {
            switch (condition.Type)
            {
                case WorkflowConditionType.Expression:
                    return EvaluateExpression(condition.Expression, context);
                
                case WorkflowConditionType.Script:
                case WorkflowConditionType.DataBased:
                case WorkflowConditionType.External:
                    _logger.LogWarning("Condition type {ConditionType} is not implemented", condition.Type);
                    return true;
                
                default:
                    return true;
            }
        }

        private bool EvaluateExpression(string expression, WorkflowContext context)
        {
            // In a real implementation, this would evaluate the expression
            // For this example, we'll implement a simple expression evaluator
            
            if (string.IsNullOrEmpty(expression))
            {
                return true;
            }
            
            // Check for variable references
            if (expression.Contains("$"))
            {
                // Replace variable references with their values
                foreach (var variable in context.Variables)
                {
                    expression = expression.Replace($"${variable.Key}", variable.Value?.ToString() ?? "null");
                }
                
                // Replace parameter references
                foreach (var parameter in context.Parameters)
                {
                    expression = expression.Replace($"$params.{parameter.Key}", parameter.Value?.ToString() ?? "null");
                }
                
                // Replace step output references
                foreach (var output in context.StepOutputs)
                {
                    expression = expression.Replace($"$steps.{output.Key}", "true");
                }
            }
            
            // Simple equality check
            if (expression.Contains("=="))
            {
                var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();
                    
                    return left == right;
                }
            }
            
            // Simple inequality check
            if (expression.Contains("!="))
            {
                var parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();
                    
                    return left != right;
                }
            }
            
            // Simple boolean check
            if (bool.TryParse(expression, out var boolResult))
            {
                return boolResult;
            }
            
            // Default to true for unrecognized expressions
            _logger.LogWarning("Could not evaluate expression: {Expression}", expression);
            return true;
        }
    }

    public class WorkflowStepFailedException : Exception
    {
        public WorkflowStepFailedException(string message) : base(message)
        {
        }
        
        public WorkflowStepFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
