using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.API.Models;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.API.Services
{
    public class DataService : IDataService
    {
        private readonly ILogger<DataService> _logger;
        
        // In a real implementation, these would be gRPC clients to the respective services
        // private readonly IngestionServiceClient _ingestionClient;
        // private readonly DatabaseServiceClient _databaseClient;
        // private readonly StorageServiceClient _storageClient;

        // For demonstration purposes, we'll use in-memory collections
        private readonly Dictionary<string, DataSourceDefinition> _dataSources;
        private readonly Dictionary<string, DataSchema> _schemas;
        private readonly Dictionary<string, List<DataRecord>> _records;

        public DataService(ILogger<DataService> logger)
        {
            _logger = logger;
            
            // Initialize with some sample data
            _dataSources = new Dictionary<string, DataSourceDefinition>();
            _schemas = new Dictionary<string, DataSchema>();
            _records = new Dictionary<string, List<DataRecord>>();
            
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // Create a sample data source
            var sourceId = Guid.NewGuid().ToString();
            var schema = new DataSchema
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Customer Schema",
                Description = "Schema for customer data",
                Type = SchemaType.Strict,
                Fields = new List<SchemaField>
                {
                    new SchemaField
                    {
                        Name = "CustomerId",
                        Description = "Unique identifier for the customer",
                        Type = FieldType.String,
                        IsRequired = true
                    },
                    new SchemaField
                    {
                        Name = "FirstName",
                        Description = "Customer's first name",
                        Type = FieldType.String,
                        IsRequired = true
                    },
                    new SchemaField
                    {
                        Name = "LastName",
                        Description = "Customer's last name",
                        Type = FieldType.String,
                        IsRequired = true
                    },
                    new SchemaField
                    {
                        Name = "Email",
                        Description = "Customer's email address",
                        Type = FieldType.String,
                        IsRequired = true,
                        Validation = new ValidationRules
                        {
                            Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
                        }
                    },
                    new SchemaField
                    {
                        Name = "Age",
                        Description = "Customer's age",
                        Type = FieldType.Integer,
                        IsRequired = false,
                        Validation = new ValidationRules
                        {
                            MinValue = 18,
                            MaxValue = 120
                        }
                    },
                    new SchemaField
                    {
                        Name = "IsActive",
                        Description = "Whether the customer is active",
                        Type = FieldType.Boolean,
                        IsRequired = false,
                        DefaultValue = "true"
                    },
                    new SchemaField
                    {
                        Name = "RegistrationDate",
                        Description = "Date when the customer registered",
                        Type = FieldType.DateTime,
                        IsRequired = true
                    }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = new SchemaVersion
                {
                    VersionNumber = "1.0",
                    EffectiveDate = DateTime.UtcNow,
                    PreviousVersion = null,
                    ChangeDescription = "Initial version"
                }
            };

            var source = new DataSourceDefinition
            {
                Id = sourceId,
                Name = "Customer Database",
                Description = "Source for customer data",
                Type = DataSourceType.Database,
                ConnectionProperties = new Dictionary<string, string>
                {
                    { "connectionString", "Server=localhost;Database=Customers;User Id=sa;Password=P@ssw0rd;" },
                    { "provider", "SqlServer" },
                    { "table", "Customers" }
                },
                Schema = schema,
                IngestMode = DataIngestMode.FullLoad,
                RefreshPolicy = DataRefreshPolicy.Manual,
                ValidationRules = new Dictionary<string, string>(),
                MetadataProperties = new Dictionary<string, string>
                {
                    { "owner", "Sales Department" },
                    { "sensitivity", "Confidential" }
                }
            };

            _dataSources[sourceId] = source;
            _schemas[sourceId] = schema;
            
            // Create some sample records
            var records = new List<DataRecord>();
            for (int i = 1; i <= 10; i++)
            {
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = schema.Id,
                    SourceId = sourceId,
                    Data = new Dictionary<string, object>
                    {
                        { "CustomerId", $"CUST-{i:D5}" },
                        { "FirstName", $"FirstName{i}" },
                        { "LastName", $"LastName{i}" },
                        { "Email", $"customer{i}@example.com" },
                        { "Age", 20 + i },
                        { "IsActive", i % 2 == 0 },
                        { "RegistrationDate", DateTime.UtcNow.AddDays(-i * 10) }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "CreatedBy", "System" },
                        { "Source", "Sample Data" }
                    },
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
                
                records.Add(record);
            }
            
            _records[sourceId] = records;
        }

        public Task<IEnumerable<DataSourceDefinition>> GetDataSourcesAsync()
        {
            return Task.FromResult<IEnumerable<DataSourceDefinition>>(_dataSources.Values);
        }

        public Task<DataSourceDefinition> GetDataSourceAsync(string id)
        {
            if (_dataSources.TryGetValue(id, out var source))
            {
                return Task.FromResult(source);
            }
            
            return Task.FromResult<DataSourceDefinition>(null);
        }

        public Task<DataSourceDefinition> CreateDataSourceAsync(DataSourceDefinition source)
        {
            if (string.IsNullOrEmpty(source.Id))
            {
                source.Id = Guid.NewGuid().ToString();
            }
            
            _dataSources[source.Id] = source;
            
            if (source.Schema != null)
            {
                _schemas[source.Id] = source.Schema;
            }
            
            return Task.FromResult(source);
        }

        public Task<DataSourceDefinition> UpdateDataSourceAsync(DataSourceDefinition source)
        {
            if (!_dataSources.ContainsKey(source.Id))
            {
                throw new KeyNotFoundException($"Data source with ID {source.Id} not found");
            }
            
            _dataSources[source.Id] = source;
            
            if (source.Schema != null)
            {
                _schemas[source.Id] = source.Schema;
            }
            
            return Task.FromResult(source);
        }

        public Task<bool> DeleteDataSourceAsync(string id)
        {
            var result = _dataSources.Remove(id);
            _schemas.Remove(id);
            _records.Remove(id);
            
            return Task.FromResult(result);
        }

        public Task<DataSchema> GetSchemaAsync(string sourceId)
        {
            if (_schemas.TryGetValue(sourceId, out var schema))
            {
                return Task.FromResult(schema);
            }
            
            return Task.FromResult<DataSchema>(null);
        }

        public Task<DataSchema> UpdateSchemaAsync(string sourceId, DataSchema schema)
        {
            if (!_dataSources.ContainsKey(sourceId))
            {
                throw new KeyNotFoundException($"Data source with ID {sourceId} not found");
            }
            
            _schemas[sourceId] = schema;
            
            // Update the schema in the data source as well
            var source = _dataSources[sourceId];
            source.Schema = schema;
            _dataSources[sourceId] = source;
            
            return Task.FromResult(schema);
        }

        public Task<PagedResult<DataRecord>> GetRecordsAsync(string sourceId, Dictionary<string, string> filters = null, int page = 1, int pageSize = 50)
        {
            if (!_records.TryGetValue(sourceId, out var records))
            {
                return Task.FromResult(new PagedResult<DataRecord>
                {
                    Items = new List<DataRecord>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = 0
                });
            }
            
            // Apply filters if any
            var filteredRecords = records;
            if (filters != null && filters.Count > 0)
            {
                filteredRecords = records.Where(r => 
                {
                    foreach (var filter in filters)
                    {
                        if (!r.Data.TryGetValue(filter.Key, out var value) || 
                            value?.ToString() != filter.Value)
                        {
                            return false;
                        }
                    }
                    return true;
                }).ToList();
            }
            
            // Apply pagination
            var totalCount = filteredRecords.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var pagedRecords = filteredRecords
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return Task.FromResult(new PagedResult<DataRecord>
            {
                Items = pagedRecords,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        public Task<DataRecord> GetRecordAsync(string id)
        {
            foreach (var recordList in _records.Values)
            {
                var record = recordList.FirstOrDefault(r => r.Id == id);
                if (record != null)
                {
                    return Task.FromResult(record);
                }
            }
            
            return Task.FromResult<DataRecord>(null);
        }

        public Task<string> CreateRecordAsync(string sourceId, DataRecord record)
        {
            if (!_dataSources.ContainsKey(sourceId))
            {
                throw new KeyNotFoundException($"Data source with ID {sourceId} not found");
            }
            
            if (string.IsNullOrEmpty(record.Id))
            {
                record.Id = Guid.NewGuid().ToString();
            }
            
            record.SourceId = sourceId;
            record.CreatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            
            if (!_records.ContainsKey(sourceId))
            {
                _records[sourceId] = new List<DataRecord>();
            }
            
            _records[sourceId].Add(record);
            
            return Task.FromResult(record.Id);
        }

        public Task<bool> UpdateRecordAsync(DataRecord record)
        {
            if (string.IsNullOrEmpty(record.SourceId))
            {
                throw new ArgumentException("Source ID is required");
            }
            
            if (!_records.TryGetValue(record.SourceId, out var records))
            {
                return Task.FromResult(false);
            }
            
            var index = records.FindIndex(r => r.Id == record.Id);
            if (index == -1)
            {
                return Task.FromResult(false);
            }
            
            record.UpdatedAt = DateTime.UtcNow;
            records[index] = record;
            
            return Task.FromResult(true);
        }

        public Task<bool> DeleteRecordAsync(string id)
        {
            foreach (var sourceId in _records.Keys)
            {
                var records = _records[sourceId];
                var index = records.FindIndex(r => r.Id == id);
                
                if (index != -1)
                {
                    records.RemoveAt(index);
                    return Task.FromResult(true);
                }
            }
            
            return Task.FromResult(false);
        }

        public Task<QueryResult> QueryAsync(DataQuery query)
        {
            if (!_records.TryGetValue(query.SourceId, out var records))
            {
                return Task.FromResult(new QueryResult
                {
                    Records = new List<Dictionary<string, object>>(),
                    TotalCount = 0
                });
            }
            
            // Apply filters
            var filteredRecords = records.AsEnumerable();
            if (query.Filters != null && query.Filters.Count > 0)
            {
                filteredRecords = filteredRecords.Where(r => 
                {
                    foreach (var filter in query.Filters)
                    {
                        // Simple equals filter for demonstration
                        if (filter.Operator == "eq" && 
                            r.Data.TryGetValue(filter.Field, out var value))
                        {
                            if (value?.ToString() != filter.Value?.ToString())
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                });
            }
            
            // Apply sorting
            if (query.Sort != null && query.Sort.Count > 0)
            {
                foreach (var sort in query.Sort)
                {
                    if (sort.Descending)
                    {
                        filteredRecords = filteredRecords.OrderByDescending(r => 
                            r.Data.TryGetValue(sort.Field, out var value) ? value : null);
                    }
                    else
                    {
                        filteredRecords = filteredRecords.OrderBy(r => 
                            r.Data.TryGetValue(sort.Field, out var value) ? value : null);
                    }
                }
            }
            
            // Apply pagination
            var totalCount = filteredRecords.Count();
            var pagedRecords = filteredRecords
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();
            
            // Project fields if specified
            var result = new List<Dictionary<string, object>>();
            foreach (var record in pagedRecords)
            {
                var projectedRecord = new Dictionary<string, object>();
                
                if (query.Fields != null && query.Fields.Count > 0)
                {
                    foreach (var field in query.Fields)
                    {
                        if (record.Data.TryGetValue(field, out var value))
                        {
                            projectedRecord[field] = value;
                        }
                    }
                }
                else
                {
                    // Include all fields
                    foreach (var kvp in record.Data)
                    {
                        projectedRecord[kvp.Key] = kvp.Value;
                    }
                }
                
                // Always include the ID
                projectedRecord["Id"] = record.Id;
                
                result.Add(projectedRecord);
            }
            
            return Task.FromResult(new QueryResult
            {
                Records = result,
                TotalCount = totalCount
            });
        }
    }
}
