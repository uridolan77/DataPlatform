using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.IngestionService.Connectors.Base
{
    public interface IDataConnector
    {
        Task<bool> ValidateConnectionAsync(DataSourceDefinition source);
        Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null);
        Task<IAsyncEnumerable<DataRecord>> StreamDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null);
        Task<DataSchema> InferSchemaAsync(DataSourceDefinition source);
        Task<DataIngestCheckpoint> GetLatestCheckpointAsync(string sourceId);
        Task SaveCheckpointAsync(DataIngestCheckpoint checkpoint);
    }

    public class DataIngestCheckpoint
    {
        public string SourceId { get; set; }
        public string CheckpointValue { get; set; } // Can be a timestamp, position, etc.
        public DateTime ProcessedAt { get; set; }
        public long RecordsProcessed { get; set; }
        public Dictionary<string, string> AdditionalInfo { get; set; }
    }
}
