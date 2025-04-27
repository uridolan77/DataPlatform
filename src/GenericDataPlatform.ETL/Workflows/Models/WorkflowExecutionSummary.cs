using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a summary of a workflow execution
    /// </summary>
    public class WorkflowExecutionSummary
    {
        /// <summary>
        /// Gets or sets the ID of the execution
        /// </summary>
        public string ExecutionId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the workflow
        /// </summary>
        public string WorkflowName { get; set; }
        
        /// <summary>
        /// Gets or sets the version of the workflow
        /// </summary>
        public string WorkflowVersion { get; set; }
        
        /// <summary>
        /// Gets or sets the status of the execution
        /// </summary>
        public WorkflowExecutionStatus Status { get; set; }
        
        /// <summary>
        /// Gets or sets the start time of the execution
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the end time of the execution
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of steps in the execution
        /// </summary>
        public int TotalSteps { get; set; }
        
        /// <summary>
        /// Gets or sets the number of completed steps in the execution
        /// </summary>
        public int CompletedSteps { get; set; }
        
        /// <summary>
        /// Gets or sets the number of failed steps in the execution
        /// </summary>
        public int FailedSteps { get; set; }
        
        /// <summary>
        /// Gets or sets the number of skipped steps in the execution
        /// </summary>
        public int SkippedSteps { get; set; }
        
        /// <summary>
        /// Gets or sets the number of errors in the execution
        /// </summary>
        public int ErrorCount { get; set; }
        
        /// <summary>
        /// Gets or sets the step execution summaries
        /// </summary>
        public List<StepExecutionSummary> StepSummaries { get; set; } = new List<StepExecutionSummary>();
    }
    
    /// <summary>
    /// Represents a summary of a step execution
    /// </summary>
    public class StepExecutionSummary
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
        /// Gets or sets the status of the step execution
        /// </summary>
        public WorkflowStepExecutionStatus Status { get; set; }
        
        /// <summary>
        /// Gets or sets the start time of the step execution
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the end time of the step execution
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the retry count for the step execution
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of errors in the step execution
        /// </summary>
        public int ErrorCount { get; set; }
    }
}
