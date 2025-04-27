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
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the workflow execution
        /// </summary>
        public string ExecutionId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the activity
        /// </summary>
        public string ActivityName { get; set; }
        
        /// <summary>
        /// Gets or sets the type of event
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets additional data for the event
        /// </summary>
        public Dictionary<string, object> Data { get; set; }
    }
}
