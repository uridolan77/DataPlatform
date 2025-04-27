using System;

namespace GenericDataPlatform.ETL.Workflows.Exceptions
{
    /// <summary>
    /// Exception thrown when a workflow execution fails
    /// </summary>
    public class WorkflowExecutionFailedException : Exception
    {
        public WorkflowExecutionFailedException(string message) : base(message)
        {
        }

        public WorkflowExecutionFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a workflow validation fails
    /// </summary>
    public class WorkflowValidationException : Exception
    {
        public WorkflowValidationException(string message) : base(message)
        {
        }

        public WorkflowValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
