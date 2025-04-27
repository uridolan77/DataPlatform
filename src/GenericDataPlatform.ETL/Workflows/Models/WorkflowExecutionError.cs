using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents an error that occurred during workflow execution
    /// </summary>
    public class WorkflowExecutionError
    {
        /// <summary>
        /// Gets or sets the ID of the error
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the time the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the ID of the step where the error occurred
        /// </summary>
        public string StepId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the step where the error occurred
        /// </summary>
        public string StepName { get; set; }
        
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the error details
        /// </summary>
        public string Details { get; set; }
        
        /// <summary>
        /// Gets or sets the error type
        /// </summary>
        public string ErrorType { get; set; }
        
        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public string ErrorCode { get; set; }
        
        /// <summary>
        /// Gets or sets the stack trace
        /// </summary>
        public string StackTrace { get; set; }
        
        /// <summary>
        /// Gets or sets additional data about the error
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets whether the error was handled
        /// </summary>
        public bool IsHandled { get; set; }
        
        /// <summary>
        /// Gets or sets how the error was handled
        /// </summary>
        public string HandledBy { get; set; }
        
        /// <summary>
        /// Gets or sets the action taken to handle the error
        /// </summary>
        public WorkflowErrorAction Action { get; set; }
    }
}
