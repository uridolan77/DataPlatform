using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace GenericDataPlatform.ETL.Workflows.Repositories
{
    /// <summary>
    /// Database implementation of the workflow repository
    /// </summary>
    public class DatabaseWorkflowRepository : IWorkflowRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseWorkflowRepository> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly JsonSerializerOptions _jsonOptions;

        public DatabaseWorkflowRepository(
            IOptions<WorkflowOptions> options,
            ILogger<DatabaseWorkflowRepository> logger,
            IAsyncPolicy resiliencePolicy)
        {
            _connectionString = options.Value.DatabaseConnectionString;
            _logger = logger;
            _resiliencePolicy = resiliencePolicy;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        #region Workflow Definition Operations

        public async Task<WorkflowDefinition> GetWorkflowByIdAsync(string id, string version = null)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    string sql;
                    object parameters;

                    if (string.IsNullOrEmpty(version))
                    {
                        // Get the latest version
                        sql = @"
                            SELECT Definition 
                            FROM WorkflowDefinitions 
                            WHERE Id = @Id 
                            ORDER BY Version DESC 
                            LIMIT 1";
                        
                        parameters = new { Id = id };
                    }
                    else
                    {
                        // Get the specific version
                        sql = @"
                            SELECT Definition 
                            FROM WorkflowDefinitions 
                            WHERE Id = @Id AND Version = @Version";
                        
                        parameters = new { Id = id, Version = version };
                    }

                    var definitionJson = await connection.QueryFirstOrDefaultAsync<string>(sql, parameters);
                    
                    if (string.IsNullOrEmpty(definitionJson))
                        return null;

                    return JsonSerializer.Deserialize<WorkflowDefinition>(definitionJson, _jsonOptions);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow {WorkflowId} version {Version}", id, version);
                throw;
            }
        }

        public async Task<List<WorkflowDefinition>> GetWorkflowsAsync(int skip = 0, int take = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var sql = @"
                        WITH LatestVersions AS (
                            SELECT Id, MAX(Version) AS Version
                            FROM WorkflowDefinitions
                            GROUP BY Id
                        )
                        SELECT wd.Definition
                        FROM WorkflowDefinitions wd
                        JOIN LatestVersions lv ON wd.Id = lv.Id AND wd.Version = lv.Version
                        ORDER BY wd.UpdatedAt DESC
                        LIMIT @Take OFFSET @Skip";

                    var definitionJsons = await connection.QueryAsync<string>(sql, new { Skip = skip, Take = take });
                    
                    var workflows = new List<WorkflowDefinition>();
                    foreach (var json in definitionJsons)
                    {
                        workflows.Add(JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions));
                    }

                    return workflows;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflows");
                throw;
            }
        }

        public async Task<List<string>> GetWorkflowVersionsAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT Version
                        FROM WorkflowDefinitions
                        WHERE Id = @Id
                        ORDER BY Version DESC";

                    return (await connection.QueryAsync<string>(sql, new { Id = id })).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions for workflow {WorkflowId}", id);
                throw;
            }
        }

        public async Task<string> SaveWorkflowAsync(WorkflowDefinition workflow)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure workflow has an ID
                    if (string.IsNullOrEmpty(workflow.Id))
                    {
                        workflow.Id = Guid.NewGuid().ToString();
                    }

                    // Set timestamps
                    var now = DateTime.UtcNow;
                    if (workflow.CreatedAt == default)
                    {
                        workflow.CreatedAt = now;
                    }
                    workflow.UpdatedAt = now;

                    // Determine version
                    if (string.IsNullOrEmpty(workflow.Version))
                    {
                        // Get the latest version and increment
                        var versions = await GetWorkflowVersionsAsync(workflow.Id);
                        if (versions.Any())
                        {
                            var latestVersion = versions.First();
                            if (Version.TryParse(latestVersion, out var version))
                            {
                                workflow.Version = new Version(version.Major, version.Minor + 1).ToString();
                            }
                            else
                            {
                                workflow.Version = "1.0";
                            }
                        }
                        else
                        {
                            workflow.Version = "1.0";
                        }
                    }

                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var definitionJson = JsonSerializer.Serialize(workflow, _jsonOptions);

                    var sql = @"
                        INSERT INTO WorkflowDefinitions (Id, Version, Name, Description, Definition, CreatedAt, UpdatedAt)
                        VALUES (@Id, @Version, @Name, @Description, @Definition, @CreatedAt, @UpdatedAt)
                        ON CONFLICT (Id, Version) 
                        DO UPDATE SET 
                            Name = EXCLUDED.Name,
                            Description = EXCLUDED.Description,
                            Definition = EXCLUDED.Definition,
                            UpdatedAt = EXCLUDED.UpdatedAt";

                    await connection.ExecuteAsync(sql, new
                    {
                        Id = workflow.Id,
                        Version = workflow.Version,
                        Name = workflow.Name,
                        Description = workflow.Description,
                        Definition = definitionJson,
                        CreatedAt = workflow.CreatedAt,
                        UpdatedAt = workflow.UpdatedAt
                    });

                    return workflow.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving workflow {WorkflowId}", workflow.Id);
                throw;
            }
        }

        public async Task<bool> DeleteWorkflowAsync(string id, string version = null)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    string sql;
                    object parameters;

                    if (string.IsNullOrEmpty(version))
                    {
                        // Delete all versions
                        sql = "DELETE FROM WorkflowDefinitions WHERE Id = @Id";
                        parameters = new { Id = id };
                    }
                    else
                    {
                        // Delete specific version
                        sql = "DELETE FROM WorkflowDefinitions WHERE Id = @Id AND Version = @Version";
                        parameters = new { Id = id, Version = version };
                    }

                    var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow {WorkflowId} version {Version}", id, version);
                throw;
            }
        }

        #endregion

        #region Workflow Execution Operations

        public async Task<WorkflowExecution> GetExecutionByIdAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT ExecutionData
                        FROM WorkflowExecutions
                        WHERE Id = @Id";

                    var executionJson = await connection.QueryFirstOrDefaultAsync<string>(sql, new { Id = id });
                    
                    if (string.IsNullOrEmpty(executionJson))
                        return null;

                    return JsonSerializer.Deserialize<WorkflowExecution>(executionJson, _jsonOptions);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution {ExecutionId}", id);
                throw;
            }
        }

        public async Task<List<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int limit = 10)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT ExecutionData
                        FROM WorkflowExecutions
                        WHERE WorkflowId = @WorkflowId
                        ORDER BY StartTime DESC
                        LIMIT @Limit";

                    var executionJsons = await connection.QueryAsync<string>(sql, new { WorkflowId = workflowId, Limit = limit });
                    
                    var executions = new List<WorkflowExecution>();
                    foreach (var json in executionJsons)
                    {
                        executions.Add(JsonSerializer.Deserialize<WorkflowExecution>(json, _jsonOptions));
                    }

                    return executions;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for workflow {WorkflowId}", workflowId);
                throw;
            }
        }

        public async Task<List<WorkflowExecution>> GetRecentExecutionsAsync(int limit = 10)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT ExecutionData
                        FROM WorkflowExecutions
                        ORDER BY StartTime DESC
                        LIMIT @Limit";

                    var executionJsons = await connection.QueryAsync<string>(sql, new { Limit = limit });
                    
                    var executions = new List<WorkflowExecution>();
                    foreach (var json in executionJsons)
                    {
                        executions.Add(JsonSerializer.Deserialize<WorkflowExecution>(json, _jsonOptions));
                    }

                    return executions;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent executions");
                throw;
            }
        }

        public async Task<string> SaveExecutionAsync(WorkflowExecution execution)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure execution has an ID
                    if (string.IsNullOrEmpty(execution.Id))
                    {
                        execution.Id = Guid.NewGuid().ToString();
                    }

                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    var executionJson = JsonSerializer.Serialize(execution, _jsonOptions);

                    var sql = @"
                        INSERT INTO WorkflowExecutions (Id, WorkflowId, Status, StartTime, EndTime, ExecutionData)
                        VALUES (@Id, @WorkflowId, @Status, @StartTime, @EndTime, @ExecutionData)
                        ON CONFLICT (Id) 
                        DO UPDATE SET 
                            Status = EXCLUDED.Status,
                            EndTime = EXCLUDED.EndTime,
                            ExecutionData = EXCLUDED.ExecutionData";

                    await connection.ExecuteAsync(sql, new
                    {
                        Id = execution.Id,
                        WorkflowId = execution.WorkflowId,
                        Status = execution.Status.ToString(),
                        StartTime = execution.StartTime,
                        EndTime = execution.EndTime,
                        ExecutionData = executionJson
                    });

                    return execution.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving execution {ExecutionId}", execution.Id);
                throw;
            }
        }

        public async Task<bool> UpdateExecutionStatusAsync(string id, WorkflowExecutionStatus status)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var execution = await GetExecutionByIdAsync(id);
                    if (execution == null)
                        return false;

                    execution.Status = status;
                    
                    if (status == WorkflowExecutionStatus.Completed || 
                        status == WorkflowExecutionStatus.Failed || 
                        status == WorkflowExecutionStatus.Cancelled)
                    {
                        execution.EndTime = DateTime.UtcNow;
                    }

                    await SaveExecutionAsync(execution);
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating execution status for {ExecutionId}", id);
                throw;
            }
        }

        public async Task<bool> UpdateStepExecutionAsync(string executionId, WorkflowStepExecution stepExecution)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var execution = await GetExecutionByIdAsync(executionId);
                    if (execution == null)
                        return false;

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

                    await SaveExecutionAsync(execution);
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating step execution for {ExecutionId}, step {StepId}", executionId, stepExecution.StepId);
                throw;
            }
        }

        #endregion

        #region Workflow Metrics Operations

        public async Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = CreateConnection();
                    await connection.OpenAsync();

                    // Get basic execution metrics
                    var sql = @"
                        SELECT 
                            COUNT(*) AS TotalExecutions,
                            SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS SuccessfulExecutions,
                            SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) AS FailedExecutions,
                            SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledExecutions,
                            AVG(CASE WHEN EndTime IS NOT NULL THEN EXTRACT(EPOCH FROM (EndTime - StartTime)) ELSE NULL END) AS AvgExecutionTime,
                            MAX(CASE WHEN EndTime IS NOT NULL THEN EXTRACT(EPOCH FROM (EndTime - StartTime)) ELSE NULL END) AS MaxExecutionTime,
                            MIN(CASE WHEN EndTime IS NOT NULL THEN EXTRACT(EPOCH FROM (EndTime - StartTime)) ELSE NULL END) AS MinExecutionTime,
                            MAX(StartTime) AS LastExecutionTime
                        FROM WorkflowExecutions
                        WHERE WorkflowId = @WorkflowId";

                    var basicMetrics = await connection.QueryFirstOrDefaultAsync(sql, new { WorkflowId = workflowId });
                    
                    if (basicMetrics == null || basicMetrics.TotalExecutions == 0)
                    {
                        return new WorkflowMetrics
                        {
                            WorkflowId = workflowId,
                            TotalExecutions = 0,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 0,
                            CancelledExecutions = 0,
                            AverageExecutionTimeInSeconds = 0,
                            MaxExecutionTimeInSeconds = 0,
                            MinExecutionTimeInSeconds = 0,
                            LastExecutionTime = DateTime.MinValue
                        };
                    }

                    // Get recent executions to analyze step metrics and common errors
                    var executions = await GetExecutionHistoryAsync(workflowId, 20);
                    
                    var stepMetrics = new Dictionary<string, StepMetrics>();
                    var errorCounts = new Dictionary<string, ErrorMetrics>();

                    foreach (var execution in executions)
                    {
                        // Process step metrics
                        foreach (var step in execution.StepExecutions)
                        {
                            if (!stepMetrics.TryGetValue(step.StepId, out var metrics))
                            {
                                metrics = new StepMetrics
                                {
                                    StepId = step.StepId,
                                    StepName = step.StepId, // We don't have the name in the execution data
                                    TotalExecutions = 0,
                                    SuccessfulExecutions = 0,
                                    FailedExecutions = 0,
                                    SkippedExecutions = 0,
                                    AverageExecutionTimeInSeconds = 0,
                                    MaxExecutionTimeInSeconds = 0,
                                    MinExecutionTimeInSeconds = double.MaxValue,
                                    AverageRetryCount = 0,
                                    CommonErrors = new List<ErrorMetrics>()
                                };
                                stepMetrics[step.StepId] = metrics;
                            }

                            metrics.TotalExecutions++;
                            
                            if (step.Status == WorkflowStepExecutionStatus.Completed)
                                metrics.SuccessfulExecutions++;
                            else if (step.Status == WorkflowStepExecutionStatus.Failed)
                                metrics.FailedExecutions++;
                            else if (step.Status == WorkflowStepExecutionStatus.Skipped)
                                metrics.SkippedExecutions++;

                            if (step.EndTime.HasValue)
                            {
                                var duration = (step.EndTime.Value - step.StartTime).TotalSeconds;
                                metrics.AverageExecutionTimeInSeconds = 
                                    (metrics.AverageExecutionTimeInSeconds * (metrics.TotalExecutions - 1) + duration) / metrics.TotalExecutions;
                                
                                metrics.MaxExecutionTimeInSeconds = Math.Max(metrics.MaxExecutionTimeInSeconds, duration);
                                metrics.MinExecutionTimeInSeconds = Math.Min(metrics.MinExecutionTimeInSeconds, duration);
                            }

                            metrics.AverageRetryCount = 
                                (metrics.AverageRetryCount * (metrics.TotalExecutions - 1) + step.RetryCount) / metrics.TotalExecutions;

                            // Process step errors
                            foreach (var error in step.Errors)
                            {
                                var errorKey = $"{error.ErrorType}:{error.Message}";
                                
                                if (!errorCounts.TryGetValue(errorKey, out var errorMetrics))
                                {
                                    errorMetrics = new ErrorMetrics
                                    {
                                        ErrorType = error.ErrorType,
                                        ErrorMessage = error.Message,
                                        Occurrences = 0,
                                        FirstOccurrence = error.Timestamp,
                                        LastOccurrence = error.Timestamp
                                    };
                                    errorCounts[errorKey] = errorMetrics;
                                }

                                errorMetrics.Occurrences++;
                                errorMetrics.FirstOccurrence = error.Timestamp < errorMetrics.FirstOccurrence 
                                    ? error.Timestamp 
                                    : errorMetrics.FirstOccurrence;
                                errorMetrics.LastOccurrence = error.Timestamp > errorMetrics.LastOccurrence 
                                    ? error.Timestamp 
                                    : errorMetrics.LastOccurrence;
                            }
                        }

                        // Process workflow errors
                        foreach (var error in execution.Errors)
                        {
                            var errorKey = $"{error.ErrorType}:{error.Message}";
                            
                            if (!errorCounts.TryGetValue(errorKey, out var errorMetrics))
                            {
                                errorMetrics = new ErrorMetrics
                                {
                                    ErrorType = error.ErrorType,
                                    ErrorMessage = error.Message,
                                    Occurrences = 0,
                                    FirstOccurrence = error.Timestamp,
                                    LastOccurrence = error.Timestamp
                                };
                                errorCounts[errorKey] = errorMetrics;
                            }

                            errorMetrics.Occurrences++;
                            errorMetrics.FirstOccurrence = error.Timestamp < errorMetrics.FirstOccurrence 
                                ? error.Timestamp 
                                : errorMetrics.FirstOccurrence;
                            errorMetrics.LastOccurrence = error.Timestamp > errorMetrics.LastOccurrence 
                                ? error.Timestamp 
                                : errorMetrics.LastOccurrence;
                        }
                    }

                    // Create the workflow metrics
                    var workflowMetrics = new WorkflowMetrics
                    {
                        WorkflowId = workflowId,
                        TotalExecutions = basicMetrics.TotalExecutions,
                        SuccessfulExecutions = basicMetrics.SuccessfulExecutions,
                        FailedExecutions = basicMetrics.FailedExecutions,
                        CancelledExecutions = basicMetrics.CancelledExecutions,
                        AverageExecutionTimeInSeconds = basicMetrics.AvgExecutionTime ?? 0,
                        MaxExecutionTimeInSeconds = basicMetrics.MaxExecutionTime ?? 0,
                        MinExecutionTimeInSeconds = basicMetrics.MinExecutionTime ?? 0,
                        LastExecutionTime = basicMetrics.LastExecutionTime,
                        StepMetrics = stepMetrics,
                        CommonErrors = errorCounts.Values.OrderByDescending(e => e.Occurrences).Take(10).ToList()
                    };

                    return workflowMetrics;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics for workflow {WorkflowId}", workflowId);
                throw;
            }
        }

        public async Task<List<WorkflowExecutionSummary>> GetExecutionSummariesAsync(string workflowId, int limit = 10)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var executions = await GetExecutionHistoryAsync(workflowId, limit);
                    var summaries = new List<WorkflowExecutionSummary>();

                    foreach (var execution in executions)
                    {
                        var summary = new WorkflowExecutionSummary
                        {
                            ExecutionId = execution.Id,
                            WorkflowId = execution.WorkflowId,
                            WorkflowName = "", // We don't have this in the execution data
                            WorkflowVersion = "", // We don't have this in the execution data
                            Status = execution.Status,
                            StartTime = execution.StartTime,
                            EndTime = execution.EndTime,
                            TotalSteps = execution.StepExecutions.Count,
                            CompletedSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepExecutionStatus.Completed),
                            FailedSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepExecutionStatus.Failed),
                            SkippedSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepExecutionStatus.Skipped),
                            TriggerType = execution.TriggerType,
                            ErrorCount = execution.Errors.Count,
                            StepSummaries = execution.StepExecutions.Select(s => new StepExecutionSummary
                            {
                                StepId = s.StepId,
                                StepName = s.StepId, // We don't have the name in the execution data
                                StepType = WorkflowStepType.Custom, // We don't have this in the execution data
                                Status = s.Status,
                                StartTime = s.StartTime,
                                EndTime = s.EndTime,
                                RetryCount = s.RetryCount,
                                ErrorCount = s.Errors.Count
                            }).ToList()
                        };

                        summaries.Add(summary);
                    }

                    return summaries;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution summaries for workflow {WorkflowId}", workflowId);
                throw;
            }
        }

        #endregion

        private IDbConnection CreateConnection()
        {
            // Create the appropriate connection based on the connection string
            if (_connectionString.Contains("postgres"))
            {
                return new Npgsql.NpgsqlConnection(_connectionString);
            }
            else if (_connectionString.Contains("Server=") || _connectionString.Contains("Data Source="))
            {
                return new System.Data.SqlClient.SqlConnection(_connectionString);
            }
            else
            {
                return new MySql.Data.MySqlClient.MySqlConnection(_connectionString);
            }
        }
    }
}
