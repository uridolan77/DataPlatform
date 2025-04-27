namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents the types of events that can occur in a workflow timeline
    /// </summary>
    public enum WorkflowTimelineEventTypes
    {
        /// <summary>
        /// Workflow started
        /// </summary>
        WorkflowStarted,
        
        /// <summary>
        /// Workflow completed
        /// </summary>
        WorkflowCompleted,
        
        /// <summary>
        /// Workflow failed
        /// </summary>
        WorkflowFailed,
        
        /// <summary>
        /// Workflow paused
        /// </summary>
        WorkflowPaused,
        
        /// <summary>
        /// Workflow resumed
        /// </summary>
        WorkflowResumed,
        
        /// <summary>
        /// Workflow cancelled
        /// </summary>
        WorkflowCancelled,
        
        /// <summary>
        /// Step started
        /// </summary>
        StepStarted,
        
        /// <summary>
        /// Step completed
        /// </summary>
        StepCompleted,
        
        /// <summary>
        /// Step failed
        /// </summary>
        StepFailed,
        
        /// <summary>
        /// Step retrying
        /// </summary>
        StepRetrying,
        
        /// <summary>
        /// Step skipped
        /// </summary>
        StepSkipped,
        
        /// <summary>
        /// Error occurred
        /// </summary>
        ErrorOccurred,
        
        /// <summary>
        /// Warning occurred
        /// </summary>
        WarningOccurred,
        
        /// <summary>
        /// Information event
        /// </summary>
        Information,
        
        /// <summary>
        /// Custom event
        /// </summary>
        Custom
    }
}
