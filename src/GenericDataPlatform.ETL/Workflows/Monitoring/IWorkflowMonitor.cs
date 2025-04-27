using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Monitoring
{
    /// <summary>
    /// Interface for monitoring workflows
    /// </summary>
    public interface IWorkflowMonitor
    {
        /// <summary>
        /// Gets workflow metrics
        /// </summary>
        Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId);

        /// <summary>
        /// Gets timeline events for a workflow execution
        /// </summary>
        Task<List<WorkflowTimelineEvent>> GetTimelineEventsAsync(string executionId, int limit = 100);

        /// <summary>
        /// Records a workflow timeline event
        /// </summary>
        Task RecordTimelineEventAsync(WorkflowTimelineEvent timelineEvent);

        /// <summary>
        /// Updates workflow metrics based on a workflow execution
        /// </summary>
        Task UpdateWorkflowMetricsAsync(WorkflowExecution execution);
    }
}
