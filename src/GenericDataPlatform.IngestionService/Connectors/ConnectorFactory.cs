using System;
using System.Collections.Generic;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors.Base;
using GenericDataPlatform.IngestionService.Connectors.Database;
using GenericDataPlatform.IngestionService.Connectors.FileSystem;
using GenericDataPlatform.IngestionService.Connectors.Rest;
using GenericDataPlatform.IngestionService.Connectors.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors
{
    public class ConnectorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConnectorFactory> _logger;
        
        public ConnectorFactory(IServiceProvider serviceProvider, ILogger<ConnectorFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public IDataConnector CreateConnector(DataSourceDefinition source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            switch (source.Type)
            {
                case DataSourceType.Database:
                    return CreateDatabaseConnector(source);
                
                case DataSourceType.Rest:
                    return _serviceProvider.GetRequiredService<RestApiConnector>();
                
                case DataSourceType.FileSystem:
                    return CreateFileSystemConnector(source);
                
                case DataSourceType.Streaming:
                    return CreateStreamingConnector(source);
                
                default:
                    throw new NotSupportedException($"Data source type {source.Type} is not supported");
            }
        }
        
        private IDataConnector CreateDatabaseConnector(DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("provider", out var provider))
            {
                throw new ArgumentException("Database provider is required for database connection");
            }
            
            switch (provider.ToLowerInvariant())
            {
                case "sqlserver":
                    return _serviceProvider.GetRequiredService<SqlServerConnector>();
                
                case "mysql":
                    return _serviceProvider.GetRequiredService<MySqlConnector>();
                
                case "postgresql":
                    return _serviceProvider.GetRequiredService<PostgreSqlConnector>();
                
                default:
                    throw new NotSupportedException($"Database provider {provider} is not supported");
            }
        }
        
        private IDataConnector CreateFileSystemConnector(DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("provider", out var provider))
            {
                // Default to local file system
                provider = "local";
            }
            
            switch (provider.ToLowerInvariant())
            {
                case "local":
                    return _serviceProvider.GetRequiredService<LocalFileSystemConnector>();
                
                case "sftp":
                    return _serviceProvider.GetRequiredService<SftpConnector>();
                
                default:
                    throw new NotSupportedException($"File system provider {provider} is not supported");
            }
        }
        
        private IDataConnector CreateStreamingConnector(DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("provider", out var provider))
            {
                throw new ArgumentException("Streaming provider is required for streaming connection");
            }
            
            switch (provider.ToLowerInvariant())
            {
                case "kafka":
                    return _serviceProvider.GetRequiredService<KafkaConnector>();
                
                case "eventhubs":
                    return _serviceProvider.GetRequiredService<EventHubsConnector>();
                
                default:
                    throw new NotSupportedException($"Streaming provider {provider} is not supported");
            }
        }
    }
}
