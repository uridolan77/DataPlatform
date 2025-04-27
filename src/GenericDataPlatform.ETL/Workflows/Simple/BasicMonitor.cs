using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Monitoring;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Simple
{
    /// <summary>
    /// Basic implementation of IWorkflowMonitor
    /// </summary>
    public class BasicMonitor : IWorkflowMonitor
    {
        private readonly ILogger<BasicMonitor> _logger;
        private readonly List<WorkflowTimelineEvent> _events = new();

        /// <summary>
        /// Initializes a new instance of the BasicMonitor class
        /// </summary>
        public BasicMonitor(ILogger<BasicMonitor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets workflow metrics
        /// </summary>
        public Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId)
        {
            _logger.LogInformation("Getting metrics for workflow {WorkflowId}", workflowId);

            // Create sample metrics
            var metrics = new WorkflowMetrics
            {
                WorkflowId = workflowId,
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                AverageDuration = 1500, // milliseconds
                LastExecutionTime = DateTime.UtcNow.AddHours(-1),
                ActivityMetrics = new Dictionary<string, ActivityMetrics>
                {
                    ["Extract"] = new ActivityMetrics
                    {
                        ActivityName = "Extract",
                        AverageDuration = 500,
                        SuccessRate = 0.98,
                        ErrorRate = 0.02,
                        ExecutionCount = 100
                    },
                    ["Transform"] = new ActivityMetrics
                    {
                        ActivityName = "Transform",
                        AverageDuration = 300,
                        SuccessRate = 0.97,
                        ErrorRate = 0.03,
                        ExecutionCount = 100
                    },
                    ["Load"] = new ActivityMetrics
                    {
                        ActivityName = "Load",
                        AverageDuration = 700,
                        SuccessRate = 0.95,
                        ErrorRate = 0.05,
                        ExecutionCount = 100
                    }
                }
            };

            return Task.FromResult(metrics);
        }

        /// <summary>
        /// Gets timeline events for a workflow execution
        /// </summary>
        public Task<List<WorkflowTimelineEvent>> GetTimelineEventsAsync(string executionId, int limit = 100)
        {
            _logger.LogInformation("Getting timeline events for execution {ExecutionId}", executionId);

            // Return events from memory
            var events = _events
                .Where(e => e.ExecutionId == executionId)
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToList();

            // If no events found, create sample events
            if (!events.Any())
            {
                var startTime = DateTime.UtcNow.AddHours(-1);

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Workflow",
                    EventType = "Started",
                    Timestamp = startTime,
                    Data = new Dictionary<string, object>
                    {
                        ["input"] = new { param1 = "value1", param2 = 123 }
                    }
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Extract",
                    EventType = "Started",
                    Timestamp = startTime.AddSeconds(1),
                    Data = null
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Extract",
                    EventType = "Completed",
                    Timestamp = startTime.AddSeconds(6),
                    Data = new Dictionary<string, object>
                    {
                        ["recordsExtracted"] = 1000
                    }
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Transform",
                    EventType = "Started",
                    Timestamp = startTime.AddSeconds(7),
                    Data = null
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Transform",
                    EventType = "Completed",
                    Timestamp = startTime.AddSeconds(10),
                    Data = new Dictionary<string, object>
                    {
                        ["recordsTransformed"] = 1000
                    }
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Load",
                    EventType = "Started",
                    Timestamp = startTime.AddSeconds(11),
                    Data = null
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Load",
                    EventType = "Completed",
                    Timestamp = startTime.AddSeconds(18),
                    Data = new Dictionary<string, object>
                    {
                        ["recordsLoaded"] = 1000
                    }
                });

                events.Add(new WorkflowTimelineEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionId = executionId,
                    ActivityName = "Workflow",
                    EventType = "Completed",
                    Timestamp = startTime.AddSeconds(19),
                    Data = new Dictionary<string, object>
                    {
                        ["output"] = new { result = "success", recordsProcessed = 1000 }
                    }
                });
            }

            return Task.FromResult(events);
        }

        /// <summary>
        /// Records a workflow timeline event
        /// </summary>
        public Task RecordTimelineEventAsync(WorkflowTimelineEvent timelineEvent)
        {
            _logger.LogInformation("Recording event {EventType} for activity {ActivityName} in execution {ExecutionId}",
                timelineEvent.EventType, timelineEvent.ActivityName, timelineEvent.ExecutionId);

            // Generate an ID if not provided
            if (string.IsNullOrEmpty(timelineEvent.Id))
            {
                timelineEvent.Id = Guid.NewGuid().ToString();
            }

            // Store the event in memory
            lock (_events)
            {
                _events.Add(timelineEvent);

                // Limit the number of events in memory
                if (_events.Count > 1000)
                {
                    _events.RemoveAt(0);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates workflow metrics based on a workflow execution
        /// </summary>
        public Task UpdateWorkflowMetricsAsync(WorkflowExecution execution)
        {
            _logger.LogInformation("Updating metrics for workflow {WorkflowId} based on execution {ExecutionId}",
                execution.WorkflowId, execution.Id);

            // In a real implementation, this would update metrics in a database
            return Task.CompletedTask;
        }
    }
}
