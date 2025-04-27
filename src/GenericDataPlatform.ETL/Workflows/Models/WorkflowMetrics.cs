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
        /// Gets or sets the number of cancelled executions
        /// </summary>
        public int CancelledExecutions { get; set; }

        /// <summary>
        /// Gets or sets the average duration in milliseconds
        /// </summary>
        public double AverageDuration { get; set; }

        /// <summary>
        /// Gets or sets the average execution time in seconds
        /// </summary>
        public double AverageExecutionTimeInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time in seconds
        /// </summary>
        public double MaxExecutionTimeInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the minimum execution time in seconds
        /// </summary>
        public double MinExecutionTimeInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last execution
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the metrics for each activity
        /// </summary>
        public Dictionary<string, ActivityMetrics> ActivityMetrics { get; set; } = new Dictionary<string, ActivityMetrics>();

        /// <summary>
        /// Gets or sets the metrics for each step
        /// </summary>
        public List<StepMetrics> StepMetrics { get; set; } = new List<StepMetrics>();

        /// <summary>
        /// Gets or sets the common errors for the workflow
        /// </summary>
        public List<ErrorMetrics> CommonErrors { get; set; } = new List<ErrorMetrics>();
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
