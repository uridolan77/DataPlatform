using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.Database
{
    public abstract class BaseDatabaseConnector : BaseConnector
    {
        protected BaseDatabaseConnector(ILogger logger) : base(logger)
        {
        }

        public override async Task<bool> ValidateConnectionAsync(DataSourceDefinition source)
        {
            try
            {
                using var connection = CreateConnection(source);
                // Use synchronous Open() since IDbConnection doesn't have OpenAsync
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error validating connection to database {source}", source.Name);
                return false;
            }
        }

        public override async Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Extract connection properties
                string query = null;
                string table = null;

                source.ConnectionProperties.TryGetValue("query", out query);
                source.ConnectionProperties.TryGetValue("table", out table);

                if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(table))
                {
                    throw new ArgumentException("Either query or table is required for database connection");
                }

                // Create connection
                using var connection = CreateConnection(source);
                // Use synchronous Open() since IDbConnection doesn't have OpenAsync
                connection.Open();

                // Build the query if only table is provided
                if (string.IsNullOrEmpty(query) && !string.IsNullOrEmpty(table))
                {
                    var schema = source.ConnectionProperties.TryGetValue("schema", out var schemaName) ? schemaName : null;
                    var tableName = GetFullTableName(table, schema);

                    // Check if we need to limit the results
                    var limit = parameters != null && parameters.TryGetValue("limit", out var limitObj) ?
                        Convert.ToInt32(limitObj) : 0;

                    // Check if we need to filter the results
                    var whereClause = BuildWhereClause(parameters);

                    query = BuildSelectQuery(tableName, whereClause, limit);
                }

                // Execute the query
                var records = await ExecuteQueryAsync(connection, query, parameters, source);
                return records;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error fetching data from database {source}", source.Name);
                throw;
            }
        }

        public override async Task<IAsyncEnumerable<DataRecord>> StreamDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            // For simplicity, we'll just return the fetched data as an async enumerable
            var records = await FetchDataAsync(source, parameters);
            return records.ToAsyncEnumerable();
        }

        public override async Task<DataSchema> InferSchemaAsync(DataSourceDefinition source)
        {
            try
            {
                // Extract connection properties
                if (!source.ConnectionProperties.TryGetValue("table", out var table))
                {
                    // If no table is specified, we'll try to infer schema from a sample query
                    if (source.ConnectionProperties.TryGetValue("query", out var query))
                    {
                        // Fetch a sample of data
                        var parameters = new Dictionary<string, object> { ["limit"] = 10 };
                        var sampleData = await FetchDataAsync(source, parameters);

                        // Infer schema from the sample data
                        return InferSchemaFromSample(sampleData, source);
                    }

                    throw new ArgumentException("Either table or query is required for schema inference");
                }

                // Create connection
                using var connection = CreateConnection(source);
                // Use synchronous Open() since IDbConnection doesn't have OpenAsync
                connection.Open();

                // Get schema information from the database
                var schema = source.ConnectionProperties.TryGetValue("schema", out var schemaName) ? schemaName : null;
                var tableName = GetFullTableName(table, schema);

                return await GetTableSchemaAsync(connection, tableName, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error inferring schema for database {source}", source.Name);
                throw;
            }
        }

        public override async Task<DataIngestCheckpoint> GetLatestCheckpointAsync(string sourceId)
        {
            // In a real implementation, this would retrieve the checkpoint from a persistent store
            // For demonstration, we'll return a dummy checkpoint
            return await Task.FromResult(new DataIngestCheckpoint
            {
                SourceId = sourceId,
                CheckpointValue = DateTime.UtcNow.AddDays(-1).ToString("o"),
                ProcessedAt = DateTime.UtcNow.AddDays(-1),
                RecordsProcessed = 0,
                AdditionalInfo = new Dictionary<string, string>()
            });
        }

        public override async Task SaveCheckpointAsync(DataIngestCheckpoint checkpoint)
        {
            // In a real implementation, this would save the checkpoint to a persistent store
            await Task.CompletedTask;
        }

        protected abstract IDbConnection CreateConnection(DataSourceDefinition source);

        protected abstract string GetFullTableName(string table, string schema);

        protected abstract string BuildSelectQuery(string tableName, string whereClause, int limit);

        protected abstract Task<DataSchema> GetTableSchemaAsync(IDbConnection connection, string tableName, DataSourceDefinition source);

        protected virtual async Task<IEnumerable<DataRecord>> ExecuteQueryAsync(IDbConnection connection, string query, Dictionary<string, object> parameters, DataSourceDefinition source)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;

            // Add parameters if any
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (param.Key != "limit" && !param.Key.StartsWith("_")) // Skip special parameters
                    {
                        var dbParam = command.CreateParameter();
                        dbParam.ParameterName = param.Key;
                        dbParam.Value = param.Value ?? DBNull.Value;
                        command.Parameters.Add(dbParam);
                    }
                }
            }

            // Execute the query
            var records = new List<DataRecord>();
            using var reader = await ExecuteReaderAsync(command);

            // Use synchronous Read() since IDataReader doesn't have ReadAsync
            while (reader.Read())
            {
                var record = ConvertReaderRowToDataRecord(reader, source);
                records.Add(record);
            }

            return records;
        }

        protected virtual async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            // This is a simple wrapper to make the code more readable
            // In a real implementation, you might want to use a more sophisticated approach
            return await Task.FromResult(command.ExecuteReader());
        }

        protected virtual DataRecord ConvertReaderRowToDataRecord(IDataReader reader, DataSourceDefinition source)
        {
            var data = new Dictionary<string, object>();
            var metadata = new Dictionary<string, string>();

            // Get field names
            var fieldCount = reader.FieldCount;
            var fieldNames = new string[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                fieldNames[i] = reader.GetName(i);
            }

            // Get field values
            for (int i = 0; i < fieldCount; i++)
            {
                var fieldName = fieldNames[i];
                var value = reader.GetValue(i);

                if (value == DBNull.Value)
                {
                    data[fieldName] = null;
                }
                else
                {
                    data[fieldName] = value;
                }
            }

            // Generate a unique ID
            var id = Guid.NewGuid().ToString();

            // Add metadata
            metadata["source"] = "Database";
            metadata["sourceType"] = source.ConnectionProperties.TryGetValue("provider", out var provider) ? provider : "Unknown";
            metadata["sourceTable"] = source.ConnectionProperties.TryGetValue("table", out var table) ? table : "Unknown";

            return new DataRecord
            {
                Id = id,
                SchemaId = source.Schema?.Id,
                SourceId = source.Id,
                Data = data,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        protected virtual string BuildWhereClause(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            var whereConditions = new List<string>();

            foreach (var param in parameters)
            {
                if (param.Key != "limit" && !param.Key.StartsWith("_")) // Skip special parameters
                {
                    whereConditions.Add($"{param.Key} = @{param.Key}");
                }
            }

            if (whereConditions.Count == 0)
            {
                return string.Empty;
            }

            return $"WHERE {string.Join(" AND ", whereConditions)}";
        }

        protected virtual DataSchema InferSchemaFromSample(IEnumerable<DataRecord> sampleData, DataSourceDefinition source)
        {
            if (sampleData == null || !sampleData.Any())
            {
                return new DataSchema
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"{source.Name} Schema",
                    Description = $"Schema for {source.Name}",
                    Type = GenericDataPlatform.Common.Models.SchemaType.Dynamic,
                    Fields = new List<SchemaField>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            var schema = new DataSchema
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{source.Name} Schema",
                Description = $"Schema for {source.Name}",
                Type = GenericDataPlatform.Common.Models.SchemaType.Dynamic,
                Fields = new List<SchemaField>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Get all unique field names from the sample data
            var fieldNames = new HashSet<string>();
            foreach (var record in sampleData)
            {
                foreach (var key in record.Data.Keys)
                {
                    fieldNames.Add(key);
                }
            }

            // For each field, determine its type and other properties
            foreach (var fieldName in fieldNames)
            {
                var field = new SchemaField
                {
                    Name = fieldName,
                    Description = $"Field {fieldName}",
                    IsRequired = IsFieldRequired(fieldName, sampleData),
                    IsArray = IsFieldArray(fieldName, sampleData),
                    Type = InferFieldType(fieldName, sampleData),
                    DefaultValue = null,
                    Validation = new ValidationRules(),
                    NestedFields = new List<SchemaField>()
                };

                schema.Fields.Add(field);
            }

            return schema;
        }

        protected virtual bool IsFieldRequired(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // A field is required if it's present in all records and never null
            return sampleData.All(r => r.Data.ContainsKey(fieldName) && r.Data[fieldName] != null);
        }

        protected virtual bool IsFieldArray(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // Check if any value for this field is an array
            return sampleData.Any(r =>
                r.Data.ContainsKey(fieldName) &&
                r.Data[fieldName] != null &&
                r.Data[fieldName].GetType().IsArray);
        }

        protected virtual FieldType InferFieldType(string fieldName, IEnumerable<DataRecord> sampleData)
        {
            // Get non-null values for this field
            var values = sampleData
                .Where(r => r.Data.ContainsKey(fieldName) && r.Data[fieldName] != null)
                .Select(r => r.Data[fieldName])
                .ToList();

            if (values.Count == 0)
            {
                return FieldType.String; // Default to string if no values
            }

            // Check if all values are of the same type
            var firstType = values[0].GetType();

            if (values.All(v => v.GetType() == firstType))
            {
                // All values are of the same type
                if (firstType == typeof(string))
                {
                    return FieldType.String;
                }
                else if (firstType == typeof(int) || firstType == typeof(long) || firstType == typeof(short))
                {
                    return FieldType.Integer;
                }
                else if (firstType == typeof(float) || firstType == typeof(double) || firstType == typeof(decimal))
                {
                    return FieldType.Decimal;
                }
                else if (firstType == typeof(bool))
                {
                    return FieldType.Boolean;
                }
                else if (firstType == typeof(DateTime))
                {
                    return FieldType.DateTime;
                }
                else if (firstType.IsArray)
                {
                    return FieldType.Array;
                }
                else
                {
                    // Try to see if it's JSON
                    try
                    {
                        var jsonString = values[0].ToString();
                        var jsonDoc = JsonDocument.Parse(jsonString);

                        if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            return FieldType.Json;
                        }
                        else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            return FieldType.Array;
                        }
                    }
                    catch
                    {
                        // Not JSON
                    }

                    return FieldType.Complex;
                }
            }
            else
            {
                // Mixed types, default to string
                return FieldType.String;
            }
        }

        protected virtual void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }
    }
}
