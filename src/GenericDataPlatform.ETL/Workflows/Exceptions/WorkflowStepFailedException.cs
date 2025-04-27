using System;

namespace GenericDataPlatform.ETL.Workflows.Exceptions
{
    /// <summary>
    /// Exception thrown when a workflow step fails
    /// </summary>
    public class WorkflowStepFailedException : Exception
    {
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }
        
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
        public string StepType { get; set; }
        
        /// <summary>
        /// Gets or sets the retry count
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of retries
        /// </summary>
        public int MaxRetries { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the WorkflowStepFailedException class
        /// </summary>
        public WorkflowStepFailedException() : base()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the WorkflowStepFailedException class with a specified error message
        /// </summary>
        public WorkflowStepFailedException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the WorkflowStepFailedException class with a specified error message and a reference to the inner exception that is the cause of this exception
        /// </summary>
        public WorkflowStepFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the WorkflowStepFailedException class with a specified error message, workflow ID, execution ID, step ID, and step name
        /// </summary>
        public WorkflowStepFailedException(string message, string workflowId, string executionId, string stepId, string stepName) : base(message)
        {
            WorkflowId = workflowId;
            ExecutionId = executionId;
            StepId = stepId;
            StepName = stepName;
        }
        
        /// <summary>
        /// Initializes a new instance of the WorkflowStepFailedException class with a specified error message, workflow ID, execution ID, step ID, step name, and inner exception
        /// </summary>
        public WorkflowStepFailedException(string message, string workflowId, string executionId, string stepId, string stepName, Exception innerException) : base(message, innerException)
        {
            WorkflowId = workflowId;
            ExecutionId = executionId;
            StepId = stepId;
            StepName = stepName;
        }
    }
}
