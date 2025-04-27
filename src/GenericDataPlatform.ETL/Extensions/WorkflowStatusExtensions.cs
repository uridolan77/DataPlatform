using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Extensions
{
    /// <summary>
    /// Extension methods for workflow status enums
    /// </summary>
    public static class WorkflowStatusExtensions
    {
        /// <summary>
        /// Converts a WorkflowStatus to a WorkflowExecutionStatus
        /// </summary>
        public static WorkflowExecutionStatus ToExecutionStatus(this WorkflowStatus status)
        {
            return status switch
            {
                WorkflowStatus.Idle => WorkflowExecutionStatus.Pending,
                WorkflowStatus.Running => WorkflowExecutionStatus.Running,
                WorkflowStatus.Paused => WorkflowExecutionStatus.Paused,
                WorkflowStatus.Completed => WorkflowExecutionStatus.Completed,
                WorkflowStatus.Failed => WorkflowExecutionStatus.Failed,
                WorkflowStatus.Cancelled => WorkflowExecutionStatus.Cancelled,
                WorkflowStatus.Unknown => WorkflowExecutionStatus.Failed,
                _ => WorkflowExecutionStatus.Failed
            };
        }
        
        /// <summary>
        /// Converts a WorkflowExecutionStatus to a WorkflowStatus
        /// </summary>
        public static WorkflowStatus ToWorkflowStatus(this WorkflowExecutionStatus status)
        {
            return status switch
            {
                WorkflowExecutionStatus.Pending => WorkflowStatus.Idle,
                WorkflowExecutionStatus.Running => WorkflowStatus.Running,
                WorkflowExecutionStatus.Paused => WorkflowStatus.Paused,
                WorkflowExecutionStatus.Completed => WorkflowStatus.Completed,
                WorkflowExecutionStatus.Failed => WorkflowStatus.Failed,
                WorkflowExecutionStatus.Cancelled => WorkflowStatus.Cancelled,
                _ => WorkflowStatus.Unknown
            };
        }
    }
}
