using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Checkpoints;
using GenericDataPlatform.IngestionService.Connectors.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace GenericDataPlatform.IngestionService.Connectors.FileSystem
{
    /// <summary>
    /// Extension methods for IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Splits an enumerable into batches of a specified size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }

    public abstract class BaseFileSystemConnector : BaseConnector
    {
        private readonly CheckpointStorageFactory _checkpointStorageFactory;
        private readonly FileSystemConnectorOptions _options;

        protected BaseFileSystemConnector(
            ILogger logger,
            CheckpointStorageFactory checkpointStorageFactory,
            IOptions<FileSystemConnectorOptions> options) : base(logger)
        {
            _checkpointStorageFactory = checkpointStorageFactory ?? throw new ArgumentNullException(nameof(checkpointStorageFactory));
            _options = options?.Value ?? new FileSystemConnectorOptions();
        }

        public override async Task<bool> ValidateConnectionAsync(DataSourceDefinition source)
        {
            try
            {
                // Check if we can list files
                var files = await ListFilesAsync(source, null);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error validating connection to file system {source}", source.Name);
                return false;
            }
        }

        public override async Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null)
        {
            try
            {
                // Create a retry policy for transient errors
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

                // Execute with retry policy
                return await retryPolicy.ExecuteAsync(async () =>
                {
                    // Get file pattern
                    if (!source.ConnectionProperties.TryGetValue("filePattern", out var filePattern))
                    {
                        filePattern = "*.*"; // Default to all files
                    }

                    // Get file format
                    if (!source.ConnectionProperties.TryGetValue("format", out var format))
                    {
                        // Try to infer format from file extension
                        format = InferFormatFromPattern(filePattern);
                    }

                    // Check if we should use incremental loading
                    bool useIncremental = source.IngestMode == DataIngestMode.Incremental;
                    DateTime? lastProcessedTime = null;

                    if (useIncremental)
                    {
                        // Get the latest checkpoint
                        var checkpoint = await GetLatestCheckpointAsync(source.Id);

                        // Parse the checkpoint value as a timestamp
                        if (!string.IsNullOrEmpty(checkpoint.CheckpointValue) &&
                            DateTime.TryParse(checkpoint.CheckpointValue, out var checkpointTime))
                        {
                            lastProcessedTime = checkpointTime;
                            _logger.LogInformation("Using incremental load with checkpoint time: {checkpointTime}", checkpointTime);
                        }
                    }

                    // List files
                    var files = await ListFilesAsync(source, filePattern);

                    // Apply incremental filter if needed
                    if (useIncremental && lastProcessedTime.HasValue)
                    {
                        // Filter files based on last modified time
                        // Note: This requires the implementation of GetFileLastModifiedTimeAsync in derived classes
                        var filteredFiles = new List<string>();

                        foreach (var file in files)
                        {
                            var lastModified = await GetFileLastModifiedTimeAsync(source, file);

                            if (lastModified > lastProcessedTime.Value)
                            {
                                filteredFiles.Add(file);
                            }
                        }

                        files = filteredFiles;
                        _logger.LogInformation("Filtered to {count} files after checkpoint time {checkpointTime}",
                            files.Count(), lastProcessedTime.Value);
                    }

                    // Check if we need to limit the number of files
                    if (parameters != null && parameters.TryGetValue("maxFiles", out var maxFilesObj) &&
                        int.TryParse(maxFilesObj.ToString(), out var maxFiles) && maxFiles > 0)
                    {
                        files = files.Take(maxFiles).ToList();
                    }

                    // Check if we need to sort files
                    if (parameters != null && parameters.TryGetValue("sortBy", out var sortByObj))
                    {
                        string sortBy = sortByObj.ToString();
                        bool sortDescending = parameters.TryGetValue("sortDescending", out var sortDescObj) &&
                            bool.TryParse(sortDescObj.ToString(), out var sortDesc) && sortDesc;

                        // Sort files based on the specified criteria
                        files = await SortFilesAsync(source, files, sortBy, sortDescending);
                    }

                    // Process each file
                    var allRecords = new List<DataRecord>();
                    DateTime? latestFileTime = null;
                    int totalProcessedFiles = 0;
                    int totalProcessedRecords = 0;

                    // Check if we should process files in parallel
                    bool processInParallel = source.ConnectionProperties.TryGetValue("parallelProcessing", out var parallelStr) &&
                        bool.TryParse(parallelStr, out var parallelBool) && parallelBool;

                    // Get batch size for parallel processing
                    int batchSize = 5; // Default batch size
                    if (processInParallel && source.ConnectionProperties.TryGetValue("batchSize", out var batchSizeStr) &&
                        int.TryParse(batchSizeStr, out var configuredBatchSize) && configuredBatchSize > 0)
                    {
                        batchSize = configuredBatchSize;
                    }

                    if (processInParallel)
                    {
                        // Process files in parallel batches
                        foreach (var fileBatch in files.Batch(batchSize))
                        {
                            var batchTasks = fileBatch.Select(async file =>
                            {
                                try
                                {
                                    // Get file last modified time
                                    var fileTime = await GetFileLastModifiedTimeAsync(source, file);

                                    // Read file content
                                    using var stream = await ReadFileAsync(source, file);

                                    // Parse file based on format
                                    var records = await ParseFileAsync(stream, format, source);

                                    // Add file metadata to each record
                                    foreach (var record in records)
                                    {
                                        record.Metadata["fileName"] = file;
                                        record.Metadata["fileFormat"] = format;
                                        record.Metadata["fileLastModified"] = fileTime.ToString("o");
                                    }

                                    // Update latest file time for checkpoint
                                    lock (this)
                                    {
                                        if (!latestFileTime.HasValue || fileTime > latestFileTime.Value)
                                        {
                                            latestFileTime = fileTime;
                                        }

                                        totalProcessedFiles++;
                                        totalProcessedRecords += records.Count();
                                    }

                                    return records;
                                }
                                catch (Exception ex)
                                {
                                    LogError(ex, "Error processing file {file}", file);
                                    return Enumerable.Empty<DataRecord>();
                                }
                            }).ToList();

                            // Wait for all batch tasks to complete
                            var batchResults = await Task.WhenAll(batchTasks);

                            // Add all records from the batch
                            foreach (var batchRecords in batchResults)
                            {
                                allRecords.AddRange(batchRecords);
                            }
                        }
                    }
                    else
                    {
                        // Process files sequentially
                        foreach (var file in files)
                        {
                            try
                            {
                                // Get file last modified time
                                var fileTime = await GetFileLastModifiedTimeAsync(source, file);

                                // Read file content
                                using var stream = await ReadFileAsync(source, file);

                                // Parse file based on format
                                var records = await ParseFileAsync(stream, format, source);

                                // Add file metadata to each record
                                foreach (var record in records)
                                {
                                    record.Metadata["fileName"] = file;
                                    record.Metadata["fileFormat"] = format;
                                    record.Metadata["fileLastModified"] = fileTime.ToString("o");

                                    allRecords.Add(record);
                                }

                                // Update latest file time for checkpoint
                                if (!latestFileTime.HasValue || fileTime > latestFileTime.Value)
                                {
                                    latestFileTime = fileTime;
                                }

                                totalProcessedFiles++;
                                totalProcessedRecords += records.Count();
                            }
                            catch (Exception ex)
                            {
                                LogError(ex, "Error processing file {file}", file);

                                // Check if we should continue on error
                                bool continueOnError = source.ConnectionProperties.TryGetValue("continueOnError", out var continueOnErrorStr) &&
                                    bool.TryParse(continueOnErrorStr, out var continueOnErrorBool) && continueOnErrorBool;

                                if (!continueOnError)
                                {
                                    throw;
                                }
                            }
                        }
                    }

                    // Update checkpoint if we processed any files and incremental loading is enabled
                    if (useIncremental && latestFileTime.HasValue && files.Any())
                    {
                        var checkpoint = new DataIngestCheckpoint
                        {
                            SourceId = source.Id,
                            CheckpointValue = latestFileTime.Value.ToString("o"),
                            ProcessedAt = DateTime.UtcNow,
                            RecordsProcessed = totalProcessedRecords,
                            AdditionalInfo = new Dictionary<string, string>
                            {
                                ["filesProcessed"] = totalProcessedFiles.ToString(),
                                ["recordsProcessed"] = totalProcessedRecords.ToString()
                            }
                        };

                        await SaveCheckpointAsync(checkpoint);
                    }

                    return allRecords;
                });
            }
            catch (Exception ex)
            {
                LogError(ex, "Error fetching data from file system {source}", source.Name);
                throw;
            }
        }

        /// <summary>
        /// Gets the last modified time of a file
        /// </summary>
        protected virtual async Task<DateTime> GetFileLastModifiedTimeAsync(DataSourceDefinition source, string filePath)
        {
            // Default implementation - derived classes should override this
            // For local file system, this would use File.GetLastWriteTimeUtc
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Sorts files based on the specified criteria
        /// </summary>
        protected virtual async Task<IEnumerable<string>> SortFilesAsync(
            DataSourceDefinition source,
            IEnumerable<string> files,
            string sortBy,
            bool descending)
        {
            switch (sortBy.ToLowerInvariant())
            {
                case "name":
                    return descending
                        ? files.OrderByDescending(f => f)
                        : files.OrderBy(f => f);

                case "lastmodified":
                    // Get last modified times for all files
                    var fileTimePairs = new List<(string FilePath, DateTime LastModified)>();

                    foreach (var file in files)
                    {
                        var lastModified = await GetFileLastModifiedTimeAsync(source, file);
                        fileTimePairs.Add((file, lastModified));
                    }

                    // Sort by last modified time
                    return descending
                        ? fileTimePairs.OrderByDescending(p => p.LastModified).Select(p => p.FilePath)
                        : fileTimePairs.OrderBy(p => p.LastModified).Select(p => p.FilePath);

                default:
                    return files; // No sorting
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
                // Get file pattern
                if (!source.ConnectionProperties.TryGetValue("filePattern", out var filePattern))
                {
                    filePattern = "*.*"; // Default to all files
                }

                // Get file format
                if (!source.ConnectionProperties.TryGetValue("format", out var format))
                {
                    // Try to infer format from file extension
                    format = InferFormatFromPattern(filePattern);
                }

                // List files
                var files = await ListFilesAsync(source, filePattern);

                if (!files.Any())
                {
                    throw new InvalidOperationException("No files found to infer schema");
                }

                // Get the first file
                var firstFile = files.First();

                // Read file content
                using var stream = await ReadFileAsync(source, firstFile);

                // Parse a sample of the file to infer schema
                var sampleRecords = await ParseFileAsync(stream, format, source, sampleSize: 10);

                // Infer schema from the sample records
                return InferSchemaFromSample(sampleRecords, source);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error inferring schema for file system {source}", source.Name);
                throw;
            }
        }

        public override async Task<DataIngestCheckpoint> GetLatestCheckpointAsync(string sourceId)
        {
            try
            {
                // Create a retry policy for transient errors
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

                // Execute with retry policy
                return await retryPolicy.ExecuteAsync(async () =>
                {
                    // Try to get checkpoint from the configured storage
                    var checkpointStorage = GetCheckpointStorage();

                    if (checkpointStorage == null)
                    {
                        // If no storage is configured, return a default checkpoint
                        return new DataIngestCheckpoint
                        {
                            SourceId = sourceId,
                            CheckpointValue = DateTime.UtcNow.AddDays(-1).ToString("o"),
                            ProcessedAt = DateTime.UtcNow.AddDays(-1),
                            RecordsProcessed = 0,
                            AdditionalInfo = new Dictionary<string, string>()
                        };
                    }

                    // Try to load the checkpoint from storage
                    var checkpointKey = $"filesystem_checkpoint_{sourceId}";
                    var checkpointJson = await checkpointStorage.GetValueAsync(checkpointKey);

                    if (string.IsNullOrEmpty(checkpointJson))
                    {
                        // No checkpoint found, return a default one
                        return new DataIngestCheckpoint
                        {
                            SourceId = sourceId,
                            CheckpointValue = DateTime.UtcNow.AddDays(-1).ToString("o"),
                            ProcessedAt = DateTime.UtcNow.AddDays(-1),
                            RecordsProcessed = 0,
                            AdditionalInfo = new Dictionary<string, string>()
                        };
                    }

                    // Deserialize the checkpoint
                    try
                    {
                        return JsonSerializer.Deserialize<DataIngestCheckpoint>(checkpointJson);
                    }
                    catch (JsonException ex)
                    {
                        LogError(ex, "Error deserializing checkpoint for source {sourceId}", sourceId);

                        // Return a default checkpoint if deserialization fails
                        return new DataIngestCheckpoint
                        {
                            SourceId = sourceId,
                            CheckpointValue = DateTime.UtcNow.AddDays(-1).ToString("o"),
                            ProcessedAt = DateTime.UtcNow.AddDays(-1),
                            RecordsProcessed = 0,
                            AdditionalInfo = new Dictionary<string, string>()
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving checkpoint for source {sourceId}", sourceId);

                // Return a default checkpoint in case of error
                return new DataIngestCheckpoint
                {
                    SourceId = sourceId,
                    CheckpointValue = DateTime.UtcNow.AddDays(-1).ToString("o"),
                    ProcessedAt = DateTime.UtcNow.AddDays(-1),
                    RecordsProcessed = 0,
                    AdditionalInfo = new Dictionary<string, string>()
                };
            }
        }

        public override async Task SaveCheckpointAsync(DataIngestCheckpoint checkpoint)
        {
            try
            {
                // Create a retry policy for transient errors
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

                // Execute with retry policy
                await retryPolicy.ExecuteAsync(async () =>
                {
                    // Get the checkpoint storage
                    var checkpointStorage = GetCheckpointStorage();

                    if (checkpointStorage == null)
                    {
                        // If no storage is configured, log a warning and return
                        _logger.LogWarning("No checkpoint storage configured. Checkpoint for source {sourceId} will not be saved.", checkpoint.SourceId);
                        return;
                    }

                    // Serialize the checkpoint
                    var checkpointJson = JsonSerializer.Serialize(checkpoint);

                    // Save to storage
                    var checkpointKey = $"filesystem_checkpoint_{checkpoint.SourceId}";
                    await checkpointStorage.SetValueAsync(checkpointKey, checkpointJson);

                    _logger.LogInformation("Saved checkpoint for source {sourceId} with value {checkpointValue}",
                        checkpoint.SourceId, checkpoint.CheckpointValue);
                });
            }
            catch (Exception ex)
            {
                LogError(ex, "Error saving checkpoint for source {sourceId}", checkpoint.SourceId);
            }
        }

        /// <summary>
        /// Gets the checkpoint storage implementation based on configuration
        /// </summary>
        protected virtual ICheckpointStorage GetCheckpointStorage()
        {
            try
            {
                // Get the checkpoint storage type from options
                var storageType = _options.CheckpointStorageType;

                // Create the storage using the factory
                return _checkpointStorageFactory.CreateStorage(storageType);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error creating checkpoint storage. Checkpoints will not be persisted.");
                return null;
            }
        }

        // Using the ICheckpointStorage interface from the Checkpoints namespace

        protected abstract Task<IEnumerable<string>> ListFilesAsync(DataSourceDefinition source, string filePattern);

        protected abstract Task<Stream> ReadFileAsync(DataSourceDefinition source, string filePath);

        protected virtual string InferFormatFromPattern(string filePattern)
        {
            var extension = Path.GetExtension(filePattern).ToLowerInvariant();

            switch (extension)
            {
                case ".csv":
                    return "csv";
                case ".json":
                    return "json";
                case ".xml":
                    return "xml";
                case ".parquet":
                    return "parquet";
                case ".avro":
                    return "avro";
                case ".txt":
                    return "text";
                default:
                    return "binary";
            }
        }

        protected virtual async Task<IEnumerable<DataRecord>> ParseFileAsync(Stream stream, string format, DataSourceDefinition source, int sampleSize = 0)
        {
            // Check if the stream is compressed and decompress if needed
            stream = await DecompressStreamIfNeededAsync(stream, source);

            switch (format.ToLowerInvariant())
            {
                case "csv":
                    return await ParseCsvAsync(stream, source, sampleSize);
                case "json":
                    return await ParseJsonAsync(stream, source, sampleSize);
                case "xml":
                    return await ParseXmlAsync(stream, source, sampleSize);
                case "text":
                    return await ParseTextAsync(stream, source, sampleSize);
                case "parquet":
                    return await ParseParquetAsync(stream, source, sampleSize);
                case "avro":
                    return await ParseAvroAsync(stream, source, sampleSize);
                default:
                    throw new NotSupportedException($"File format {format} is not supported");
            }
        }

        protected virtual async Task<Stream> DecompressStreamIfNeededAsync(Stream stream, DataSourceDefinition source)
        {
            // Check if compression detection is enabled
            bool detectCompression = source.ConnectionProperties.TryGetValue("detectCompression", out var detectCompressionStr) &&
                bool.TryParse(detectCompressionStr, out var detectCompressionBool) && detectCompressionBool;

            if (!detectCompression)
            {
                return stream;
            }

            // Reset stream position
            stream.Position = 0;

            // Read the first few bytes to detect compression
            byte[] header = new byte[4];
            int bytesRead = await stream.ReadAsync(header, 0, 4);

            // Reset stream position
            stream.Position = 0;

            // Check for GZIP magic number (1F 8B)
            if (bytesRead >= 2 && header[0] == 0x1F && header[1] == 0x8B)
            {
                return new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false);
            }

            // Check for ZIP magic number (50 4B 03 04)
            if (bytesRead >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
            {
                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
                var entry = zipArchive.Entries.FirstOrDefault();

                if (entry != null)
                {
                    return entry.Open();
                }
            }

            // No compression detected or unsupported format
            return stream;
        }

        protected virtual async Task<IEnumerable<DataRecord>> ParseCsvAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();

            // Reset stream position
            stream.Position = 0;

            // Read the stream
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);

            // Check if the CSV has a header
            bool hasHeader = source.ConnectionProperties.TryGetValue("hasHeader", out var hasHeaderStr) &&
                bool.TryParse(hasHeaderStr, out var hasHeaderBool) && hasHeaderBool;

            // Get delimiter
            char delimiter = source.ConnectionProperties.TryGetValue("delimiter", out var delimiterStr) ?
                delimiterStr[0] : ',';

            // Read header
            string[] headers = null;
            string line = await reader.ReadLineAsync();

            if (line != null)
            {
                if (hasHeader)
                {
                    headers = line.Split(delimiter);
                    line = await reader.ReadLineAsync();
                }
                else
                {
                    // Generate column names (Column1, Column2, etc.)
                    var columnCount = line.Split(delimiter).Length;
                    headers = Enumerable.Range(1, columnCount).Select(i => $"Column{i}").ToArray();
                }
            }

            // Read data rows
            int rowCount = 0;
            while (line != null && (sampleSize == 0 || rowCount < sampleSize))
            {
                var values = line.Split(delimiter);
                var data = new Dictionary<string, object>();

                for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
                {
                    data[headers[i]] = values[i];
                }

                // Create a record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "CSV",
                        ["rowNumber"] = (rowCount + 1).ToString()
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                records.Add(record);
                rowCount++;

                line = await reader.ReadLineAsync();
            }

            return records;
        }

        protected virtual async Task<IEnumerable<DataRecord>> ParseJsonAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();

            // Reset stream position
            stream.Position = 0;

            // Read the stream
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            var json = await reader.ReadToEndAsync();

            // Parse JSON
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            // Check if the root is an array
            if (root.ValueKind == JsonValueKind.Array)
            {
                // Process each array element as a record
                int count = 0;
                foreach (var element in root.EnumerateArray())
                {
                    if (sampleSize > 0 && count >= sampleSize)
                    {
                        break;
                    }

                    var data = new Dictionary<string, object>();

                    // Process properties
                    foreach (var property in element.EnumerateObject())
                    {
                        switch (property.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                data[property.Name] = property.Value.GetString();
                                break;

                            case JsonValueKind.Number:
                                if (property.Value.TryGetInt64(out var intValue))
                                {
                                    data[property.Name] = intValue;
                                }
                                else if (property.Value.TryGetDouble(out var doubleValue))
                                {
                                    data[property.Name] = doubleValue;
                                }
                                break;

                            case JsonValueKind.True:
                                data[property.Name] = true;
                                break;

                            case JsonValueKind.False:
                                data[property.Name] = false;
                                break;

                            case JsonValueKind.Null:
                                data[property.Name] = null;
                                break;

                            case JsonValueKind.Object:
                            case JsonValueKind.Array:
                                // For complex types, store the JSON string
                                data[property.Name] = property.Value.GetRawText();
                                break;
                        }
                    }

                    // Create a record
                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = data,
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "FileSystem",
                            ["format"] = "JSON",
                            ["index"] = count.ToString()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };

                    records.Add(record);
                    count++;
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Process the object as a single record
                var data = new Dictionary<string, object>();

                // Process properties
                foreach (var property in root.EnumerateObject())
                {
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            data[property.Name] = property.Value.GetString();
                            break;

                        case JsonValueKind.Number:
                            if (property.Value.TryGetInt64(out var intValue))
                            {
                                data[property.Name] = intValue;
                            }
                            else if (property.Value.TryGetDouble(out var doubleValue))
                            {
                                data[property.Name] = doubleValue;
                            }
                            break;

                        case JsonValueKind.True:
                            data[property.Name] = true;
                            break;

                        case JsonValueKind.False:
                            data[property.Name] = false;
                            break;

                        case JsonValueKind.Null:
                            data[property.Name] = null;
                            break;

                        case JsonValueKind.Object:
                        case JsonValueKind.Array:
                            // For complex types, store the JSON string
                            data[property.Name] = property.Value.GetRawText();
                            break;
                    }
                }

                // Create a record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "JSON"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                records.Add(record);
            }

            return records;
        }

        protected virtual async Task<IEnumerable<DataRecord>> ParseXmlAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();

            // Reset stream position
            stream.Position = 0;

            try
            {
                // Load the XML document
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.Load(stream);

                // Get the root element
                var root = xmlDoc.DocumentElement;
                if (root == null)
                {
                    return records;
                }

                // Get the record element name
                string recordElementName = source.ConnectionProperties.TryGetValue("recordElement", out var recordElement) ?
                    recordElement : null;

                // If record element is specified, find all matching elements
                if (!string.IsNullOrEmpty(recordElementName))
                {
                    var recordNodes = root.GetElementsByTagName(recordElementName);
                    int count = 0;

                    foreach (System.Xml.XmlNode node in recordNodes)
                    {
                        if (sampleSize > 0 && count >= sampleSize)
                        {
                            break;
                        }

                        var data = ExtractDataFromXmlNode(node);

                        // Create a record
                        var record = new DataRecord
                        {
                            Id = Guid.NewGuid().ToString(),
                            SchemaId = source.Schema?.Id,
                            SourceId = source.Id,
                            Data = data,
                            Metadata = new Dictionary<string, string>
                            {
                                ["source"] = "FileSystem",
                                ["format"] = "XML",
                                ["index"] = count.ToString()
                            },
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = "1.0"
                        };

                        records.Add(record);
                        count++;
                    }
                }
                else
                {
                    // Try to find repeating child elements under the root
                    var childElements = root.ChildNodes.OfType<System.Xml.XmlElement>().ToList();

                    if (childElements.Count > 0)
                    {
                        // Group by element name to find the most common
                        var groupedByName = childElements.GroupBy(e => e.Name)
                            .OrderByDescending(g => g.Count())
                            .First();

                        // If there are multiple elements with the same name, treat them as records
                        if (groupedByName.Count() > 1)
                        {
                            int count = 0;
                            foreach (var element in groupedByName)
                            {
                                if (sampleSize > 0 && count >= sampleSize)
                                {
                                    break;
                                }

                                var data = ExtractDataFromXmlNode(element);

                                // Create a record
                                var record = new DataRecord
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    SchemaId = source.Schema?.Id,
                                    SourceId = source.Id,
                                    Data = data,
                                    Metadata = new Dictionary<string, string>
                                    {
                                        ["source"] = "FileSystem",
                                        ["format"] = "XML",
                                        ["index"] = count.ToString()
                                    },
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    Version = "1.0"
                                };

                                records.Add(record);
                                count++;
                            }
                        }
                        else
                        {
                            // Treat the root element as a single record
                            var data = ExtractDataFromXmlNode(root);

                            // Create a record
                            var record = new DataRecord
                            {
                                Id = Guid.NewGuid().ToString(),
                                SchemaId = source.Schema?.Id,
                                SourceId = source.Id,
                                Data = data,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["source"] = "FileSystem",
                                    ["format"] = "XML"
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
                        // Treat the root element as a single record
                        var data = ExtractDataFromXmlNode(root);

                        // Create a record
                        var record = new DataRecord
                        {
                            Id = Guid.NewGuid().ToString(),
                            SchemaId = source.Schema?.Id,
                            SourceId = source.Id,
                            Data = data,
                            Metadata = new Dictionary<string, string>
                            {
                                ["source"] = "FileSystem",
                                ["format"] = "XML"
                            },
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = "1.0"
                        };

                        records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error parsing XML file");
            }

            return await Task.FromResult(records);
        }

        private Dictionary<string, object> ExtractDataFromXmlNode(System.Xml.XmlNode node)
        {
            var data = new Dictionary<string, object>();

            // Extract attributes
            if (node.Attributes != null)
            {
                foreach (System.Xml.XmlAttribute attr in node.Attributes)
                {
                    data[$"@{attr.Name}"] = attr.Value;
                }
            }

            // Extract child elements
            foreach (System.Xml.XmlNode childNode in node.ChildNodes)
            {
                if (childNode is System.Xml.XmlElement childElement)
                {
                    // Check if this element has child elements
                    var hasChildElements = childElement.ChildNodes.OfType<System.Xml.XmlElement>().Any();

                    if (hasChildElements)
                    {
                        // Complex element, extract recursively
                        var childData = ExtractDataFromXmlNode(childElement);

                        // Check if we already have this element name
                        if (data.ContainsKey(childElement.Name))
                        {
                            // Convert to array if not already
                            if (data[childElement.Name] is List<Dictionary<string, object>> list)
                            {
                                list.Add(childData);
                            }
                            else if (data[childElement.Name] is Dictionary<string, object> dict)
                            {
                                data[childElement.Name] = new List<Dictionary<string, object>> { dict, childData };
                            }
                            else
                            {
                                // Unexpected type, overwrite
                                data[childElement.Name] = childData;
                            }
                        }
                        else
                        {
                            data[childElement.Name] = childData;
                        }
                    }
                    else
                    {
                        // Simple element, extract text value
                        var value = childElement.InnerText;

                        // Try to parse the value
                        if (int.TryParse(value, out var intValue))
                        {
                            data[childElement.Name] = intValue;
                        }
                        else if (double.TryParse(value, out var doubleValue))
                        {
                            data[childElement.Name] = doubleValue;
                        }
                        else if (bool.TryParse(value, out var boolValue))
                        {
                            data[childElement.Name] = boolValue;
                        }
                        else if (DateTime.TryParse(value, out var dateValue))
                        {
                            data[childElement.Name] = dateValue;
                        }
                        else
                        {
                            data[childElement.Name] = value;
                        }
                    }
                }
                else if (childNode is System.Xml.XmlText textNode && node.ChildNodes.Count == 1)
                {
                    // If this node has only a text child, store the text value
                    data["_value"] = textNode.Value;
                }
            }

            return data;
        }

        protected virtual async Task<IEnumerable<DataRecord>> ParseTextAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            var records = new List<DataRecord>();

            // Reset stream position
            stream.Position = 0;

            // Read the stream
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);

            // Check if we should treat each line as a record
            bool lineByLine = source.ConnectionProperties.TryGetValue("lineByLine", out var lineByLineStr) &&
                bool.TryParse(lineByLineStr, out var lineByLineBool) && lineByLineBool;

            if (lineByLine)
            {
                // Read line by line
                int lineNumber = 0;
                string line;

                while ((line = await reader.ReadLineAsync()) != null && (sampleSize == 0 || lineNumber < sampleSize))
                {
                    var data = new Dictionary<string, object>
                    {
                        ["text"] = line
                    };

                    // Create a record
                    var record = new DataRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        SchemaId = source.Schema?.Id,
                        SourceId = source.Id,
                        Data = data,
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "FileSystem",
                            ["format"] = "Text",
                            ["lineNumber"] = lineNumber.ToString()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = "1.0"
                    };

                    records.Add(record);
                    lineNumber++;
                }
            }
            else
            {
                // Read the entire file as a single record
                var text = await reader.ReadToEndAsync();

                var data = new Dictionary<string, object>
                {
                    ["text"] = text
                };

                // Create a record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "Text"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                records.Add(record);
            }

            return records;
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
                    Type = SchemaType.Dynamic,
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
                Type = SchemaType.Dynamic,
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

        protected virtual async Task<IEnumerable<DataRecord>> ParseParquetAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            // This is a placeholder implementation for Parquet parsing
            // In a real implementation, you would use a library like Parquet.NET or Apache Arrow

            var records = new List<DataRecord>();

            try
            {
                // Reset stream position
                stream.Position = 0;

                // Create a simulated record for demonstration purposes
                var data = new Dictionary<string, object>
                {
                    ["message"] = "Parquet parsing is not fully implemented yet. This is a placeholder."
                };

                // Add file metadata
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "Parquet",
                        ["note"] = "This is a placeholder implementation"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                records.Add(record);

                // Log a warning that this is not fully implemented
                _logger.LogWarning("Parquet parsing is not fully implemented. Using placeholder implementation.");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error parsing Parquet file");
            }

            return records;
        }

        protected virtual async Task<IEnumerable<DataRecord>> ParseAvroAsync(Stream stream, DataSourceDefinition source, int sampleSize = 0)
        {
            // This is a placeholder implementation for Avro parsing
            // In a real implementation, you would use a library like Apache.Avro

            var records = new List<DataRecord>();

            try
            {
                // Reset stream position
                stream.Position = 0;

                // Create a simulated record for demonstration purposes
                var data = new Dictionary<string, object>
                {
                    ["message"] = "Avro parsing is not fully implemented yet. This is a placeholder."
                };

                // Add file metadata
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "FileSystem",
                        ["format"] = "Avro",
                        ["note"] = "This is a placeholder implementation"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                records.Add(record);

                // Log a warning that this is not fully implemented
                _logger.LogWarning("Avro parsing is not fully implemented. Using placeholder implementation.");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error parsing Avro file");
            }

            return records;
        }

        protected override void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }
    }
}
