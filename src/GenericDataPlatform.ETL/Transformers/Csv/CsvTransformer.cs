using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Transformers.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Transformers.Csv
{
    public class CsvTransformer : ITransformer
    {
        private readonly ILogger<CsvTransformer> _logger;

        public string Type => "Csv";

        public CsvTransformer(ILogger<CsvTransformer> logger)
        {
            _logger = logger;
        }

        public async Task<object> TransformAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Determine the input type and handle accordingly
                if (input is IEnumerable<DataRecord> inputRecords)
                {
                    // Input is already a collection of DataRecord objects
                    return await TransformDataRecordsAsync(inputRecords, configuration, source);
                }
                else if (input is string csvContent)
                {
                    // Input is a CSV string
                    return await TransformCsvStringAsync(csvContent, configuration, source);
                }
                else if (input is byte[] csvBytes)
                {
                    // Input is CSV bytes
                    var csvString = Encoding.UTF8.GetString(csvBytes);
                    return await TransformCsvStringAsync(csvString, configuration, source);
                }
                else if (input is Stream csvStream)
                {
                    // Input is a stream
                    using var reader = new StreamReader(csvStream, Encoding.UTF8, true, 1024, true);
                    var csvString = await reader.ReadToEndAsync();
                    return await TransformCsvStringAsync(csvString, configuration, source);
                }
                else
                {
                    throw new ArgumentException($"Unsupported input type: {input?.GetType().Name ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming CSV data");
                throw;
            }
        }

        private async Task<object> TransformDataRecordsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            // Get transformation type
            if (!configuration.TryGetValue("transformationType", out var transformationTypeObj))
            {
                throw new ArgumentException("Transformation type is required");
            }

            var transformationType = transformationTypeObj.ToString();

            // Apply the transformation
            switch (transformationType.ToLowerInvariant())
            {
                case "toCsv":
                    return await ConvertToCsvAsync(records, configuration);

                case "filter":
                    return await FilterRecordsAsync(records, configuration);

                case "map":
                    return await MapFieldsAsync(records, configuration);

                case "aggregate":
                    return await AggregateRecordsAsync(records, configuration);

                default:
                    throw new NotSupportedException($"Transformation type {transformationType} is not supported");
            }
        }

        private async Task<object> TransformCsvStringAsync(string csvContent, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            // Parse CSV configuration
            var delimiter = configuration.TryGetValue("delimiter", out var delimiterObj) ?
                delimiterObj.ToString() : ",";

            var hasHeader = configuration.TryGetValue("hasHeader", out var hasHeaderObj) &&
                hasHeaderObj is bool hasHeaderBool && hasHeaderBool;

            var culture = configuration.TryGetValue("culture", out var cultureObj) ?
                CultureInfo.GetCultureInfo(cultureObj.ToString()) : CultureInfo.InvariantCulture;

            // Create CSV configuration
            var csvConfig = new CsvConfiguration(culture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = hasHeader,
                MissingFieldFound = null,
                BadDataFound = null
            };

            // Parse CSV
            using var reader = new StringReader(csvContent);
            using var csv = new CsvReader(reader, csvConfig);

            // Read records
            var records = new List<DataRecord>();

            if (hasHeader)
            {
                // Read header
                await Task.Run(() => csv.Read());
                csv.ReadHeader();

                // Read records
                while (await Task.Run(() => csv.Read()))
                {
                    var data = new Dictionary<string, object>();

                    foreach (var header in csv.HeaderRecord)
                    {
                        data[header] = csv.GetField(header);
                    }

                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = data,
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "CSV",
                            ["rowNumber"] = csv.Context.Parser.Row.ToString()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };

                    records.Add(record);
                }
            }
            else
            {
                // No header, use index-based field names
                int rowNumber = 0;

                while (await Task.Run(() => csv.Read()))
                {
                    rowNumber++;
                    var data = new Dictionary<string, object>();

                    for (int i = 0; i < csv.Context.Parser.Record.Length; i++)
                    {
                        data[$"Field{i + 1}"] = csv.GetField(i);
                    }

                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = data,
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "CSV",
                            ["rowNumber"] = rowNumber.ToString()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };

                    records.Add(record);
                }
            }

            // Apply additional transformations if specified
            if (configuration.TryGetValue("transformationType", out var transformationTypeObj))
            {
                return await TransformDataRecordsAsync(records, configuration, source);
            }

            return records;
        }

        private async Task<object> ConvertToCsvAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get CSV configuration
            var delimiter = configuration.TryGetValue("delimiter", out var delimiterObj) ?
                delimiterObj.ToString() : ",";

            var includeHeader = !configuration.TryGetValue("includeHeader", out var includeHeaderObj) ||
                (includeHeaderObj is bool includeHeaderBool && includeHeaderBool);

            var culture = configuration.TryGetValue("culture", out var cultureObj) ?
                CultureInfo.GetCultureInfo(cultureObj.ToString()) : CultureInfo.InvariantCulture;

            // Get fields to include
            var includeFields = configuration.TryGetValue("fields", out var fieldsObj) ?
                (fieldsObj as IEnumerable<object>)?.Select(f => f.ToString()).ToList() : null;

            // Create CSV configuration
            var csvConfig = new CsvConfiguration(culture)
            {
                Delimiter = delimiter
            };

            // Convert to CSV
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, csvConfig);

            // Get all field names if not specified
            if (includeFields == null || !includeFields.Any())
            {
                includeFields = records
                    .SelectMany(r => r.Data.Keys)
                    .Distinct()
                    .ToList();
            }

            // Write header
            if (includeHeader)
            {
                foreach (var field in includeFields)
                {
                    csv.WriteField(field);
                }

                csv.NextRecord();
            }

            // Write records
            foreach (var record in records)
            {
                foreach (var field in includeFields)
                {
                    if (record.Data.TryGetValue(field, out var value))
                    {
                        csv.WriteField(value?.ToString());
                    }
                    else
                    {
                        csv.WriteField(string.Empty);
                    }
                }

                csv.NextRecord();
            }

            await csv.FlushAsync();
            return writer.ToString();
        }

        private async Task<object> FilterRecordsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get filter conditions
            if (!configuration.TryGetValue("filterConditions", out var filterConditionsObj))
            {
                throw new ArgumentException("Filter conditions are required for filter transformation");
            }

            var filterConditions = filterConditionsObj as Dictionary<string, object>;
            if (filterConditions == null)
            {
                throw new ArgumentException("Filter conditions must be a dictionary");
            }

            // Apply filter
            var filteredRecords = records.ToList();

            foreach (var condition in filterConditions)
            {
                var field = condition.Key;
                var value = condition.Value;

                filteredRecords = filteredRecords
                    .Where(r => r.Data.TryGetValue(field, out var fieldValue) &&
                               IsMatch(fieldValue, value))
                    .ToList();
            }

            return await Task.FromResult(filteredRecords);
        }

        private async Task<object> MapFieldsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get field mappings
            if (!configuration.TryGetValue("fieldMappings", out var fieldMappingsObj))
            {
                throw new ArgumentException("Field mappings are required for map transformation");
            }

            var fieldMappings = fieldMappingsObj as Dictionary<string, object>;
            if (fieldMappings == null)
            {
                throw new ArgumentException("Field mappings must be a dictionary");
            }

            // Apply mappings
            var mappedRecords = new List<DataRecord>();

            foreach (var record in records)
            {
                var mappedData = new Dictionary<string, object>();

                foreach (var mapping in fieldMappings)
                {
                    var targetField = mapping.Key;
                    var sourceField = mapping.Value.ToString();

                    if (record.Data.TryGetValue(sourceField, out var value))
                    {
                        mappedData[targetField] = value;
                    }
                }

                // Copy fields that are not mapped
                if (configuration.TryGetValue("includeUnmappedFields", out var includeUnmappedFieldsObj) &&
                    includeUnmappedFieldsObj is bool includeUnmappedFields && includeUnmappedFields)
                {
                    foreach (var field in record.Data)
                    {
                        if (!fieldMappings.Values.Contains(field.Key))
                        {
                            mappedData[field.Key] = field.Value;
                        }
                    }
                }

                // Create a new record with mapped data
                var mappedRecord = new DataRecord
                {
                    Id = record.Id,
                    SchemaId = record.SchemaId,
                    SourceId = record.SourceId,
                    Data = mappedData,
                    Metadata = record.Metadata,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                    Version = record.Version
                };

                mappedRecords.Add(mappedRecord);
            }

            return await Task.FromResult(mappedRecords);
        }

        private async Task<object> AggregateRecordsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get group by fields
            if (!configuration.TryGetValue("groupBy", out var groupByObj))
            {
                throw new ArgumentException("Group by fields are required for aggregate transformation");
            }

            var groupByFields = groupByObj as IEnumerable<object>;
            if (groupByFields == null)
            {
                throw new ArgumentException("Group by fields must be a list");
            }

            // Get aggregations
            if (!configuration.TryGetValue("aggregations", out var aggregationsObj))
            {
                throw new ArgumentException("Aggregations are required for aggregate transformation");
            }

            var aggregations = aggregationsObj as Dictionary<string, object>;
            if (aggregations == null)
            {
                throw new ArgumentException("Aggregations must be a dictionary");
            }

            // Group records
            var groups = records.GroupBy(record =>
            {
                var key = new Dictionary<string, object>();

                foreach (var fieldObj in groupByFields)
                {
                    var field = fieldObj.ToString();

                    if (record.Data.TryGetValue(field, out var value))
                    {
                        key[field] = value;
                    }
                    else
                    {
                        key[field] = null;
                    }
                }

                return new GroupKey(key);
            });

            // Apply aggregations
            var aggregatedRecords = new List<DataRecord>();

            foreach (var group in groups)
            {
                var aggregatedData = new Dictionary<string, object>();

                // Add group by fields
                foreach (var field in group.Key.Fields)
                {
                    aggregatedData[field.Key] = field.Value;
                }

                // Apply aggregations
                foreach (var aggregation in aggregations)
                {
                    var targetField = aggregation.Key;
                    var aggregationConfig = aggregation.Value as Dictionary<string, object>;

                    if (aggregationConfig == null)
                    {
                        throw new ArgumentException($"Aggregation configuration for {targetField} must be a dictionary");
                    }

                    if (!aggregationConfig.TryGetValue("type", out var aggregationTypeObj))
                    {
                        throw new ArgumentException($"Aggregation type is required for {targetField}");
                    }

                    var aggregationType = aggregationTypeObj.ToString();

                    if (!aggregationConfig.TryGetValue("field", out var fieldObj))
                    {
                        throw new ArgumentException($"Field is required for {targetField} aggregation");
                    }

                    var field = fieldObj.ToString();

                    // Apply aggregation
                    switch (aggregationType.ToLowerInvariant())
                    {
                        case "sum":
                            aggregatedData[targetField] = group
                                .Where(r => r.Data.TryGetValue(field, out var value) && value != null)
                                .Sum(r => Convert.ToDouble(r.Data[field]));
                            break;

                        case "avg":
                            aggregatedData[targetField] = group
                                .Where(r => r.Data.TryGetValue(field, out var value) && value != null)
                                .Average(r => Convert.ToDouble(r.Data[field]));
                            break;

                        case "min":
                            aggregatedData[targetField] = group
                                .Where(r => r.Data.TryGetValue(field, out var value) && value != null)
                                .Min(r => Convert.ToDouble(r.Data[field]));
                            break;

                        case "max":
                            aggregatedData[targetField] = group
                                .Where(r => r.Data.TryGetValue(field, out var value) && value != null)
                                .Max(r => Convert.ToDouble(r.Data[field]));
                            break;

                        case "count":
                            aggregatedData[targetField] = group.Count();
                            break;

                        case "countdistinct":
                            aggregatedData[targetField] = group
                                .Where(r => r.Data.TryGetValue(field, out var value) && value != null)
                                .Select(r => r.Data[field])
                                .Distinct()
                                .Count();
                            break;

                        case "first":
                            var firstRecord = group.FirstOrDefault();
                            if (firstRecord != null && firstRecord.Data.TryGetValue(field, out var firstValue))
                            {
                                aggregatedData[targetField] = firstValue;
                            }
                            break;

                        case "last":
                            var lastRecord = group.LastOrDefault();
                            if (lastRecord != null && lastRecord.Data.TryGetValue(field, out var lastValue))
                            {
                                aggregatedData[targetField] = lastValue;
                            }
                            break;

                        case "list":
                            aggregatedData[targetField] = group
                                .Where(r => r.Data.TryGetValue(field, out var value) && value != null)
                                .Select(r => r.Data[field])
                                .ToList();
                            break;

                        default:
                            throw new NotSupportedException($"Aggregation type {aggregationType} is not supported");
                    }
                }

                // Create a new record with aggregated data
                var aggregatedRecord = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = records.FirstOrDefault()?.SchemaId,
                    SourceId = records.FirstOrDefault()?.SourceId,
                    Data = aggregatedData,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "Aggregation",
                        ["recordCount"] = group.Count().ToString()
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                aggregatedRecords.Add(aggregatedRecord);
            }

            return await Task.FromResult(aggregatedRecords);
        }

        private bool IsMatch(object fieldValue, object conditionValue)
        {
            if (fieldValue == null && conditionValue == null)
            {
                return true;
            }

            if (fieldValue == null || conditionValue == null)
            {
                return false;
            }

            // Handle special condition values
            if (conditionValue is string conditionString)
            {
                // Check for null condition
                if (conditionString == "null")
                {
                    return fieldValue == null;
                }

                // Check for not null condition
                if (conditionString == "!null")
                {
                    return fieldValue != null;
                }

                // Check for wildcard condition
                if (conditionString.Contains("*"))
                {
                    var pattern = "^" + conditionString.Replace("*", ".*") + "$";
                    return System.Text.RegularExpressions.Regex.IsMatch(fieldValue.ToString(), pattern);
                }

                // Check for range condition
                if (conditionString.StartsWith("[") && conditionString.EndsWith("]") && conditionString.Contains(","))
                {
                    var range = conditionString.Trim('[', ']').Split(',');
                    if (range.Length == 2)
                    {
                        var min = double.Parse(range[0]);
                        var max = double.Parse(range[1]);
                        var value = Convert.ToDouble(fieldValue);

                        return value >= min && value <= max;
                    }
                }

                // Check for comparison conditions
                if (conditionString.StartsWith(">"))
                {
                    var value = double.Parse(conditionString.Substring(1));
                    return Convert.ToDouble(fieldValue) > value;
                }

                if (conditionString.StartsWith("<"))
                {
                    var value = double.Parse(conditionString.Substring(1));
                    return Convert.ToDouble(fieldValue) < value;
                }

                if (conditionString.StartsWith(">="))
                {
                    var value = double.Parse(conditionString.Substring(2));
                    return Convert.ToDouble(fieldValue) >= value;
                }

                if (conditionString.StartsWith("<="))
                {
                    var value = double.Parse(conditionString.Substring(2));
                    return Convert.ToDouble(fieldValue) <= value;
                }

                if (conditionString.StartsWith("!="))
                {
                    var value = conditionString.Substring(2);
                    return !fieldValue.ToString().Equals(value);
                }
            }

            // Default equality check
            return fieldValue.ToString().Equals(conditionValue.ToString());
        }

        private class GroupKey
        {
            public Dictionary<string, object> Fields { get; }

            public GroupKey(Dictionary<string, object> fields)
            {
                Fields = fields;
            }

            public override bool Equals(object obj)
            {
                if (obj is GroupKey other)
                {
                    if (Fields.Count != other.Fields.Count)
                    {
                        return false;
                    }

                    foreach (var field in Fields)
                    {
                        if (!other.Fields.TryGetValue(field.Key, out var otherValue) ||
                            !Equals(field.Value, otherValue))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            public override int GetHashCode()
            {
                var hash = 17;

                foreach (var field in Fields)
                {
                    hash = hash * 23 + (field.Key?.GetHashCode() ?? 0);
                    hash = hash * 23 + (field.Value?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }
    }
}
