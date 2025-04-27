using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.Streaming
{
    public abstract class BaseStreamingConnector : BaseConnector
    {
        protected BaseStreamingConnector(ILogger logger) : base(logger)
        {
        }

        public override async Task<bool> ValidateConnectionAsync(DataSourceDefinition source)
        {
            try
            {
                // Try to create a consumer
                var consumer = await CreateConsumerAsync(source);
                // Close the consumer when done
                CloseConsumer(consumer);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error validating connection to streaming source {source}", source.Name);
                return false;
            }
        }

        public override async Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Extract parameters
                int maxMessages = 100; // Default to 100 messages
                if (parameters != null && parameters.TryGetValue("maxMessages", out var maxMessagesObj) &&
                    int.TryParse(maxMessagesObj.ToString(), out var maxMessagesValue))
                {
                    maxMessages = maxMessagesValue;
                }

                int timeoutMs = 5000; // Default to 5 seconds
                if (parameters != null && parameters.TryGetValue("timeoutMs", out var timeoutObj) &&
                    int.TryParse(timeoutObj.ToString(), out var timeoutValue))
                {
                    timeoutMs = timeoutValue;
                }

                // Create a consumer
                var consumer = await CreateConsumerAsync(source);
                try {

                // Consume messages
                var records = new List<DataRecord>();
                var cts = new CancellationTokenSource(timeoutMs);

                try
                {
                    while (records.Count < maxMessages && !cts.IsCancellationRequested)
                    {
                        var message = await ConsumeMessageAsync(consumer, cts.Token);

                        if (message != null)
                        {
                            var record = ConvertMessageToDataRecord(message, source);
                            records.Add(record);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout expired, return what we have
                }

                    return records;
                }
                finally
                {
                    // Close the consumer when done
                    CloseConsumer(consumer);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error fetching data from streaming source {source}", source.Name);
                throw;
            }
        }

        public override async Task<IAsyncEnumerable<DataRecord>> StreamDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Create a consumer
                var consumer = await CreateConsumerAsync(source);

                // Return an async enumerable that consumes messages
                return ConsumeMessagesAsyncEnumerable(consumer, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error streaming data from streaming source {source}", source.Name);
                throw;
            }
        }

        public override async Task<DataSchema> InferSchemaAsync(DataSourceDefinition source)
        {
            try
            {
                // Fetch a sample of messages
                var parameters = new Dictionary<string, object>
                {
                    ["maxMessages"] = 10,
                    ["timeoutMs"] = 10000
                };

                var sampleRecords = await FetchDataAsync(source, parameters);

                // Infer schema from the sample records
                return InferSchemaFromSample(sampleRecords, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error inferring schema for streaming source {source}", source.Name);
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
                CheckpointValue = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
                ProcessedAt = DateTime.UtcNow.AddMinutes(-5),
                RecordsProcessed = 0,
                AdditionalInfo = new Dictionary<string, string>()
            });
        }

        public override async Task SaveCheckpointAsync(DataIngestCheckpoint checkpoint)
        {
            // In a real implementation, this would save the checkpoint to a persistent store
            await Task.CompletedTask;
        }

        protected abstract Task<object> CreateConsumerAsync(DataSourceDefinition source);

        protected abstract Task<object> ConsumeMessageAsync(object consumer, CancellationToken cancellationToken);

        protected abstract void CloseConsumer(object consumer);

        protected virtual async IAsyncEnumerable<DataRecord> ConsumeMessagesAsyncEnumerable(object consumer, DataSourceDefinition source)
        {
            try
            {
                while (true)
                {
                    var message = await ConsumeMessageAsync(consumer, CancellationToken.None);

                    if (message != null)
                    {
                        var record = ConvertMessageToDataRecord(message, source);
                        yield return record;
                    }
                }
            }
            finally
            {
                CloseConsumer(consumer);
            }
        }

        protected virtual DataRecord ConvertMessageToDataRecord(object message, DataSourceDefinition source)
        {
            // This is a base implementation that should be overridden by specific connectors
            var data = new Dictionary<string, object>
            {
                ["message"] = message.ToString()
            };

            return new DataRecord
            {
                Id = Guid.NewGuid().ToString(),
                SchemaId = source.Schema?.Id,
                SourceId = source.Id,
                Data = data,
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = "Streaming",
                    ["timestamp"] = DateTime.UtcNow.ToString("o")
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        protected virtual DataSchema InferSchemaFromSample(IEnumerable<DataRecord> sampleRecords, DataSourceDefinition source)
        {
            // Create a basic schema
            var schema = new DataSchema
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{source.Name} Schema",
                Description = $"Schema for {source.Name}",
                Type = SchemaType.Dynamic,
                Fields = new List<SchemaField>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // If we have sample records, infer schema from them
            if (sampleRecords != null && sampleRecords.GetEnumerator().MoveNext())
            {
                // Get all unique field names from the sample data
                var fieldNames = new HashSet<string>();
                foreach (var record in sampleRecords)
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
                        IsRequired = IsFieldRequired(fieldName, sampleRecords),
                        IsArray = IsFieldArray(fieldName, sampleRecords),
                        Type = InferFieldType(fieldName, sampleRecords),
                        DefaultValue = null,
                        Validation = new ValidationRules(),
                        NestedFields = new List<SchemaField>()
                    };

                    schema.Fields.Add(field);
                }
            }
            else
            {
                // Add a default field for the message
                schema.Fields.Add(new SchemaField
                {
                    Name = "message",
                    Description = "Message content",
                    IsRequired = true,
                    Type = FieldType.String,
                    DefaultValue = null,
                    Validation = new ValidationRules(),
                    NestedFields = new List<SchemaField>()
                });
            }

            return schema;
        }

        protected virtual bool IsFieldRequired(string fieldName, IEnumerable<DataRecord> sampleRecords)
        {
            // A field is required if it's present in all records and never null
            return sampleRecords.All(r => r.Data.ContainsKey(fieldName) && r.Data[fieldName] != null);
        }

        protected virtual bool IsFieldArray(string fieldName, IEnumerable<DataRecord> sampleRecords)
        {
            // Check if any value for this field is an array
            return sampleRecords.Any(r =>
                r.Data.ContainsKey(fieldName) &&
                r.Data[fieldName] != null &&
                r.Data[fieldName].GetType().IsArray);
        }

        protected virtual FieldType InferFieldType(string fieldName, IEnumerable<DataRecord> sampleRecords)
        {
            // Get non-null values for this field
            var values = sampleRecords
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
