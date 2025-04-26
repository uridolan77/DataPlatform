using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Lineage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Repository for storing and retrieving data lineage information
    /// </summary>
    public class DataLineageRepository : IDataLineageRepository
    {
        private readonly SecurityOptions _options;
        private readonly ILogger<DataLineageRepository> _logger;
        private readonly string _dataDirectory;

        public DataLineageRepository(IOptions<SecurityOptions> options, ILogger<DataLineageRepository> logger)
        {
            _options = options.Value;
            _logger = logger;
            
            // Create data directory if it doesn't exist
            _dataDirectory = _options.DataDirectory ?? Path.Combine(AppContext.BaseDirectory, "SecurityData");
            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(Path.Combine(_dataDirectory, "Lineage"));
            Directory.CreateDirectory(Path.Combine(_dataDirectory, "Lineage", "Events"));
            Directory.CreateDirectory(Path.Combine(_dataDirectory, "Lineage", "Entities"));
        }

        /// <summary>
        /// Saves a data entity
        /// </summary>
        public async Task SaveDataEntityAsync(DataEntity entity)
        {
            try
            {
                // Ensure ID is set
                entity.Id ??= Guid.NewGuid().ToString();
                
                // Save entity
                var filePath = Path.Combine(_dataDirectory, "Lineage", "Entities", $"{entity.Id}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(entity, options));
                
                _logger.LogInformation("Saved data entity {EntityId} of type {EntityType}", entity.Id, entity.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving data entity {EntityId}", entity.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets a data entity by ID
        /// </summary>
        public async Task<DataEntity> GetDataEntityAsync(string entityId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "Lineage", "Entities", $"{entityId}.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Data entity {EntityId} not found", entityId);
                    return null;
                }
                
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<DataEntity>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entity {EntityId}", entityId);
                return null;
            }
        }

        /// <summary>
        /// Gets data entities by type
        /// </summary>
        public async Task<List<DataEntity>> GetDataEntitiesByTypeAsync(string entityType, int limit = 100)
        {
            try
            {
                var entities = new List<DataEntity>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Lineage", "Entities"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    if (entities.Count >= limit)
                    {
                        break;
                    }
                    
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var entity = JsonSerializer.Deserialize<DataEntity>(json);
                        
                        if (entity != null && entity.Type == entityType)
                        {
                            entities.Add(entity);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading entity file {FilePath}", file.FullName);
                    }
                }
                
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entities of type {EntityType}", entityType);
                return new List<DataEntity>();
            }
        }

        /// <summary>
        /// Searches for data entities
        /// </summary>
        public async Task<List<DataEntity>> SearchDataEntitiesAsync(string searchTerm, int limit = 100)
        {
            try
            {
                var entities = new List<DataEntity>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Lineage", "Entities"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    if (entities.Count >= limit)
                    {
                        break;
                    }
                    
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var entity = JsonSerializer.Deserialize<DataEntity>(json);
                        
                        if (entity != null && 
                            (entity.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                             entity.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        {
                            entities.Add(entity);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading entity file {FilePath}", file.FullName);
                    }
                }
                
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data entities with term {SearchTerm}", searchTerm);
                return new List<DataEntity>();
            }
        }

        /// <summary>
        /// Saves a lineage event
        /// </summary>
        public async Task SaveLineageEventAsync(LineageEvent lineageEvent)
        {
            try
            {
                // Ensure ID is set
                lineageEvent.Id ??= Guid.NewGuid().ToString();
                
                // Save event
                var filePath = Path.Combine(_dataDirectory, "Lineage", "Events", $"{lineageEvent.Id}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(lineageEvent, options));
                
                _logger.LogInformation("Saved lineage event {EventId} of type {EventType}", lineageEvent.Id, lineageEvent.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving lineage event {EventId}", lineageEvent.Id);
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
                var events = new List<LineageEvent>();
                var directory = new DirectoryInfo(Path.Combine(_dataDirectory, "Lineage", "Events"));
                
                foreach (var file in directory.GetFiles("*.json"))
                {
                    if (events.Count >= limit)
                    {
                        break;
                    }
                    
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var lineageEvent = JsonSerializer.Deserialize<LineageEvent>(json);
                        
                        if (lineageEvent != null)
                        {
                            bool includeEvent = false;
                            
                            // Check entity ID
                            if (direction == LineageDirection.Upstream && lineageEvent.TargetId == entityId)
                            {
                                includeEvent = true;
                            }
                            else if (direction == LineageDirection.Downstream && lineageEvent.SourceId == entityId)
                            {
                                includeEvent = true;
                            }
                            else if (direction == LineageDirection.Both && 
                                     (lineageEvent.SourceId == entityId || lineageEvent.TargetId == entityId))
                            {
                                includeEvent = true;
                            }
                            
                            // Check time range
                            if (includeEvent && startTime.HasValue && lineageEvent.Timestamp < startTime.Value)
                            {
                                includeEvent = false;
                            }
                            
                            if (includeEvent && endTime.HasValue && lineageEvent.Timestamp > endTime.Value)
                            {
                                includeEvent = false;
                            }
                            
                            if (includeEvent)
                            {
                                events.Add(lineageEvent);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading lineage event file {FilePath}", file.FullName);
                    }
                }
                
                // Sort by timestamp
                return events.OrderByDescending(e => e.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage events for entity {EntityId}", entityId);
                return new List<LineageEvent>();
            }
        }
    }

    /// <summary>
    /// Interface for data lineage repository
    /// </summary>
    public interface IDataLineageRepository
    {
        Task SaveDataEntityAsync(DataEntity entity);
        Task<DataEntity> GetDataEntityAsync(string entityId);
        Task<List<DataEntity>> GetDataEntitiesByTypeAsync(string entityType, int limit = 100);
        Task<List<DataEntity>> SearchDataEntitiesAsync(string searchTerm, int limit = 100);
        Task SaveLineageEventAsync(LineageEvent lineageEvent);
        Task<List<LineageEvent>> GetLineageEventsAsync(string entityId, LineageDirection direction = LineageDirection.Both, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
    }
}
