namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents the status of a workflow execution
    /// </summary>
    public enum WorkflowExecutionStatus
    {
        /// <summary>
        /// The workflow execution is pending
        /// </summary>
        Pending,
        
        /// <summary>
        /// The workflow execution is running
        /// </summary>
        Running,
        
        /// <summary>
        /// The workflow execution is paused
        /// </summary>
        Paused,
        
        /// <summary>
        /// The workflow execution has completed successfully
        /// </summary>
        Completed,
        
        /// <summary>
        /// The workflow execution has failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// The workflow execution has been cancelled
        /// </summary>
        Cancelled
    }
}
