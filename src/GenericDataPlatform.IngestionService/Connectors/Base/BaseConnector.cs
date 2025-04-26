using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.Base
{
    public abstract class BaseConnector : IDataConnector
    {
        protected readonly ILogger _logger;

        protected BaseConnector(ILogger logger)
        {
            _logger = logger;
        }

        public abstract Task<bool> ValidateConnectionAsync(DataSourceDefinition source);

        public abstract Task<IEnumerable<DataRecord>> FetchDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null);

        public abstract Task<IAsyncEnumerable<DataRecord>> StreamDataAsync(DataSourceDefinition source, Dictionary<string, object> parameters = null);

        public abstract Task<DataSchema> InferSchemaAsync(DataSourceDefinition source);

        public abstract Task<DataIngestCheckpoint> GetLatestCheckpointAsync(string sourceId);

        public abstract Task SaveCheckpointAsync(DataIngestCheckpoint checkpoint);

        protected virtual void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        protected virtual void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        protected virtual void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }
    }
}
