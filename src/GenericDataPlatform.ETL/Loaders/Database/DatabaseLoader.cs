using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Loaders;
using GenericDataPlatform.ETL.Loaders.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Loaders.Database
{
    public class DatabaseLoader : ILoader
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DatabaseLoader> _logger;

        public string Type => "Database";

        public DatabaseLoader(IHttpClientFactory httpClientFactory, ILogger<DatabaseLoader> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<object> LoadAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Ensure the input is a list of DataRecord objects
                if (!(input is IEnumerable<DataRecord> inputRecords))
                {
                    throw new ArgumentException("Input must be a list of DataRecord objects");
                }

                var records = inputRecords.ToList();

                // Check if there are any records to load
                if (!records.Any())
                {
                    return LoadResult.Success(
                        destinationId: null,
                        recordsProcessed: 0,
                        metadata: new Dictionary<string, object>
                        {
                            { "message", "No records to load" }
                        });
                }

                // Get configuration values
                if (!configuration.TryGetValue("databaseServiceUrl", out var databaseServiceUrlObj))
                {
                    throw new ArgumentException("Database service URL is required");
                }

                var databaseServiceUrl = databaseServiceUrlObj.ToString();

                if (!configuration.TryGetValue("destinationSourceId", out var destinationSourceIdObj))
                {
                    throw new ArgumentException("Destination source ID is required");
                }

                var destinationSourceId = destinationSourceIdObj.ToString();

                // Get batch size (default to 100)
                configuration.TryGetValue("batchSize", out var batchSizeObj);
                var batchSize = batchSizeObj != null && int.TryParse(batchSizeObj.ToString(), out var bs) ? bs : 100;

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();

                // Process records in batches
                var totalRecordsProcessed = 0;
                var batches = records.Select((record, index) => new { record, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.record).ToList())
                    .ToList();

                foreach (var batch in batches)
                {
                    // Update source ID for each record
                    foreach (var record in batch)
                    {
                        record.SourceId = destinationSourceId;
                    }

                    // Send the batch to the database service
                    var url = $"{databaseServiceUrl.TrimEnd('/')}/api/data/records/batch";
                    var content = new StringContent(JsonSerializer.Serialize(batch), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();

                    totalRecordsProcessed += batch.Count;
                }

                return LoadResult.Success(
                    destinationId: destinationSourceId,
                    recordsProcessed: totalRecordsProcessed,
                    metadata: new Dictionary<string, object>
                    {
                        { "batchCount", batches.Count },
                        { "batchSize", batchSize },
                        { "databaseServiceUrl", databaseServiceUrl }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data to database");

                // Get destination ID if available
                string destinationId = null;
                if (configuration.TryGetValue("destinationSourceId", out var destinationSourceIdObj))
                {
                    destinationId = destinationSourceIdObj.ToString();
                }

                return LoadResult.Failure(
                    destinationId: destinationId,
                    errorMessage: $"Failed to load data to database: {ex.Message}",
                    errorDetails: ex.ToString());
            }
        }
    }
}
