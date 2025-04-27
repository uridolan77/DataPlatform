using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Tracking
{
    /// <summary>
    /// Represents a data lineage event
    /// </summary>
    public class LineageEvent
    {
        /// <summary>
        /// Gets or sets the ID of the event
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the type of the event
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the source entity
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the target entity
        /// </summary>
        public string TargetId { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets additional properties for the event
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
