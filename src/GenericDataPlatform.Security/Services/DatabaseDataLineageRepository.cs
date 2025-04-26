using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Security.Models.DataLineage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// SQL Server implementation of the data lineage repository
    /// </summary>
    public class DatabaseDataLineageRepository : IDataLineageRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseDataLineageRepository> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly JsonSerializerOptions _jsonOptions;

        public DatabaseDataLineageRepository(
            IOptions<SecurityOptions> options,
            ILogger<DatabaseDataLineageRepository> logger,
            IAsyncPolicy resiliencePolicy)
        {
            _connectionString = options.Value.ConnectionStrings?.SqlServer 
                ?? throw new ArgumentNullException(nameof(options.Value.ConnectionStrings.SqlServer), 
                    "SQL Server connection string is required for DatabaseDataLineageRepository");
            _logger = logger;
            _resiliencePolicy = resiliencePolicy;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Ensure database tables exist
            EnsureTablesExistAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureTablesExistAsync()
        {
            try
            {
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Check if DataEntities table exists
                    var tableExists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataEntities'");

                    if (tableExists == 0)
                    {
                        _logger.LogInformation("Creating lineage tables");
                        
                        // Read SQL script from embedded resource
                        var assembly = typeof(DatabaseDataLineageRepository).Assembly;
                        var resourceName = "GenericDataPlatform.Security.Database.Scripts.CreateLineageTables.sql";
                        
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
                        }
                        
                        using var reader = new System.IO.StreamReader(stream);
                        var sql = await reader.ReadToEndAsync();
                        
                        // Execute script
                        await connection.ExecuteAsync(sql);
                        
                        _logger.LogInformation("Lineage tables created successfully");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring lineage tables exist");
                throw;
            }
        }

        /// <summary>
        /// Adds a data entity
        /// </summary>
        public async Task<string> AddDataEntityAsync(DataEntity entity)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    entity.Id ??= Guid.NewGuid().ToString();
                    
                    // Set timestamps
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO DataEntities (Id, Name, Type, Description, Schema, Location, Owner, Tags, CreatedAt, UpdatedAt, LastAccessedAt)
                        VALUES (@Id, @Name, @Type, @Description, @Schema, @Location, @Owner, @Tags, @CreatedAt, @UpdatedAt, @LastAccessedAt)";

                    await connection.ExecuteAsync(sql, new
                    {
                        entity.Id,
                        entity.Name,
                        entity.Type,
                        entity.Description,
                        Schema = entity.Schema != null ? JsonSerializer.Serialize(entity.Schema, _jsonOptions) : null,
                        entity.Location,
                        entity.Owner,
                        Tags = entity.Tags != null ? JsonSerializer.Serialize(entity.Tags, _jsonOptions) : null,
                        entity.CreatedAt,
                        entity.UpdatedAt,
                        entity.LastAccessedAt
                    });
                    
                    _logger.LogInformation("Added data entity {EntityId} of type {EntityType}", entity.Id, entity.Type);
                    
                    return entity.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding data entity {EntityName}", entity.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates a data entity
        /// </summary>
        public async Task<bool> UpdateDataEntityAsync(DataEntity entity)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Set updated timestamp
                    entity.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        UPDATE DataEntities
                        SET Name = @Name,
                            Type = @Type,
                            Description = @Description,
                            Schema = @Schema,
                            Location = @Location,
                            Owner = @Owner,
                            Tags = @Tags,
                            UpdatedAt = @UpdatedAt,
                            LastAccessedAt = @LastAccessedAt
                        WHERE Id = @Id";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        entity.Id,
                        entity.Name,
                        entity.Type,
                        entity.Description,
                        Schema = entity.Schema != null ? JsonSerializer.Serialize(entity.Schema, _jsonOptions) : null,
                        entity.Location,
                        entity.Owner,
                        Tags = entity.Tags != null ? JsonSerializer.Serialize(entity.Tags, _jsonOptions) : null,
                        entity.UpdatedAt,
                        entity.LastAccessedAt
                    });
                    
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Data entity {EntityId} not found for update", entity.Id);
                        return false;
                    }
                    
                    _logger.LogInformation("Updated data entity {EntityId}", entity.Id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data entity {EntityId}", entity.Id);
                return false;
            }
        }

        /// <summary>
        /// Gets a data entity by ID
        /// </summary>
        public async Task<DataEntity> GetDataEntityAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataEntities WHERE Id = @Id";
                    var entity = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
                    
                    if (entity == null)
                    {
                        _logger.LogWarning("Data entity {EntityId} not found", id);
                        return null;
                    }
                    
                    return MapToDataEntity(entity);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entity {EntityId}", id);
                return null;
            }
        }

        /// <summary>
        /// Gets data entities by type
        /// </summary>
        public async Task<List<DataEntity>> GetDataEntitiesByTypeAsync(string type)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataEntities WHERE Type = @Type ORDER BY Name";
                    var entities = await connection.QueryAsync<dynamic>(sql, new { Type = type });
                    
                    return entities.Select(MapToDataEntity).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entities by type {EntityType}", type);
                return new List<DataEntity>();
            }
        }

        /// <summary>
        /// Gets data entities by location
        /// </summary>
        public async Task<List<DataEntity>> GetDataEntitiesByLocationAsync(string location)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataEntities WHERE Location LIKE @Location ORDER BY Name";
                    var entities = await connection.QueryAsync<dynamic>(sql, new { Location = $"%{location}%" });
                    
                    return entities.Select(MapToDataEntity).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data entities by location {Location}", location);
                return new List<DataEntity>();
            }
        }

        /// <summary>
        /// Adds a lineage relationship
        /// </summary>
        public async Task<string> AddLineageAsync(DataLineage lineage)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    lineage.Id ??= Guid.NewGuid().ToString();
                    
                    // Set timestamp
                    lineage.CreatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO DataLineage (Id, SourceEntityId, TargetEntityId, ProcessId, ProcessName, ProcessType, TransformationDetails, CreatedAt)
                        VALUES (@Id, @SourceEntityId, @TargetEntityId, @ProcessId, @ProcessName, @ProcessType, @TransformationDetails, @CreatedAt)";

                    await connection.ExecuteAsync(sql, new
                    {
                        lineage.Id,
                        lineage.SourceEntityId,
                        lineage.TargetEntityId,
                        lineage.ProcessId,
                        lineage.ProcessName,
                        lineage.ProcessType,
                        TransformationDetails = lineage.TransformationDetails != null ? JsonSerializer.Serialize(lineage.TransformationDetails, _jsonOptions) : null,
                        lineage.CreatedAt
                    });
                    
                    _logger.LogInformation("Added lineage {LineageId} from {SourceId} to {TargetId}", lineage.Id, lineage.SourceEntityId, lineage.TargetEntityId);
                    
                    return lineage.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lineage from {SourceId} to {TargetId}", lineage.SourceEntityId, lineage.TargetEntityId);
                throw;
            }
        }

        /// <summary>
        /// Gets lineage relationships by source entity ID
        /// </summary>
        public async Task<List<DataLineage>> GetLineageBySourceAsync(string sourceEntityId)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataLineage WHERE SourceEntityId = @SourceEntityId ORDER BY CreatedAt DESC";
                    var lineages = await connection.QueryAsync<dynamic>(sql, new { SourceEntityId = sourceEntityId });
                    
                    return lineages.Select(MapToDataLineage).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage by source {SourceId}", sourceEntityId);
                return new List<DataLineage>();
            }
        }

        /// <summary>
        /// Gets lineage relationships by target entity ID
        /// </summary>
        public async Task<List<DataLineage>> GetLineageByTargetAsync(string targetEntityId)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataLineage WHERE TargetEntityId = @TargetEntityId ORDER BY CreatedAt DESC";
                    var lineages = await connection.QueryAsync<dynamic>(sql, new { TargetEntityId = targetEntityId });
                    
                    return lineages.Select(MapToDataLineage).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage by target {TargetId}", targetEntityId);
                return new List<DataLineage>();
            }
        }

        /// <summary>
        /// Gets lineage relationships by process ID
        /// </summary>
        public async Task<List<DataLineage>> GetLineageByProcessAsync(string processId)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataLineage WHERE ProcessId = @ProcessId ORDER BY CreatedAt DESC";
                    var lineages = await connection.QueryAsync<dynamic>(sql, new { ProcessId = processId });
                    
                    return lineages.Select(MapToDataLineage).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lineage by process {ProcessId}", processId);
                return new List<DataLineage>();
            }
        }

        /// <summary>
        /// Adds a data process
        /// </summary>
        public async Task<string> AddDataProcessAsync(DataProcess process)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    process.Id ??= Guid.NewGuid().ToString();
                    
                    // Set timestamps
                    process.CreatedAt = DateTime.UtcNow;
                    process.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO DataProcesses (Id, Name, Type, Description, Owner, Schedule, LastRunAt, Status, Configuration, CreatedAt, UpdatedAt)
                        VALUES (@Id, @Name, @Type, @Description, @Owner, @Schedule, @LastRunAt, @Status, @Configuration, @CreatedAt, @UpdatedAt)";

                    await connection.ExecuteAsync(sql, new
                    {
                        process.Id,
                        process.Name,
                        process.Type,
                        process.Description,
                        process.Owner,
                        process.Schedule,
                        process.LastRunAt,
                        process.Status,
                        Configuration = process.Configuration != null ? JsonSerializer.Serialize(process.Configuration, _jsonOptions) : null,
                        process.CreatedAt,
                        process.UpdatedAt
                    });
                    
                    _logger.LogInformation("Added data process {ProcessId} of type {ProcessType}", process.Id, process.Type);
                    
                    return process.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding data process {ProcessName}", process.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates a data process
        /// </summary>
        public async Task<bool> UpdateDataProcessAsync(DataProcess process)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Set updated timestamp
                    process.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        UPDATE DataProcesses
                        SET Name = @Name,
                            Type = @Type,
                            Description = @Description,
                            Owner = @Owner,
                            Schedule = @Schedule,
                            LastRunAt = @LastRunAt,
                            Status = @Status,
                            Configuration = @Configuration,
                            UpdatedAt = @UpdatedAt
                        WHERE Id = @Id";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        process.Id,
                        process.Name,
                        process.Type,
                        process.Description,
                        process.Owner,
                        process.Schedule,
                        process.LastRunAt,
                        process.Status,
                        Configuration = process.Configuration != null ? JsonSerializer.Serialize(process.Configuration, _jsonOptions) : null,
                        process.UpdatedAt
                    });
                    
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Data process {ProcessId} not found for update", process.Id);
                        return false;
                    }
                    
                    _logger.LogInformation("Updated data process {ProcessId}", process.Id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data process {ProcessId}", process.Id);
                return false;
            }
        }

        /// <summary>
        /// Gets a data process by ID
        /// </summary>
        public async Task<DataProcess> GetDataProcessAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataProcesses WHERE Id = @Id";
                    var process = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
                    
                    if (process == null)
                    {
                        _logger.LogWarning("Data process {ProcessId} not found", id);
                        return null;
                    }
                    
                    return MapToDataProcess(process);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data process {ProcessId}", id);
                return null;
            }
        }

        /// <summary>
        /// Gets data processes by type
        /// </summary>
        public async Task<List<DataProcess>> GetDataProcessesByTypeAsync(string type)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataProcesses WHERE Type = @Type ORDER BY Name";
                    var processes = await connection.QueryAsync<dynamic>(sql, new { Type = type });
                    
                    return processes.Select(MapToDataProcess).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data processes by type {ProcessType}", type);
                return new List<DataProcess>();
            }
        }

        /// <summary>
        /// Adds a data quality metric
        /// </summary>
        public async Task<string> AddDataQualityMetricAsync(DataQualityMetric metric)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    metric.Id ??= Guid.NewGuid().ToString();
                    
                    // Set timestamp
                    metric.Timestamp = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO DataQuality (Id, EntityId, MetricName, MetricValue, Timestamp)
                        VALUES (@Id, @EntityId, @MetricName, @MetricValue, @Timestamp)";

                    await connection.ExecuteAsync(sql, new
                    {
                        metric.Id,
                        metric.EntityId,
                        metric.MetricName,
                        metric.MetricValue,
                        metric.Timestamp
                    });
                    
                    _logger.LogInformation("Added data quality metric {MetricName} for entity {EntityId}", metric.MetricName, metric.EntityId);
                    
                    return metric.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding data quality metric {MetricName} for entity {EntityId}", metric.MetricName, metric.EntityId);
                throw;
            }
        }

        /// <summary>
        /// Gets data quality metrics by entity ID
        /// </summary>
        public async Task<List<DataQualityMetric>> GetDataQualityMetricsByEntityAsync(string entityId)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataQuality WHERE EntityId = @EntityId ORDER BY Timestamp DESC";
                    var metrics = await connection.QueryAsync<DataQualityMetric>(sql, new { EntityId = entityId });
                    
                    return metrics.ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data quality metrics for entity {EntityId}", entityId);
                return new List<DataQualityMetric>();
            }
        }

        /// <summary>
        /// Gets data quality metrics by metric name
        /// </summary>
        public async Task<List<DataQualityMetric>> GetDataQualityMetricsByNameAsync(string metricName)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataQuality WHERE MetricName = @MetricName ORDER BY Timestamp DESC";
                    var metrics = await connection.QueryAsync<DataQualityMetric>(sql, new { MetricName = metricName });
                    
                    return metrics.ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data quality metrics by name {MetricName}", metricName);
                return new List<DataQualityMetric>();
            }
        }

        #region Helper Methods

        private DataEntity MapToDataEntity(dynamic entity)
        {
            var dataEntity = new DataEntity
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Type,
                Description = entity.Description,
                Location = entity.Location,
                Owner = entity.Owner,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                LastAccessedAt = entity.LastAccessedAt
            };
            
            // Deserialize Schema
            if (!string.IsNullOrEmpty(entity.Schema))
            {
                dataEntity.Schema = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Schema, _jsonOptions);
            }
            
            // Deserialize Tags
            if (!string.IsNullOrEmpty(entity.Tags))
            {
                dataEntity.Tags = JsonSerializer.Deserialize<List<string>>(entity.Tags, _jsonOptions);
            }
            
            return dataEntity;
        }

        private DataLineage MapToDataLineage(dynamic lineage)
        {
            var dataLineage = new DataLineage
            {
                Id = lineage.Id,
                SourceEntityId = lineage.SourceEntityId,
                TargetEntityId = lineage.TargetEntityId,
                ProcessId = lineage.ProcessId,
                ProcessName = lineage.ProcessName,
                ProcessType = lineage.ProcessType,
                CreatedAt = lineage.CreatedAt
            };
            
            // Deserialize TransformationDetails
            if (!string.IsNullOrEmpty(lineage.TransformationDetails))
            {
                dataLineage.TransformationDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(lineage.TransformationDetails, _jsonOptions);
            }
            
            return dataLineage;
        }

        private DataProcess MapToDataProcess(dynamic process)
        {
            var dataProcess = new DataProcess
            {
                Id = process.Id,
                Name = process.Name,
                Type = process.Type,
                Description = process.Description,
                Owner = process.Owner,
                Schedule = process.Schedule,
                LastRunAt = process.LastRunAt,
                Status = process.Status,
                CreatedAt = process.CreatedAt,
                UpdatedAt = process.UpdatedAt
            };
            
            // Deserialize Configuration
            if (!string.IsNullOrEmpty(process.Configuration))
            {
                dataProcess.Configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(process.Configuration, _jsonOptions);
            }
            
            return dataProcess;
        }

        #endregion
    }
}
