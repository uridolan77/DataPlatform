using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.ETL.Workflows.Monitoring
{
    /// <summary>
    /// Implementation of the workflow monitor
    /// </summary>
    public class WorkflowMonitor : IWorkflowMonitor
    {
        private readonly IWorkflowRepository _repository;
        private readonly ILogger<WorkflowMonitor> _logger;
        private readonly WorkflowOptions _options;
        private readonly List<WorkflowTimelineEvent> _timelineEvents = new List<WorkflowTimelineEvent>();

        public WorkflowMonitor(
            IWorkflowRepository repository,
            IOptions<WorkflowOptions> options,
            ILogger<WorkflowMonitor> logger)
        {
            _repository = repository;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Records a timeline event for a workflow execution
        /// </summary>
        public async Task RecordTimelineEventAsync(WorkflowTimelineEvent timelineEvent)
        {
            try
            {
                // Generate an ID for the event if not provided
                if (string.IsNullOrEmpty(timelineEvent.Id))
                {
                    timelineEvent.Id = Guid.NewGuid().ToString();
                }

                // Store the event in memory
                lock (_timelineEvents)
                {
                    _timelineEvents.Add(timelineEvent);

                    // Limit the number of events in memory
                    if (_timelineEvents.Count > 1000)
                    {
                        _timelineEvents.RemoveAt(0);
                    }
                }

                // In a real implementation, this would store the event in a database
                // For now, we'll just log it
                _logger.LogInformation("Timeline event: {EventType} for execution {ExecutionId}, activity {ActivityName}",
                    timelineEvent.EventType, timelineEvent.ExecutionId, timelineEvent.ActivityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording timeline event");
            }
        }

        /// <summary>
        /// Updates workflow metrics based on an execution
        /// </summary>
        public async Task UpdateWorkflowMetricsAsync(WorkflowExecution execution)
        {
            try
            {
                if (!_options.EnableMetricsCollection)
                {
                    return;
                }

                // Get current metrics
                var metrics = await _repository.GetWorkflowMetricsAsync(execution.WorkflowId);

                // In a real implementation, this would update the metrics in a database
                // For now, we'll just log it
                _logger.LogInformation("Updated metrics for workflow {WorkflowId}: {SuccessfulExecutions} successful, {FailedExecutions} failed",
                    execution.WorkflowId, metrics.SuccessfulExecutions, metrics.FailedExecutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow metrics");
            }
        }

        /// <summary>
        /// Gets workflow metrics
        /// </summary>
        public async Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId)
        {
            try
            {
                return await _repository.GetWorkflowMetricsAsync(workflowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow metrics");
                throw;
            }
        }

        /// <summary>
        /// Gets timeline events for a workflow execution
        /// </summary>
        public async Task<List<WorkflowTimelineEvent>> GetTimelineEventsAsync(string executionId, int limit = 100)
        {
            try
            {
                // In a real implementation, this would query a database
                // For now, we'll return the in-memory events
                lock (_timelineEvents)
                {
                    return _timelineEvents
                        .Where(e => e.ExecutionId == executionId)
                        .OrderByDescending(e => e.Timestamp)
                        .Take(limit)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timeline events");
                throw;
            }
        }
    }
}
