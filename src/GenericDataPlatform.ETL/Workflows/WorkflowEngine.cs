using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.ETL.Workflows
{
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly ILogger<WorkflowEngine> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IWorkflowStepProcessor> _stepProcessors;
        private readonly IWorkflowRepository _repository;
        private readonly WorkflowOptions _options;
        private readonly Dictionary<string, CancellationTokenSource> _executionCancellationTokens = new Dictionary<string, CancellationTokenSource>();
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly IWorkflowMonitor _monitor;

        public WorkflowEngine(
            ILogger<WorkflowEngine> logger,
            IServiceProvider serviceProvider,
            IEnumerable<IWorkflowStepProcessor> stepProcessors,
            IWorkflowRepository repository,
            IOptions<WorkflowOptions> options,
            IWorkflowMonitor monitor = null)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _stepProcessors = stepProcessors.ToDictionary(p => p.StepType, p => p);
            _repository = repository;
            _options = options.Value;
            _monitor = monitor;
            _executionSemaphore = new SemaphoreSlim(_options.MaxConcurrentExecutions, _options.MaxConcurrentExecutions);

            // Initialize sample workflow if needed
            _ = InitializeSampleWorkflowAsync();
        }

        private async Task InitializeSampleWorkflowAsync()
        {
            try
            {
                // Check if sample workflow exists
                var sampleWorkflow = await _repository.GetWorkflowByIdAsync("sample-workflow");

                if (sampleWorkflow == null)
                {
                    // Create a sample workflow
                    sampleWorkflow = new WorkflowDefinition
                    {
                        Id = "sample-workflow",
                        Name = "Sample Workflow",
                        Description = "A sample workflow for demonstration purposes",
                        Version = "1.0",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Steps = new List<WorkflowStep>
                        {
                            new WorkflowStep
                            {
                                Id = "extract-step",
                                Name = "Extract Data",
                                Description = "Extract data from a REST API",
                                Type = WorkflowStepType.Extract,
                                Configuration = new Dictionary<string, object>
                                {
                                    ["extractorType"] = "RestApi",
                                    ["url"] = "https://api.example.com/data",
                                    ["method"] = "GET"
                                },
                                DependsOn = new List<string>(),
                                ErrorHandling = new WorkflowStepErrorHandling
                                {
                                    OnError = WorkflowErrorAction.StopWorkflow
                                }
                            },
                            new WorkflowStep
                            {
                                Id = "transform-step",
                                Name = "Transform Data",
                                Description = "Transform the extracted data",
                                Type = WorkflowStepType.Transform,
                                Configuration = new Dictionary<string, object>
                                {
                                    ["transformerType"] = "Json",
                                    ["mappings"] = new Dictionary<string, string>
                                    {
                                        ["id"] = "$.id",
                                        ["name"] = "$.name",
                                        ["value"] = "$.value"
                                    }
                                },
                                DependsOn = new List<string> { "extract-step" },
                                ErrorHandling = new WorkflowStepErrorHandling
                                {
                                    OnError = WorkflowErrorAction.StopWorkflow
                                }
                            },
                            new WorkflowStep
                            {
                                Id = "load-step",
                                Name = "Load Data",
                                Description = "Load the transformed data",
                                Type = WorkflowStepType.Load,
                                Configuration = new Dictionary<string, object>
                                {
                                    ["loaderType"] = "Database",
                                    ["connectionString"] = "Server=localhost;Database=MyDb;User Id=sa;Password=P@ssw0rd;",
                                    ["tableName"] = "ProcessedData"
                                },
                                DependsOn = new List<string> { "transform-step" },
                                ErrorHandling = new WorkflowStepErrorHandling
                                {
                                    OnError = WorkflowErrorAction.StopWorkflow
                                }
                            }
                        },
                        ErrorHandling = new WorkflowErrorHandling
                        {
                            DefaultAction = WorkflowErrorAction.StopWorkflow,
                            MaxErrors = 3,
                            LogDetailedErrors = true
                        }
                    };

                    await _repository.SaveWorkflowAsync(sampleWorkflow);
                    _logger.LogInformation("Created sample workflow");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing sample workflow");
            }
        }

        public async Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Get the workflow from the repository
                var workflow = await _repository.GetWorkflowByIdAsync(workflowId);

                if (workflow == null)
                {
                    throw new KeyNotFoundException($"Workflow with ID {workflowId} not found");
                }

                // Execute the workflow
                return await ExecuteWorkflowAsync(workflow, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
                throw;
            }
        }

        public async Task<WorkflowExecution> ExecuteWorkflowAsync(WorkflowDefinition workflow, Dictionary<string, object> parameters = null)
        {
            if (workflow == null)
            {
                throw new ArgumentNullException(nameof(workflow));
            }

            // Check if we can acquire a semaphore to execute the workflow
            if (!await _executionSemaphore.WaitAsync(0))
            {
                throw new InvalidOperationException($"Maximum number of concurrent workflow executions ({_options.MaxConcurrentExecutions}) reached");
            }

            try
            {
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

                // Create cancellation token with timeout
                var cts = new CancellationTokenSource(_options.DefaultWorkflowTimeoutSeconds * 1000);
                _executionCancellationTokens[execution.Id] = cts;

                // Save execution to repository
                await _repository.SaveExecutionAsync(execution);

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

                // Record workflow start event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowStarted, execution.Id, null, new Dictionary<string, object>
                {
                    ["workflowId"] = workflow.Id,
                    ["workflowName"] = workflow.Name,
                    ["workflowVersion"] = workflow.Version,
                    ["parameters"] = parameters
                });

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

                        // Record workflow failed event
                        await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowFailed, execution.Id, null, new Dictionary<string, object>
                        {
                            ["errorType"] = ex.GetType().Name,
                            ["errorMessage"] = ex.Message
                        });

                        // Update execution in repository
                        await _repository.SaveExecutionAsync(execution);

                        // Send notification if enabled
                        if (_options.EnableNotifications)
                        {
                            await SendNotificationAsync(execution, "Workflow Failed", $"Workflow {workflow.Name} failed: {ex.Message}");
                        }
                    }
                    finally
                    {
                        // Clean up
                        _executionCancellationTokens.Remove(execution.Id);
                        cts.Dispose();
                        _executionSemaphore.Release();
                    }
                });

                return execution;
            }
            catch (Exception ex)
            {
                _executionSemaphore.Release();
                _logger.LogError(ex, "Error starting workflow execution for {WorkflowId}", workflow.Id);
                throw;
            }
        }

        public async Task<WorkflowExecution> GetExecutionStatusAsync(string executionId)
        {
            try
            {
                // Get execution from repository
                var execution = await _repository.GetExecutionByIdAsync(executionId);

                if (execution == null)
                {
                    throw new KeyNotFoundException($"Execution {executionId} not found");
                }

                return execution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution status for {ExecutionId}", executionId);
                throw;
            }
        }

        public async Task<bool> CancelExecutionAsync(string executionId)
        {
            try
            {
                // Get execution from repository
                var execution = await _repository.GetExecutionByIdAsync(executionId);

                if (execution == null)
                {
                    return false;
                }

                // Check if execution is already completed or cancelled
                if (execution.Status == WorkflowExecutionStatus.Completed ||
                    execution.Status == WorkflowExecutionStatus.Failed ||
                    execution.Status == WorkflowExecutionStatus.Cancelled)
                {
                    return false;
                }

                // Cancel the execution
                if (_executionCancellationTokens.TryGetValue(executionId, out var cts))
                {
                    cts.Cancel();
                }

                // Update execution status
                execution.Status = WorkflowExecutionStatus.Cancelled;
                execution.EndTime = DateTime.UtcNow;

                // Record cancellation event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowCancelled, executionId, null, new Dictionary<string, object>
                {
                    ["cancelledBy"] = "User"
                });

                // Save execution to repository
                await _repository.SaveExecutionAsync(execution);

                // Send notification if enabled
                if (_options.EnableNotifications)
                {
                    await SendNotificationAsync(execution, "Workflow Cancelled", $"Workflow {execution.WorkflowId} was cancelled");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling execution {ExecutionId}", executionId);
                return false;
            }
        }

        public async Task<bool> PauseExecutionAsync(string executionId)
        {
            try
            {
                // Get execution from repository
                var execution = await _repository.GetExecutionByIdAsync(executionId);

                if (execution == null)
                {
                    return false;
                }

                // Check if execution is already completed, failed, or cancelled
                if (execution.Status == WorkflowExecutionStatus.Completed ||
                    execution.Status == WorkflowExecutionStatus.Failed ||
                    execution.Status == WorkflowExecutionStatus.Cancelled ||
                    execution.Status == WorkflowExecutionStatus.Paused)
                {
                    return false;
                }

                // Update execution status
                execution.Status = WorkflowExecutionStatus.Paused;

                // Record pause event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowPaused, executionId, null, new Dictionary<string, object>
                {
                    ["pausedBy"] = "User"
                });

                // Save execution to repository
                await _repository.SaveExecutionAsync(execution);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing execution {ExecutionId}", executionId);
                return false;
            }
        }

        public async Task<bool> ResumeExecutionAsync(string executionId)
        {
            try
            {
                // Get execution from repository
                var execution = await _repository.GetExecutionByIdAsync(executionId);

                if (execution == null)
                {
                    return false;
                }

                // Check if execution is paused
                if (execution.Status != WorkflowExecutionStatus.Paused)
                {
                    return false;
                }

                // Update execution status
                execution.Status = WorkflowExecutionStatus.Running;

                // Record resume event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowResumed, executionId, null, new Dictionary<string, object>
                {
                    ["resumedBy"] = "User"
                });

                // Save execution to repository
                await _repository.SaveExecutionAsync(execution);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming execution {ExecutionId}", executionId);
                return false;
            }
        }

        public async Task<List<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int limit = 10)
        {
            try
            {
                // Get execution history from repository
                return await _repository.GetExecutionHistoryAsync(workflowId, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for workflow {WorkflowId}", workflowId);
                throw;
            }
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
                                   AreDependenciesCompleted(s, stepStatuses))
                        .ToList();

                    if (executableSteps.Any())
                    {
                        progress = true;

                        // Execute each step
                        foreach (var step in executableSteps)
                        {
                            await ExecuteStepAsync(step, execution, context, stepStatuses);

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
                    await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowCompleted, execution.Id, null, new Dictionary<string, object>
                    {
                        ["completedSteps"] = stepStatuses.Count(s => s.Value == WorkflowStepExecutionStatus.Completed),
                        ["skippedSteps"] = stepStatuses.Count(s => s.Value == WorkflowStepExecutionStatus.Skipped)
                    });

                    // Send notification if enabled
                    if (_options.EnableNotifications)
                    {
                        await SendNotificationAsync(execution, "Workflow Completed", $"Workflow {context.Workflow.Name} completed successfully");
                    }
                }
                else if (context.CancellationToken.IsCancellationRequested)
                {
                    execution.Status = WorkflowExecutionStatus.Cancelled;

                    // Record workflow cancelled event
                    await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowCancelled, execution.Id, null, new Dictionary<string, object>
                    {
                        ["cancelledBy"] = "System",
                        ["reason"] = "Cancellation requested"
                    });

                    // Send notification if enabled
                    if (_options.EnableNotifications)
                    {
                        await SendNotificationAsync(execution, "Workflow Cancelled", $"Workflow {context.Workflow.Name} was cancelled");
                    }
                }
                else
                {
                    execution.Status = WorkflowExecutionStatus.Failed;

                    // Record workflow failed event
                    await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowFailed, execution.Id, null, new Dictionary<string, object>
                    {
                        ["reason"] = "Not all steps completed"
                    });

                    // Send notification if enabled
                    if (_options.EnableNotifications)
                    {
                        await SendNotificationAsync(execution, "Workflow Failed", $"Workflow {context.Workflow.Name} failed: Not all steps completed");
                    }
                }

                execution.EndTime = DateTime.UtcNow;

                // Update execution in repository
                await _repository.SaveExecutionAsync(execution);

                // Update workflow metrics
                if (_options.EnableMetricsCollection)
                {
                    await _monitor?.UpdateWorkflowMetricsAsync(execution);
                }

                _logger.LogInformation("Workflow execution {ExecutionId} completed with status {Status}",
                    execution.Id, execution.Status);
            }
            catch (OperationCanceledException)
            {
                execution.Status = WorkflowExecutionStatus.Cancelled;
                execution.EndTime = DateTime.UtcNow;

                // Record workflow cancelled event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowCancelled, execution.Id, null, new Dictionary<string, object>
                {
                    ["cancelledBy"] = "System",
                    ["reason"] = "Operation cancelled"
                });

                // Update execution in repository
                await _repository.SaveExecutionAsync(execution);

                // Send notification if enabled
                if (_options.EnableNotifications)
                {
                    await SendNotificationAsync(execution, "Workflow Cancelled", $"Workflow {context.Workflow.Name} was cancelled");
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
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                });

                // Record workflow failed event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.WorkflowFailed, execution.Id, null, new Dictionary<string, object>
                {
                    ["errorType"] = ex.GetType().Name,
                    ["errorMessage"] = ex.Message
                });

                // Update execution in repository
                await _repository.SaveExecutionAsync(execution);

                // Send notification if enabled
                if (_options.EnableNotifications)
                {
                    await SendNotificationAsync(execution, "Workflow Failed", $"Workflow {context.Workflow.Name} failed: {ex.Message}");
                }

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

                // Record step started event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepStarted, execution.Id, step.Id, new Dictionary<string, object>
                {
                    ["stepName"] = step.Name,
                    ["stepType"] = step.Type.ToString()
                });

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
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepCompleted, execution.Id, step.Id, new Dictionary<string, object>
                {
                    ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds,
                    ["retryCount"] = stepExecution.RetryCount
                });

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
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                };

                stepExecution.Errors.Add(error);
                execution.Errors.Add(error);

                // Record error event
                await RecordTimelineEventAsync(WorkflowTimelineEventTypes.ErrorOccurred, execution.Id, step.Id, new Dictionary<string, object>
                {
                    ["errorType"] = ex.GetType().Name,
                    ["errorMessage"] = ex.Message,
                    ["stackTrace"] = ex.StackTrace
                });

                // Handle error based on step configuration
                switch (step.ErrorHandling.OnError)
                {
                    case WorkflowErrorAction.StopWorkflow:
                        stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                        stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                        stepExecution.EndTime = DateTime.UtcNow;

                        // Record step failed event
                        await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepFailed, execution.Id, step.Id, new Dictionary<string, object>
                        {
                            ["errorType"] = ex.GetType().Name,
                            ["errorMessage"] = ex.Message,
                            ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds
                        });

                        // Update execution in repository
                        await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

                        throw new WorkflowStepFailedException($"Step {step.Id} failed and is configured to stop the workflow", ex);

                    case WorkflowErrorAction.ContinueWorkflow:
                        stepStatuses[step.Id] = WorkflowStepExecutionStatus.Failed;
                        stepExecution.Status = WorkflowStepExecutionStatus.Failed;
                        stepExecution.EndTime = DateTime.UtcNow;

                        // Record step failed event
                        await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepFailed, execution.Id, step.Id, new Dictionary<string, object>
                        {
                            ["errorType"] = ex.GetType().Name,
                            ["errorMessage"] = ex.Message,
                            ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds,
                            ["continueWorkflow"] = true
                        });

                        // Update execution in repository
                        await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

                        _logger.LogWarning("Step {StepId} failed but workflow will continue", step.Id);
                        break;

                    case WorkflowErrorAction.RetryStep:
                        if (stepExecution.RetryCount < step.RetryCount)
                        {
                            stepExecution.RetryCount++;
                            _logger.LogInformation("Retrying step {StepId} (attempt {RetryCount} of {MaxRetries})",
                                step.Id, stepExecution.RetryCount, step.RetryCount);

                            // Record retry event
                            await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepRetry, execution.Id, step.Id, new Dictionary<string, object>
                            {
                                ["retryCount"] = stepExecution.RetryCount,
                                ["maxRetries"] = step.RetryCount,
                                ["errorType"] = ex.GetType().Name,
                                ["errorMessage"] = ex.Message
                            });

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
                            await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepFailed, execution.Id, step.Id, new Dictionary<string, object>
                            {
                                ["errorType"] = ex.GetType().Name,
                                ["errorMessage"] = ex.Message,
                                ["duration"] = (stepExecution.EndTime.Value - stepExecution.StartTime).TotalMilliseconds,
                                ["retryCount"] = stepExecution.RetryCount,
                                ["maxRetries"] = step.RetryCount
                            });

                            // Update execution in repository
                            await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

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

                        // Record step skipped event
                        await RecordTimelineEventAsync(WorkflowTimelineEventTypes.StepSkipped, execution.Id, step.Id, new Dictionary<string, object>
                        {
                            ["reason"] = "Error occurred and step is configured to be skipped",
                            ["errorType"] = ex.GetType().Name,
                            ["errorMessage"] = ex.Message
                        });

                        // Update execution in repository
                        await _repository.UpdateStepExecutionAsync(execution.Id, stepExecution);

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

    /// <summary>
    /// Interface for workflow monitoring
    /// </summary>
    public interface IWorkflowMonitor
    {
        Task RecordTimelineEventAsync(WorkflowTimelineEvent timelineEvent);
        Task UpdateWorkflowMetricsAsync(WorkflowExecution execution);
        Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId);
        Task<List<WorkflowTimelineEvent>> GetTimelineEventsAsync(string executionId, int limit = 100);
    }

    /// <summary>
    /// Extension methods for the WorkflowEngine
    /// </summary>
    public static class WorkflowEngineExtensions
    {
        /// <summary>
        /// Records a timeline event for a workflow execution
        /// </summary>
        private static async Task RecordTimelineEventAsync(
            this WorkflowEngine engine,
            string eventType,
            string executionId,
            string stepId,
            Dictionary<string, object> details)
        {
            try
            {
                var timelineEvent = new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    StepId = stepId,
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow,
                    Details = details ?? new Dictionary<string, object>()
                };

                await engine._monitor?.RecordTimelineEventAsync(timelineEvent);
            }
            catch (Exception ex)
            {
                engine._logger.LogError(ex, "Error recording timeline event {EventType} for execution {ExecutionId}", eventType, executionId);
            }
        }

        /// <summary>
        /// Sends a notification for a workflow execution
        /// </summary>
        private static async Task SendNotificationAsync(
            this WorkflowEngine engine,
            WorkflowExecution execution,
            string subject,
            string message)
        {
            try
            {
                if (string.IsNullOrEmpty(engine._options.NotificationServiceUrl))
                {
                    return;
                }

                using var httpClient = new HttpClient();
                var notification = new
                {
                    ExecutionId = execution.Id,
                    WorkflowId = execution.WorkflowId,
                    Subject = subject,
                    Message = message,
                    Status = execution.Status.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(notification),
                    System.Text.Encoding.UTF8,
                    "application/json");

                await httpClient.PostAsync(engine._options.NotificationServiceUrl, content);
            }
            catch (Exception ex)
            {
                engine._logger.LogError(ex, "Error sending notification for execution {ExecutionId}", execution.Id);
            }
        }
    }
}
