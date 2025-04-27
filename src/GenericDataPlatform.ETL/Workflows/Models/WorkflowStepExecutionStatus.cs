namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents the status of a workflow step execution
    /// </summary>
    public enum WorkflowStepExecutionStatus
    {
        /// <summary>
        /// The step execution is pending
        /// </summary>
        Pending,
        
        /// <summary>
        /// The step execution is running
        /// </summary>
        Running,
        
        /// <summary>
        /// The step execution has completed successfully
        /// </summary>
        Completed,
        
        /// <summary>
        /// The step execution has failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// The step execution has been skipped
        /// </summary>
        Skipped,
        
        /// <summary>
        /// The step execution has been cancelled
        /// </summary>
        Cancelled
    }
}
