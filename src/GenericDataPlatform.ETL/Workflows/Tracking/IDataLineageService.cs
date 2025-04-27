using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericDataPlatform.ETL.Workflows.Tracking
{
    /// <summary>
    /// Service for tracking data lineage
    /// </summary>
    public interface IDataLineageService
    {
        /// <summary>
        /// Records a data lineage event
        /// </summary>
        Task<string> RecordLineageEventAsync(LineageEvent lineageEvent);
        
        /// <summary>
        /// Gets lineage events for a data entity
        /// </summary>
        Task<List<LineageEvent>> GetLineageEventsAsync(string entityId, string entityType = null);
        
        /// <summary>
        /// Gets the lineage graph for a data entity
        /// </summary>
        Task<LineageGraph> GetLineageGraphAsync(string entityId, string entityType = null, int depth = 3);
    }
    
    /// <summary>
    /// Represents a data lineage event
    /// </summary>
    public class LineageEvent
    {
        /// <summary>
        /// Gets or sets the ID of the lineage event
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the source entity
        /// </summary>
        public string SourceEntityId { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the source entity
        /// </summary>
        public string SourceEntityType { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the target entity
        /// </summary>
        public string TargetEntityId { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the target entity
        /// </summary>
        public string TargetEntityType { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the lineage event
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the lineage event
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the user who performed the action
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the lineage event
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
    }
    
    /// <summary>
    /// Represents a data lineage graph
    /// </summary>
    public class LineageGraph
    {
        /// <summary>
        /// Gets or sets the nodes in the lineage graph
        /// </summary>
        public List<LineageNode> Nodes { get; set; }
        
        /// <summary>
        /// Gets or sets the edges in the lineage graph
        /// </summary>
        public List<LineageEdge> Edges { get; set; }
    }
    
    /// <summary>
    /// Represents a node in a data lineage graph
    /// </summary>
    public class LineageNode
    {
        /// <summary>
        /// Gets or sets the ID of the node
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the node
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the node
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets additional properties for the node
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
    
    /// <summary>
    /// Represents an edge in a data lineage graph
    /// </summary>
    public class LineageEdge
    {
        /// <summary>
        /// Gets or sets the ID of the edge
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the source node
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the target node
        /// </summary>
        public string TargetId { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the edge
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets additional properties for the edge
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
}
