using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GenericDataPlatform.DatabaseService.Repositories
{
    public class PostgresRepository : IDbRepository
    {
        private readonly PostgresOptions _options;
        private readonly ILogger<PostgresRepository> _logger;
        
        public PostgresRepository(IOptions<PostgresOptions> options, ILogger<PostgresRepository> logger)
        {
            _options = options.Value;
            _logger = logger;
        }
        
        public async Task<IEnumerable<DataRecord>> GetRecordsAsync(string sourceId, Dictionary<string, string> filters = null, int page = 1, int pageSize = 50)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Get the table name for this source
                var tableName = GetTableName(sourceId);
                
                // Build the query
                var query = new StringBuilder($"SELECT * FROM {tableName}");
                var parameters = new DynamicParameters();
                
                // Add filters if any
                if (filters != null && filters.Count > 0)
                {
                    query.Append(" WHERE ");
                    var filterClauses = new List<string>();
                    
                    foreach (var (key, value) in filters)
                    {
                        filterClauses.Add($"{key} = @{key}");
                        parameters.Add($"@{key}", value);
                    }
                    
                    query.Append(string.Join(" AND ", filterClauses));
                }
                
                // Add pagination
                query.Append(" ORDER BY id");
                query.Append(" LIMIT @pageSize OFFSET @offset");
                parameters.Add("@pageSize", pageSize);
                parameters.Add("@offset", (page - 1) * pageSize);
                
                // Execute the query
                var rows = await connection.QueryAsync(query.ToString(), parameters);
                
                // Convert to DataRecord objects
                var records = new List<DataRecord>();
                foreach (var row in rows)
                {
                    var record = ConvertRowToDataRecord(row, sourceId);
                    records.Add(record);
                }
                
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting records for source {sourceId}", sourceId);
                throw;
            }
        }
        
        public async Task<DataRecord> GetRecordAsync(string id)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // First, find the source ID for this record
                var sourceQuery = "SELECT source_id FROM data_records WHERE id = @id";
                var sourceId = await connection.QueryFirstOrDefaultAsync<string>(sourceQuery, new { id });
                
                if (string.IsNullOrEmpty(sourceId))
                {
                    return null;
                }
                
                // Get the table name for this source
                var tableName = GetTableName(sourceId);
                
                // Get the record
                var query = $"SELECT * FROM {tableName} WHERE id = @id";
                var row = await connection.QueryFirstOrDefaultAsync(query, new { id });
                
                if (row == null)
                {
                    return null;
                }
                
                return ConvertRowToDataRecord(row, sourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting record {id}", id);
                throw;
            }
        }
        
        public async Task<string> InsertRecordAsync(DataRecord record)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Ensure the record has an ID
                if (string.IsNullOrEmpty(record.Id))
                {
                    record.Id = Guid.NewGuid().ToString();
                }
                
                // Get the table name for this source
                var tableName = GetTableName(record.SourceId);
                
                // Ensure the table exists
                await EnsureTableExistsAsync(connection, record.SourceId);
                
                // Build the insert query
                var columns = new List<string> { "id", "schema_id", "source_id", "data", "metadata", "created_at", "updated_at", "version" };
                var values = new List<string> { "@id", "@schemaId", "@sourceId", "@data", "@metadata", "@createdAt", "@updatedAt", "@version" };
                
                var query = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
                
                var parameters = new
                {
                    id = record.Id,
                    schemaId = record.SchemaId,
                    sourceId = record.SourceId,
                    data = JsonSerializer.Serialize(record.Data),
                    metadata = JsonSerializer.Serialize(record.Metadata),
                    createdAt = record.CreatedAt,
                    updatedAt = record.UpdatedAt,
                    version = record.Version
                };
                
                await connection.ExecuteAsync(query, parameters);
                
                // Also insert into the data_records table for cross-referencing
                var crossRefQuery = "INSERT INTO data_records (id, source_id) VALUES (@id, @sourceId)";
                await connection.ExecuteAsync(crossRefQuery, new { id = record.Id, sourceId = record.SourceId });
                
                return record.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting record for source {sourceId}", record.SourceId);
                throw;
            }
        }
        
        public async Task<bool> UpdateRecordAsync(DataRecord record)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Get the table name for this source
                var tableName = GetTableName(record.SourceId);
                
                // Build the update query
                var query = $@"
                    UPDATE {tableName}
                    SET schema_id = @schemaId,
                        data = @data,
                        metadata = @metadata,
                        updated_at = @updatedAt,
                        version = @version
                    WHERE id = @id";
                
                var parameters = new
                {
                    id = record.Id,
                    schemaId = record.SchemaId,
                    data = JsonSerializer.Serialize(record.Data),
                    metadata = JsonSerializer.Serialize(record.Metadata),
                    updatedAt = DateTime.UtcNow,
                    version = record.Version
                };
                
                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating record {id}", record.Id);
                throw;
            }
        }
        
        public async Task<bool> DeleteRecordAsync(string id)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // First, find the source ID for this record
                var sourceQuery = "SELECT source_id FROM data_records WHERE id = @id";
                var sourceId = await connection.QueryFirstOrDefaultAsync<string>(sourceQuery, new { id });
                
                if (string.IsNullOrEmpty(sourceId))
                {
                    return false;
                }
                
                // Get the table name for this source
                var tableName = GetTableName(sourceId);
                
                // Delete the record
                var query = $"DELETE FROM {tableName} WHERE id = @id";
                var rowsAffected = await connection.ExecuteAsync(query, new { id });
                
                // Also delete from the data_records table
                var crossRefQuery = "DELETE FROM data_records WHERE id = @id";
                await connection.ExecuteAsync(crossRefQuery, new { id });
                
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting record {id}", id);
                throw;
            }
        }
        
        public async Task<long> CountRecordsAsync(string sourceId, Dictionary<string, string> filters = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Get the table name for this source
                var tableName = GetTableName(sourceId);
                
                // Build the query
                var query = new StringBuilder($"SELECT COUNT(*) FROM {tableName}");
                var parameters = new DynamicParameters();
                
                // Add filters if any
                if (filters != null && filters.Count > 0)
                {
                    query.Append(" WHERE ");
                    var filterClauses = new List<string>();
                    
                    foreach (var (key, value) in filters)
                    {
                        filterClauses.Add($"{key} = @{key}");
                        parameters.Add($"@{key}", value);
                    }
                    
                    query.Append(string.Join(" AND ", filterClauses));
                }
                
                // Execute the query
                return await connection.ExecuteScalarAsync<long>(query.ToString(), parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting records for source {sourceId}", sourceId);
                throw;
            }
        }
        
        public async Task<IEnumerable<DataRecord>> QueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Execute the query
                var rows = await connection.QueryAsync(query, parameters);
                
                // Convert to DataRecord objects
                var records = new List<DataRecord>();
                foreach (var row in rows)
                {
                    // Try to extract source ID from the row
                    string sourceId = null;
                    if (((IDictionary<string, object>)row).TryGetValue("source_id", out var sourceIdObj))
                    {
                        sourceId = sourceIdObj?.ToString();
                    }
                    
                    var record = ConvertRowToDataRecord(row, sourceId);
                    records.Add(record);
                }
                
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {query}", query);
                throw;
            }
        }
        
        public async Task<bool> CreateTableAsync(string sourceId, DataSchema schema)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Get the table name for this source
                var tableName = GetTableName(sourceId);
                
                // Check if the table already exists
                var tableExistsQuery = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = @tableName
                    )";
                
                var tableExists = await connection.ExecuteScalarAsync<bool>(tableExistsQuery, new { tableName });
                
                if (tableExists)
                {
                    // Table already exists
                    return false;
                }
                
                // Create the table
                var createTableQuery = $@"
                    CREATE TABLE {tableName} (
                        id VARCHAR(50) PRIMARY KEY,
                        schema_id VARCHAR(50) NOT NULL,
                        source_id VARCHAR(50) NOT NULL,
                        data JSONB NOT NULL,
                        metadata JSONB,
                        created_at TIMESTAMP NOT NULL,
                        updated_at TIMESTAMP NOT NULL,
                        version VARCHAR(50)
                    )";
                
                await connection.ExecuteAsync(createTableQuery);
                
                // Create indexes
                var createIndexQuery = $@"
                    CREATE INDEX idx_{tableName}_source_id ON {tableName} (source_id);
                    CREATE INDEX idx_{tableName}_schema_id ON {tableName} (schema_id);
                    CREATE INDEX idx_{tableName}_created_at ON {tableName} (created_at);
                ";
                
                await connection.ExecuteAsync(createIndexQuery);
                
                // Ensure the data_records table exists
                await EnsureDataRecordsTableExistsAsync(connection);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table for source {sourceId}", sourceId);
                throw;
            }
        }
        
        public async Task<bool> UpdateTableAsync(string sourceId, DataSchema schema)
        {
            // For this simple implementation, we don't actually modify the table structure
            // In a real implementation, this would handle schema evolution
            return await Task.FromResult(true);
        }
        
        public async Task<bool> DeleteTableAsync(string sourceId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                // Get the table name for this source
                var tableName = GetTableName(sourceId);
                
                // Check if the table exists
                var tableExistsQuery = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = @tableName
                    )";
                
                var tableExists = await connection.ExecuteScalarAsync<bool>(tableExistsQuery, new { tableName });
                
                if (!tableExists)
                {
                    // Table doesn't exist
                    return false;
                }
                
                // Delete the table
                var dropTableQuery = $"DROP TABLE {tableName}";
                await connection.ExecuteAsync(dropTableQuery);
                
                // Delete records from the data_records table
                var deleteRecordsQuery = "DELETE FROM data_records WHERE source_id = @sourceId";
                await connection.ExecuteAsync(deleteRecordsQuery, new { sourceId });
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table for source {sourceId}", sourceId);
                throw;
            }
        }
        
        private IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_options.ConnectionString);
        }
        
        private string GetTableName(string sourceId)
        {
            // Sanitize the source ID to create a valid table name
            var sanitized = new string(sourceId.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return $"data_{sanitized}";
        }
        
        private async Task EnsureTableExistsAsync(IDbConnection connection, string sourceId)
        {
            var tableName = GetTableName(sourceId);
            
            // Check if the table exists
            var tableExistsQuery = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = @tableName
                )";
            
            var tableExists = await connection.ExecuteScalarAsync<bool>(tableExistsQuery, new { tableName });
            
            if (!tableExists)
            {
                // Create the table
                var createTableQuery = $@"
                    CREATE TABLE {tableName} (
                        id VARCHAR(50) PRIMARY KEY,
                        schema_id VARCHAR(50) NOT NULL,
                        source_id VARCHAR(50) NOT NULL,
                        data JSONB NOT NULL,
                        metadata JSONB,
                        created_at TIMESTAMP NOT NULL,
                        updated_at TIMESTAMP NOT NULL,
                        version VARCHAR(50)
                    )";
                
                await connection.ExecuteAsync(createTableQuery);
                
                // Create indexes
                var createIndexQuery = $@"
                    CREATE INDEX idx_{tableName}_source_id ON {tableName} (source_id);
                    CREATE INDEX idx_{tableName}_schema_id ON {tableName} (schema_id);
                    CREATE INDEX idx_{tableName}_created_at ON {tableName} (created_at);
                ";
                
                await connection.ExecuteAsync(createIndexQuery);
            }
            
            // Ensure the data_records table exists
            await EnsureDataRecordsTableExistsAsync(connection);
        }
        
        private async Task EnsureDataRecordsTableExistsAsync(IDbConnection connection)
        {
            // Check if the data_records table exists
            var tableExistsQuery = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'data_records'
                )";
            
            var tableExists = await connection.ExecuteScalarAsync<bool>(tableExistsQuery);
            
            if (!tableExists)
            {
                // Create the data_records table for cross-referencing
                var createTableQuery = @"
                    CREATE TABLE data_records (
                        id VARCHAR(50) PRIMARY KEY,
                        source_id VARCHAR(50) NOT NULL
                    )";
                
                await connection.ExecuteAsync(createTableQuery);
                
                // Create index
                var createIndexQuery = "CREATE INDEX idx_data_records_source_id ON data_records (source_id)";
                await connection.ExecuteAsync(createIndexQuery);
            }
        }
        
        private DataRecord ConvertRowToDataRecord(dynamic row, string sourceId)
        {
            var rowDict = (IDictionary<string, object>)row;
            
            var record = new DataRecord
            {
                Id = rowDict["id"]?.ToString(),
                SchemaId = rowDict["schema_id"]?.ToString(),
                SourceId = sourceId ?? rowDict["source_id"]?.ToString(),
                CreatedAt = rowDict["created_at"] is DateTime createdAt ? createdAt : DateTime.UtcNow,
                UpdatedAt = rowDict["updated_at"] is DateTime updatedAt ? updatedAt : DateTime.UtcNow,
                Version = rowDict["version"]?.ToString()
            };
            
            // Parse the data JSON
            if (rowDict["data"] != null)
            {
                var dataJson = rowDict["data"].ToString();
                record.Data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataJson);
            }
            else
            {
                record.Data = new Dictionary<string, object>();
            }
            
            // Parse the metadata JSON
            if (rowDict["metadata"] != null)
            {
                var metadataJson = rowDict["metadata"].ToString();
                record.Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
            }
            else
            {
                record.Metadata = new Dictionary<string, string>();
            }
            
            return record;
        }
    }
    
    public class PostgresOptions
    {
        public string ConnectionString { get; set; }
    }
}
