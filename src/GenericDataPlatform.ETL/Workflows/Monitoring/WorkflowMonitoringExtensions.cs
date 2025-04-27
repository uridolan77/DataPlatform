using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Monitoring
{
    /// <summary>
    /// Extension methods for workflow monitoring
    /// </summary>
    public static class WorkflowMonitoringExtensions
    {
        /// <summary>
        /// Records a timeline event for a workflow execution
        /// </summary>
        public static async Task RecordTimelineEventAsync(
            this IWorkflowMonitor monitor,
            string executionId,
            string? stepId,
            WorkflowTimelineEventTypes eventType,
            Dictionary<string, object> details = null,
            ILogger? logger = null)
        {
            if (monitor == null)
            {
                return;
            }

            try
            {
                var timelineEvent = new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    StepId = stepId ?? string.Empty,
                    EventTypeString = eventType.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                // Convert details to string format
                if (details != null)
                {
                    timelineEvent.Data = details;
                }

                await monitor.RecordTimelineEventAsync(timelineEvent);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error recording timeline event {EventType} for execution {ExecutionId}", eventType, executionId);
            }
        }

        /// <summary>
        /// Sends a notification for a workflow execution
        /// </summary>
        public static async Task SendNotificationAsync(
            string notificationServiceUrl,
            WorkflowExecution execution,
            string subject,
            string message,
            ILogger logger = null)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationServiceUrl))
                {
                    return;
                }

                using var httpClient = new HttpClient();
                var notification = new
                {
                    ExecutionId = execution.Id,
                    WorkflowId = execution.WorkflowId,
                    Subject = subject,
                    Message = message,
                    Status = execution.Status.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(notification),
                    Encoding.UTF8,
                    "application/json");

                await httpClient.PostAsync(notificationServiceUrl, content);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error sending notification for execution {ExecutionId}", execution.Id);
            }
        }
    }
}
