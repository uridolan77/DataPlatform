using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Lineage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Service for tracking data lineage
    /// </summary>
    public class DataLineageService : IDataLineageService
    {
        private readonly SecurityOptions _options;
        private readonly IDataLineageRepository _repository;
        private readonly ILogger<DataLineageService> _logger;

        public DataLineageService(
            IOptions<SecurityOptions> options,
            IDataLineageRepository repository,
            ILogger<DataLineageService> logger)
        {
            _options = options.Value;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Records a data lineage event
        /// </summary>
        public async Task<string> RecordLineageEventAsync(LineageEvent lineageEvent)
        {
            try
            {
                _logger.LogInformation("Recording lineage event: {EventType} for {SourceId} to {TargetId}",
                    lineageEvent.EventType, lineageEvent.SourceId, lineageEvent.TargetId);
                
                // Ensure required fields are set
                lineageEvent.Id ??= Guid.NewGuid().ToString();
                lineageEvent.Timestamp = lineageEvent.Timestamp == default ? DateTime.UtcNow : lineageEvent.Timestamp;
                
                // Record the event
                await _repository.SaveLineageEventAsync(lineageEvent);
                
                return lineageEvent.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording lineage event: {EventType} for {SourceId} to {TargetId}",
                    lineageEvent.EventType, lineageEvent.SourceId, lineageEvent.TargetId);
                throw;
            }
        }

        /// <summary>
        /// Gets lineage events for a data entity
        /// </summary>
        public async Task<List<LineageEvent>> GetLineageEventsAsync(string entityId, LineageDirection direction = LineageDirection.Both, DateTime? startTime = null, DateTime? endTime = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting lineage events for entity {EntityId} with direction {Direction}", entityId, direction);
                
                var events = await _repository.GetLineageEventsAsync(entityId, direction, startTime, endTime, limit);
                
                _logger.LogInformation("Found {EventCount} lineage events for entity {EntityId}", events.Count, entityId);
                
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage events for entity {EntityId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// Gets the lineage graph for a data entity
        /// </summary>
        public async Task<LineageGraph> GetLineageGraphAsync(string entityId, LineageDirection direction = LineageDirection.Both, int depth = 3)
        {
            try
            {
                _logger.LogInformation("Getting lineage graph for entity {EntityId} with direction {Direction} and depth {Depth}",
                    entityId, direction, depth);
                
                var graph = new LineageGraph
                {
                    RootEntityId = entityId,
                    Direction = direction,
                    Depth = depth,
                    Nodes = new List<LineageNode>(),
                    Edges = new List<LineageEdge>()
                };
                
                // Get the root entity
                var rootEntity = await _repository.GetDataEntityAsync(entityId);
                if (rootEntity == null)
                {
                    _logger.LogWarning("Entity {EntityId} not found", entityId);
                    return graph;
                }
                
                // Add the root node
                graph.Nodes.Add(new LineageNode
                {
                    Id = rootEntity.Id,
                    Type = rootEntity.Type,
                    Name = rootEntity.Name,
                    Properties = rootEntity.Properties
                });
                
                // Build the graph
                await BuildLineageGraphAsync(graph, entityId, direction, depth);
                
                _logger.LogInformation("Built lineage graph for entity {EntityId} with {NodeCount} nodes and {EdgeCount} edges",
                    entityId, graph.Nodes.Count, graph.Edges.Count);
                
                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage graph for entity {EntityId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// Gets the impact analysis for a data entity
        /// </summary>
        public async Task<ImpactAnalysis> GetImpactAnalysisAsync(string entityId, int depth = 3)
        {
            try
            {
                _logger.LogInformation("Getting impact analysis for entity {EntityId} with depth {Depth}", entityId, depth);
                
                // Get downstream lineage graph
                var graph = await GetLineageGraphAsync(entityId, LineageDirection.Downstream, depth);
                
                var analysis = new ImpactAnalysis
                {
                    EntityId = entityId,
                    Timestamp = DateTime.UtcNow,
                    ImpactedEntities = new List<ImpactedEntity>()
                };
                
                // Get the entity
                var entity = await _repository.GetDataEntityAsync(entityId);
                if (entity == null)
                {
                    _logger.LogWarning("Entity {EntityId} not found", entityId);
                    return analysis;
                }
                
                analysis.EntityName = entity.Name;
                analysis.EntityType = entity.Type;
                
                // Analyze impact
                foreach (var node in graph.Nodes.Where(n => n.Id != entityId))
                {
                    var impactedEntity = new ImpactedEntity
                    {
                        EntityId = node.Id,
                        EntityName = node.Name,
                        EntityType = node.Type,
                        ImpactLevel = CalculateImpactLevel(graph, entityId, node.Id)
                    };
                    
                    analysis.ImpactedEntities.Add(impactedEntity);
                }
                
                // Sort by impact level
                analysis.ImpactedEntities = analysis.ImpactedEntities
                    .OrderByDescending(e => e.ImpactLevel)
                    .ToList();
                
                _logger.LogInformation("Completed impact analysis for entity {EntityId} with {ImpactedCount} impacted entities",
                    entityId, analysis.ImpactedEntities.Count);
                
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impact analysis for entity {EntityId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// Gets data entities by type
        /// </summary>
        public async Task<List<DataEntity>> GetDataEntitiesByTypeAsync(string entityType, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting data entities of type {EntityType}", entityType);
                
                var entities = await _repository.GetDataEntitiesByTypeAsync(entityType, limit);
                
                _logger.LogInformation("Found {EntityCount} entities of type {EntityType}", entities.Count, entityType);
                
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entities of type {EntityType}", entityType);
                throw;
            }
        }

        /// <summary>
        /// Searches for data entities
        /// </summary>
        public async Task<List<DataEntity>> SearchDataEntitiesAsync(string searchTerm, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for data entities with term {SearchTerm}", searchTerm);
                
                var entities = await _repository.SearchDataEntitiesAsync(searchTerm, limit);
                
                _logger.LogInformation("Found {EntityCount} entities matching search term {SearchTerm}", entities.Count, searchTerm);
                
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data entities with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        /// <summary>
        /// Builds a lineage graph recursively
        /// </summary>
        private async Task BuildLineageGraphAsync(LineageGraph graph, string entityId, LineageDirection direction, int depth, int currentDepth = 0)
        {
            if (currentDepth >= depth)
            {
                return;
            }
            
            // Get lineage events
            var events = await _repository.GetLineageEventsAsync(entityId, direction);
            
            foreach (var lineageEvent in events)
            {
                string connectedEntityId;
                bool isUpstream;
                
                if (lineageEvent.SourceId == entityId)
                {
                    connectedEntityId = lineageEvent.TargetId;
                    isUpstream = false;
                }
                else
                {
                    connectedEntityId = lineageEvent.SourceId;
                    isUpstream = true;
                }
                
                // Skip if we're only interested in one direction and this is the other
                if ((direction == LineageDirection.Upstream && !isUpstream) ||
                    (direction == LineageDirection.Downstream && isUpstream))
                {
                    continue;
                }
                
                // Get the connected entity
                var connectedEntity = await _repository.GetDataEntityAsync(connectedEntityId);
                if (connectedEntity == null)
                {
                    continue;
                }
                
                // Add the node if it doesn't exist
                if (!graph.Nodes.Any(n => n.Id == connectedEntityId))
                {
                    graph.Nodes.Add(new LineageNode
                    {
                        Id = connectedEntity.Id,
                        Type = connectedEntity.Type,
                        Name = connectedEntity.Name,
                        Properties = connectedEntity.Properties
                    });
                }
                
                // Add the edge if it doesn't exist
                var edgeId = isUpstream
                    ? $"{connectedEntityId}->{entityId}"
                    : $"{entityId}->{connectedEntityId}";
                
                if (!graph.Edges.Any(e => e.Id == edgeId))
                {
                    graph.Edges.Add(new LineageEdge
                    {
                        Id = edgeId,
                        SourceId = isUpstream ? connectedEntityId : entityId,
                        TargetId = isUpstream ? entityId : connectedEntityId,
                        Type = lineageEvent.EventType,
                        Properties = lineageEvent.Properties
                    });
                }
                
                // Recursively build the graph
                await BuildLineageGraphAsync(graph, connectedEntityId, direction, depth, currentDepth + 1);
            }
        }

        /// <summary>
        /// Calculates the impact level of an entity
        /// </summary>
        private ImpactLevel CalculateImpactLevel(LineageGraph graph, string sourceId, string targetId)
        {
            // Calculate the distance between the source and target
            var distance = CalculateDistance(graph, sourceId, targetId);
            
            // Calculate the impact level based on distance and entity type
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == targetId);
            if (targetNode == null)
            {
                return ImpactLevel.Low;
            }
            
            // Critical entities are always high impact
            if (targetNode.Properties.ContainsKey("criticality") &&
                targetNode.Properties["criticality"].ToString().Equals("critical", StringComparison.OrdinalIgnoreCase))
            {
                return ImpactLevel.High;
            }
            
            // Impact decreases with distance
            if (distance == 1)
            {
                return ImpactLevel.High;
            }
            else if (distance == 2)
            {
                return ImpactLevel.Medium;
            }
            else
            {
                return ImpactLevel.Low;
            }
        }

        /// <summary>
        /// Calculates the shortest distance between two nodes in the graph
        /// </summary>
        private int CalculateDistance(LineageGraph graph, string sourceId, string targetId)
        {
            // Simple BFS to find the shortest path
            var visited = new HashSet<string>();
            var queue = new Queue<(string Id, int Distance)>();
            
            queue.Enqueue((sourceId, 0));
            visited.Add(sourceId);
            
            while (queue.Count > 0)
            {
                var (currentId, distance) = queue.Dequeue();
                
                if (currentId == targetId)
                {
                    return distance;
                }
                
                // Find all connected nodes
                var edges = graph.Edges.Where(e => e.SourceId == currentId).ToList();
                
                foreach (var edge in edges)
                {
                    if (!visited.Contains(edge.TargetId))
                    {
                        visited.Add(edge.TargetId);
                        queue.Enqueue((edge.TargetId, distance + 1));
                    }
                }
            }
            
            // No path found
            return int.MaxValue;
        }
    }

    /// <summary>
    /// Interface for data lineage service
    /// </summary>
    public interface IDataLineageService
    {
        Task<string> RecordLineageEventAsync(LineageEvent lineageEvent);
        Task<List<LineageEvent>> GetLineageEventsAsync(string entityId, LineageDirection direction = LineageDirection.Both, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<LineageGraph> GetLineageGraphAsync(string entityId, LineageDirection direction = LineageDirection.Both, int depth = 3);
        Task<ImpactAnalysis> GetImpactAnalysisAsync(string entityId, int depth = 3);
        Task<List<DataEntity>> GetDataEntitiesByTypeAsync(string entityType, int limit = 100);
        Task<List<DataEntity>> SearchDataEntitiesAsync(string searchTerm, int limit = 100);
    }
}
