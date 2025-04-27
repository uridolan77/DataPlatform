using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents metrics for a workflow
    /// </summary>
    public class WorkflowMetrics
    {
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of executions
        /// </summary>
        public int TotalExecutions { get; set; }
        
        /// <summary>
        /// Gets or sets the number of successful executions
        /// </summary>
        public int SuccessfulExecutions { get; set; }
        
        /// <summary>
        /// Gets or sets the number of failed executions
        /// </summary>
        public int FailedExecutions { get; set; }
        
        /// <summary>
        /// Gets or sets the average duration in milliseconds
        /// </summary>
        public double AverageDuration { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time of the last execution
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }
        
        /// <summary>
        /// Gets or sets the metrics for each activity
        /// </summary>
        public Dictionary<string, ActivityMetrics> ActivityMetrics { get; set; } = new Dictionary<string, ActivityMetrics>();
    }
    
    /// <summary>
    /// Represents metrics for an activity
    /// </summary>
    public class ActivityMetrics
    {
        /// <summary>
        /// Gets or sets the name of the activity
        /// </summary>
        public string ActivityName { get; set; }
        
        /// <summary>
        /// Gets or sets the average duration in milliseconds
        /// </summary>
        public double AverageDuration { get; set; }
        
        /// <summary>
        /// Gets or sets the success rate (0.0 to 1.0)
        /// </summary>
        public double SuccessRate { get; set; }
        
        /// <summary>
        /// Gets or sets the error rate (0.0 to 1.0)
        /// </summary>
        public double ErrorRate { get; set; }
        
        /// <summary>
        /// Gets or sets the number of executions
        /// </summary>
        public int ExecutionCount { get; set; }
    }
}
