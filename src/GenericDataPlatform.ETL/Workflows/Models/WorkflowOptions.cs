namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Options for workflow configuration
    /// </summary>
    public class WorkflowOptions
    {
        /// <summary>
        /// Gets or sets whether metrics collection is enabled
        /// </summary>
        public bool EnableMetricsCollection { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether timeline events are enabled
        /// </summary>
        public bool EnableTimelineEvents { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to track data lineage
        /// </summary>
        public bool TrackDataLineage { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the maximum number of retries for failed steps
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the retry delay in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent executions
        /// </summary>
        public int MaxConcurrentExecutions { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the execution timeout in seconds
        /// </summary>
        public int ExecutionTimeoutSeconds { get; set; } = 3600;
    }
}
