namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents error handling configuration for a workflow
    /// </summary>
    public class WorkflowErrorHandling
    {
        /// <summary>
        /// Gets or sets the default action to take when an error occurs
        /// </summary>
        public WorkflowErrorAction DefaultAction { get; set; } = WorkflowErrorAction.StopWorkflow;
        
        /// <summary>
        /// Gets or sets the maximum number of errors allowed before stopping the workflow
        /// </summary>
        public int MaxErrors { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets whether to log detailed error information
        /// </summary>
        public bool LogDetailedErrors { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to notify on errors
        /// </summary>
        public bool NotifyOnError { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the notification channel for errors
        /// </summary>
        public string NotificationChannel { get; set; }
    }
    
    /// <summary>
    /// Represents error handling configuration for a workflow step
    /// </summary>
    public class WorkflowStepErrorHandling
    {
        /// <summary>
        /// Gets or sets the action to take when an error occurs
        /// </summary>
        public WorkflowErrorAction OnError { get; set; } = WorkflowErrorAction.StopWorkflow;
        
        /// <summary>
        /// Gets or sets the maximum number of retries
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the retry interval in seconds
        /// </summary>
        public int RetryIntervalSeconds { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to notify on errors
        /// </summary>
        public bool NotifyOnError { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the notification channel for errors
        /// </summary>
        public string NotificationChannel { get; set; }
    }
    
    /// <summary>
    /// Represents the action to take when an error occurs in a workflow
    /// </summary>
    public enum WorkflowErrorAction
    {
        /// <summary>
        /// Continue execution and ignore the error
        /// </summary>
        Continue,
        
        /// <summary>
        /// Retry the step
        /// </summary>
        Retry,
        
        /// <summary>
        /// Skip the step and continue with the next step
        /// </summary>
        SkipStep,
        
        /// <summary>
        /// Stop the workflow
        /// </summary>
        StopWorkflow,
        
        /// <summary>
        /// Pause the workflow for manual intervention
        /// </summary>
        PauseWorkflow
    }
}
