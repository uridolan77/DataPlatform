using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a timeline event for a workflow execution
    /// </summary>
    public class WorkflowTimelineEvent
    {
        /// <summary>
        /// Gets or sets the ID of the event
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the ID of the workflow execution
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string StepId { get; set; }

        /// <summary>
        /// Gets or sets the name of the activity
        /// </summary>
        public string ActivityName { get; set; }

        /// <summary>
        /// Gets or sets the type of event
        /// </summary>
        public WorkflowTimelineEventTypes EventType { get; set; }

        /// <summary>
        /// Gets or sets the event type as a string
        /// </summary>
        public string EventTypeString
        {
            get => EventType.ToString();
            set => EventType = value != null ? System.Enum.Parse<WorkflowTimelineEventTypes>(value) : WorkflowTimelineEventTypes.Custom;
        }

        /// <summary>
        /// Gets or sets the timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the message for the event
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the details of the event
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Gets or sets additional data for the event
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
