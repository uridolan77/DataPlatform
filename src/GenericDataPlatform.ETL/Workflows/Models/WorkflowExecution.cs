using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a workflow execution
    /// </summary>
    public class WorkflowExecution
    {
        /// <summary>
        /// Gets or sets the ID of the execution
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }

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
        /// Gets or sets the start time of the execution (alias for StartTime)
        /// </summary>
        public DateTime StartedAt
        {
            get => StartTime;
            set => StartTime = value;
        }

        /// <summary>
        /// Gets or sets the completion time of the execution (alias for EndTime)
        /// </summary>
        public DateTime? CompletedAt
        {
            get => EndTime;
            set => EndTime = value;
        }

        /// <summary>
        /// Gets or sets the parameters for the execution
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the input data for the execution
        /// </summary>
        public Dictionary<string, object> Input { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the output data for the execution
        /// </summary>
        public Dictionary<string, object> Output { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the variables for the execution
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the step executions for the execution
        /// </summary>
        public List<WorkflowStepExecution> StepExecutions { get; set; } = new List<WorkflowStepExecution>();

        /// <summary>
        /// Gets or sets the errors that occurred during execution
        /// </summary>
        public List<WorkflowExecutionError> Errors { get; set; } = new List<WorkflowExecutionError>();

        /// <summary>
        /// Gets or sets the error message for the execution
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the error details for the execution
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Gets or sets the trigger type for the execution
        /// </summary>
        public string TriggerType { get; set; }
    }
}
