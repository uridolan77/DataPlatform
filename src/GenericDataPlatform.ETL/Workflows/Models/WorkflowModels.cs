using System;
using System.Collections.Generic;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<WorkflowTrigger> Triggers { get; set; } = new List<WorkflowTrigger>();
        public WorkflowErrorHandling ErrorHandling { get; set; } = new WorkflowErrorHandling();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Version { get; set; }
    }

    public class WorkflowStep
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public WorkflowStepType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public List<string> DependsOn { get; set; } = new List<string>();
        public WorkflowStepErrorHandling ErrorHandling { get; set; } = new WorkflowStepErrorHandling();
        public List<WorkflowCondition> Conditions { get; set; } = new List<WorkflowCondition>();
        public int RetryCount { get; set; } = 0;
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(30);
        public int Timeout { get; set; } = 3600; // Default timeout in seconds
    }

    public enum WorkflowStepType
    {
        Extract,
        Transform,
        Load,
        Validate,
        Enrich,
        Branch,
        Merge,
        Notification,
        Custom
    }

    public class WorkflowStepErrorHandling
    {
        public WorkflowErrorAction OnError { get; set; } = WorkflowErrorAction.StopWorkflow;
        public string FallbackStepId { get; set; }
        public Dictionary<string, object> ErrorOutput { get; set; } = new Dictionary<string, object>();
    }

    public class WorkflowErrorHandling
    {
        public WorkflowErrorAction DefaultAction { get; set; } = WorkflowErrorAction.StopWorkflow;
        public int MaxErrors { get; set; } = 10;
        public List<string> NotificationTargets { get; set; } = new List<string>();
        public bool LogDetailedErrors { get; set; } = true;
    }

    public enum WorkflowErrorAction
    {
        StopWorkflow,
        ContinueWorkflow,
        RetryStep,
        SkipStep,
        ExecuteFallback
    }

    public class WorkflowTrigger
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public WorkflowTriggerType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    public enum WorkflowTriggerType
    {
        Schedule,
        Event,
        Manual,
        DataChange,
        Dependency
    }

    public class WorkflowCondition
    {
        public string Id { get; set; }
        public string Expression { get; set; }
        public string Description { get; set; }
        public WorkflowConditionType Type { get; set; } = WorkflowConditionType.Expression;
    }

    public enum WorkflowConditionType
    {
        Expression,
        Script,
        DataBased,
        External
    }

    public class WorkflowExecution
    {
        public string Id { get; set; }
        public string WorkflowId { get; set; }
        public WorkflowExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Output { get; set; } = new Dictionary<string, object>();
        public List<WorkflowStepExecution> StepExecutions { get; set; } = new List<WorkflowStepExecution>();
        public List<WorkflowExecutionError> Errors { get; set; } = new List<WorkflowExecutionError>();
        public string TriggerId { get; set; }
        public string TriggerType { get; set; }
    }

    public class WorkflowStepExecution
    {
        public string Id { get; set; }
        public string StepId { get; set; }
        public WorkflowStepExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Dictionary<string, object> Input { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Output { get; set; } = new Dictionary<string, object>();
        public List<WorkflowExecutionError> Errors { get; set; } = new List<WorkflowExecutionError>();
        public int RetryCount { get; set; } = 0;
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
    }

    public enum WorkflowExecutionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }

    public enum WorkflowStepExecutionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Skipped,
        Cancelled,
        Waiting
    }

    public class WorkflowExecutionError
    {
        public string Id { get; set; }
        public string StepId { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    public class WorkflowContext
    {
        public string ExecutionId { get; set; }
        public WorkflowDefinition Workflow { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> StepOutputs { get; set; } = new Dictionary<string, object>();
        public DataSourceDefinition Source { get; set; }
        public System.Threading.CancellationToken CancellationToken { get; set; }
    }
}
