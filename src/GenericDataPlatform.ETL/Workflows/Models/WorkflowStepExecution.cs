using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a workflow step execution
    /// </summary>
    public class WorkflowStepExecution
    {
        /// <summary>
        /// Gets or sets the ID of the step execution
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the execution
        /// </summary>
        public string ExecutionId { get; set; }
        
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
        /// Gets or sets the error message for the step execution
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the error details for the step execution
        /// </summary>
        public string ErrorDetails { get; set; }
        
        /// <summary>
        /// Gets or sets the input data for the step execution
        /// </summary>
        public object InputData { get; set; }
        
        /// <summary>
        /// Gets or sets the output data for the step execution
        /// </summary>
        public object OutputData { get; set; }
        
        /// <summary>
        /// Gets or sets the metadata for the step execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
