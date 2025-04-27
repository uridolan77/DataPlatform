using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents metrics for a workflow step
    /// </summary>
    public class StepMetrics
    {
        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string StepId { get; set; }

        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string StepName { get; set; }

        /// <summary>
        /// Gets or sets the type of the step
        /// </summary>
        public WorkflowStepType StepType { get; set; }

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
        /// Gets or sets the maximum duration in milliseconds
        /// </summary>
        public double MaxDuration { get; set; }

        /// <summary>
        /// Gets or sets the minimum duration in milliseconds
        /// </summary>
        public double MinDuration { get; set; }

        /// <summary>
        /// Gets or sets the average number of records processed
        /// </summary>
        public double AverageRecordsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of records processed
        /// </summary>
        public long MaxRecordsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped executions
        /// </summary>
        public int SkippedExecutions { get; set; }

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
        /// Gets or sets the average retry count
        /// </summary>
        public double AverageRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the common errors for the step
        /// </summary>
        public List<ErrorMetrics> CommonErrors { get; set; } = new List<ErrorMetrics>();
    }
}
