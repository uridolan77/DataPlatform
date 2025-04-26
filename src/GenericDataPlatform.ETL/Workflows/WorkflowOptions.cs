namespace GenericDataPlatform.ETL.Workflows
{
    /// <summary>
    /// Options for configuring the workflow engine
    /// </summary>
    public class WorkflowOptions
    {
        /// <summary>
        /// The connection string for the workflow database
        /// </summary>
        public string DatabaseConnectionString { get; set; }
        
        /// <summary>
        /// The maximum number of concurrent workflow executions
        /// </summary>
        public int MaxConcurrentExecutions { get; set; } = 10;
        
        /// <summary>
        /// The maximum number of retries for a workflow step
        /// </summary>
        public int DefaultMaxRetries { get; set; } = 3;
        
        /// <summary>
        /// The default timeout for a workflow step in seconds
        /// </summary>
        public int DefaultStepTimeoutSeconds { get; set; } = 3600;
        
        /// <summary>
        /// The default timeout for a workflow execution in seconds
        /// </summary>
        public int DefaultWorkflowTimeoutSeconds { get; set; } = 86400;
        
        /// <summary>
        /// Whether to enable workflow execution history
        /// </summary>
        public bool EnableExecutionHistory { get; set; } = true;
        
        /// <summary>
        /// Whether to enable workflow metrics collection
        /// </summary>
        public bool EnableMetricsCollection { get; set; } = true;
        
        /// <summary>
        /// The maximum number of workflow execution history records to keep
        /// </summary>
        public int MaxExecutionHistoryRecords { get; set; } = 100;
        
        /// <summary>
        /// Whether to enable workflow execution notifications
        /// </summary>
        public bool EnableNotifications { get; set; } = true;
        
        /// <summary>
        /// The URL of the notification service
        /// </summary>
        public string NotificationServiceUrl { get; set; }
    }
}
