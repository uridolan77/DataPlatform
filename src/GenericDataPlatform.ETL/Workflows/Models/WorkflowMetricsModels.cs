using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents metrics for a workflow
    /// </summary>
    public class WorkflowMetrics
    {
        public string WorkflowId { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public int CancelledExecutions { get; set; }
        public double AverageExecutionTimeInSeconds { get; set; }
        public double MaxExecutionTimeInSeconds { get; set; }
        public double MinExecutionTimeInSeconds { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public Dictionary<string, StepMetrics> StepMetrics { get; set; } = new Dictionary<string, StepMetrics>();
        public List<ErrorMetrics> CommonErrors { get; set; } = new List<ErrorMetrics>();
    }

    /// <summary>
    /// Represents metrics for a workflow step
    /// </summary>
    public class StepMetrics
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public int SkippedExecutions { get; set; }
        public double AverageExecutionTimeInSeconds { get; set; }
        public double MaxExecutionTimeInSeconds { get; set; }
        public double MinExecutionTimeInSeconds { get; set; }
        public int AverageRetryCount { get; set; }
        public List<ErrorMetrics> CommonErrors { get; set; } = new List<ErrorMetrics>();
    }

    /// <summary>
    /// Represents error metrics
    /// </summary>
    public class ErrorMetrics
    {
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public int Occurrences { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    /// <summary>
    /// Represents a summary of a workflow execution
    /// </summary>
    public class WorkflowExecutionSummary
    {
        public string ExecutionId { get; set; }
        public string WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public string WorkflowVersion { get; set; }
        public WorkflowExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int FailedSteps { get; set; }
        public int SkippedSteps { get; set; }
        public string TriggerType { get; set; }
        public int ErrorCount { get; set; }
        public List<StepExecutionSummary> StepSummaries { get; set; } = new List<StepExecutionSummary>();
    }

    /// <summary>
    /// Represents a summary of a workflow step execution
    /// </summary>
    public class StepExecutionSummary
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public WorkflowStepType StepType { get; set; }
        public WorkflowStepExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
        public int RetryCount { get; set; }
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// Represents a timeline event for a workflow execution
    /// </summary>
    public class WorkflowTimelineEvent
    {
        public string Id { get; set; }
        public string ExecutionId { get; set; }
        public string StepId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the type of timeline event
    /// </summary>
    public static class WorkflowTimelineEventTypes
    {
        public const string WorkflowStarted = "WorkflowStarted";
        public const string WorkflowCompleted = "WorkflowCompleted";
        public const string WorkflowFailed = "WorkflowFailed";
        public const string WorkflowCancelled = "WorkflowCancelled";
        public const string WorkflowPaused = "WorkflowPaused";
        public const string WorkflowResumed = "WorkflowResumed";
        public const string StepStarted = "StepStarted";
        public const string StepCompleted = "StepCompleted";
        public const string StepFailed = "StepFailed";
        public const string StepSkipped = "StepSkipped";
        public const string StepRetry = "StepRetry";
        public const string ErrorOccurred = "ErrorOccurred";
        public const string VariableSet = "VariableSet";
        public const string ConditionEvaluated = "ConditionEvaluated";
    }
}
