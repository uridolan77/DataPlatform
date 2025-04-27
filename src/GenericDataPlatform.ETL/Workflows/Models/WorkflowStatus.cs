namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents the status of a workflow
    /// </summary>
    public enum WorkflowStatus
    {
        /// <summary>
        /// The workflow is idle
        /// </summary>
        Idle,
        
        /// <summary>
        /// The workflow is running
        /// </summary>
        Running,
        
        /// <summary>
        /// The workflow is paused
        /// </summary>
        Paused,
        
        /// <summary>
        /// The workflow has completed successfully
        /// </summary>
        Completed,
        
        /// <summary>
        /// The workflow has failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// The workflow has been cancelled
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// The workflow status is unknown
        /// </summary>
        Unknown
    }
}
