using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models.Lineage
{
    /// <summary>
    /// Data entity
    /// </summary>
    public class DataEntity
    {
        /// <summary>
        /// ID of the entity
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Type of the entity (e.g., Table, File, API)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Name of the entity
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the entity
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// When the entity was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the entity was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Additional properties of the entity
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Lineage event
    /// </summary>
    public class LineageEvent
    {
        /// <summary>
        /// ID of the event
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Type of the event (e.g., Read, Write, Transform)
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// ID of the source entity
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// ID of the target entity
        /// </summary>
        public string TargetId { get; set; }
        
        /// <summary>
        /// When the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// User who performed the action
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Process that performed the action
        /// </summary>
        public string ProcessId { get; set; }
        
        /// <summary>
        /// Additional properties of the event
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Lineage graph
    /// </summary>
    public class LineageGraph
    {
        /// <summary>
        /// ID of the root entity
        /// </summary>
        public string RootEntityId { get; set; }
        
        /// <summary>
        /// Direction of the graph
        /// </summary>
        public LineageDirection Direction { get; set; }
        
        /// <summary>
        /// Depth of the graph
        /// </summary>
        public int Depth { get; set; }
        
        /// <summary>
        /// Nodes in the graph
        /// </summary>
        public List<LineageNode> Nodes { get; set; } = new List<LineageNode>();
        
        /// <summary>
        /// Edges in the graph
        /// </summary>
        public List<LineageEdge> Edges { get; set; } = new List<LineageEdge>();
    }
    
    /// <summary>
    /// Lineage node
    /// </summary>
    public class LineageNode
    {
        /// <summary>
        /// ID of the node
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Type of the node
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Additional properties of the node
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Lineage edge
    /// </summary>
    public class LineageEdge
    {
        /// <summary>
        /// ID of the edge
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// ID of the source node
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// ID of the target node
        /// </summary>
        public string TargetId { get; set; }
        
        /// <summary>
        /// Type of the edge
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Additional properties of the edge
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Impact analysis
    /// </summary>
    public class ImpactAnalysis
    {
        /// <summary>
        /// ID of the entity
        /// </summary>
        public string EntityId { get; set; }
        
        /// <summary>
        /// Name of the entity
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// Type of the entity
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// When the analysis was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Entities impacted by changes to the entity
        /// </summary>
        public List<ImpactedEntity> ImpactedEntities { get; set; } = new List<ImpactedEntity>();
    }
    
    /// <summary>
    /// Impacted entity
    /// </summary>
    public class ImpactedEntity
    {
        /// <summary>
        /// ID of the entity
        /// </summary>
        public string EntityId { get; set; }
        
        /// <summary>
        /// Name of the entity
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// Type of the entity
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// Level of impact
        /// </summary>
        public ImpactLevel ImpactLevel { get; set; }
    }
    
    /// <summary>
    /// Lineage direction
    /// </summary>
    public enum LineageDirection
    {
        Upstream,
        Downstream,
        Both
    }
    
    /// <summary>
    /// Impact level
    /// </summary>
    public enum ImpactLevel
    {
        Low,
        Medium,
        High
    }
}
