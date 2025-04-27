using GenericDataPlatform.ETL.Workflows.Conditions;
using GenericDataPlatform.ETL.Workflows.Execution;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Monitoring;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.ETL.Workflows
{
    /// <summary>
    /// Main workflow engine implementation
    /// </summary>
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly ILogger<WorkflowEngine> _logger;
        private readonly Dictionary<string, IWorkflowStepProcessor> _stepProcessors;
        private readonly IWorkflowRepository _repository;
        private readonly WorkflowOptions _options;
        private readonly Dictionary<string, CancellationTokenSource> _executionCancellationTokens = new();
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly IWorkflowMonitor _monitor;
        private readonly WorkflowConditionEvaluator _conditionEvaluator;
        private readonly WorkflowStepExecutor _stepExecutor;
        private readonly WorkflowExecutor _workflowExecutor;

        public WorkflowEngine(
            ILogger<WorkflowEngine> logger,
            IServiceProvider serviceProvider, // Not used but kept for backward compatibility
            IEnumerable<IWorkflowStepProcessor> stepProcessors,
            IWorkflowRepository repository,
            IOptions<WorkflowOptions> options,
            IWorkflowMonitor monitor,
            ILogger<WorkflowConditionEvaluator> conditionEvaluatorLogger,
            ILogger<WorkflowStepExecutor> stepExecutorLogger,
            ILogger<WorkflowExecutor> workflowExecutorLogger)
        {
            _logger = logger;
            _stepProcessors = stepProcessors.ToDictionary(p => p.StepType, p => p);
            _repository = repository;
            _options = options.Value;
            _monitor = monitor;
            _executionSemaphore = new SemaphoreSlim(_options.MaxConcurrentExecutions, _options.MaxConcurrentExecutions);

            // Create condition evaluator
            _conditionEvaluator = new WorkflowConditionEvaluator(conditionEvaluatorLogger);

            // Create step executor
            _stepExecutor = new WorkflowStepExecutor(
                stepExecutorLogger,
                _stepProcessors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                _repository,
                _monitor,
                _conditionEvaluator,
                _options);

            // Create workflow executor
            _workflowExecutor = new WorkflowExecutor(
                workflowExecutorLogger,
                _repository,
                _monitor,
                _stepExecutor,
                _options);

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

        public async Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object>? parameters = null)
        {
            try
            {
                // Get the workflow from the repository
                var workflow = await _repository.GetWorkflowByIdAsync(workflowId);

                if (workflow is null)
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

        public async Task<WorkflowExecution> ExecuteWorkflowAsync(WorkflowDefinition workflow, Dictionary<string, object>? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(workflow);

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
                    Parameters = parameters ?? new(),
                    StepExecutions = new(),
                    Errors = new(),
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
                    Variables = new(),
                    StepOutputs = new(),
                    CancellationToken = cts.Token
                };

                // Record workflow start event
                await _monitor.RecordTimelineEventAsync(
                    execution.Id,
                    null,
                    WorkflowTimelineEventTypes.WorkflowStarted,
                    new Dictionary<string, object>
                    {
                        ["workflowId"] = workflow.Id,
                        ["workflowName"] = workflow.Name,
                        ["workflowVersion"] = workflow.Version,
                        ["parameters"] = parameters ?? new()
                    },
                    _logger);

                // Execute workflow asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _workflowExecutor.ExecuteWorkflowInternalAsync(execution, context);
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
                                $"Workflow {workflow.Name} failed: {ex.Message}",
                                _logger);
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

                if (execution is null)
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
                await _monitor.RecordTimelineEventAsync(
                    executionId,
                    null,
                    WorkflowTimelineEventTypes.WorkflowCancelled,
                    new Dictionary<string, object>
                    {
                        ["cancelledBy"] = "User"
                    },
                    _logger);

                // Save execution to repository
                await _repository.SaveExecutionAsync(execution);

                // Send notification if enabled
                if (_options.EnableNotifications)
                {
                    await WorkflowMonitoringExtensions.SendNotificationAsync(
                        _options.NotificationServiceUrl,
                        execution,
                        "Workflow Cancelled",
                        $"Workflow {execution.WorkflowId} was cancelled",
                        _logger);
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
                await _monitor.RecordTimelineEventAsync(
                    executionId,
                    null,
                    WorkflowTimelineEventTypes.WorkflowPaused,
                    new Dictionary<string, object>
                    {
                        ["pausedBy"] = "User"
                    },
                    _logger);

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
                await _monitor.RecordTimelineEventAsync(
                    executionId,
                    null,
                    WorkflowTimelineEventTypes.WorkflowResumed,
                    new Dictionary<string, object>
                    {
                        ["resumedBy"] = "User"
                    },
                    _logger);

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


    }


}
