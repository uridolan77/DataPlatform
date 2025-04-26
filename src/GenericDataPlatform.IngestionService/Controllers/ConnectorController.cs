using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.IngestionService.Connectors;
using GenericDataPlatform.IngestionService.Connectors.Database;
using GenericDataPlatform.IngestionService.Connectors.FileSystem;
using GenericDataPlatform.IngestionService.Connectors.Rest;
using GenericDataPlatform.IngestionService.Connectors.Streaming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Controllers
{
    [ApiController]
    [Route("api/connectors")]
    public class ConnectorController : ControllerBase
    {
        private readonly ConnectorFactory _connectorFactory;
        private readonly ILogger<ConnectorController> _logger;
        private readonly SqlServerConnector _sqlServerConnector;
        private readonly MySqlConnector _mySqlConnector;
        private readonly PostgreSqlConnector _postgreSqlConnector;
        private readonly LocalFileSystemConnector _localFileSystemConnector;
        private readonly SftpConnector _sftpConnector;
        private readonly RestApiConnector _restApiConnector;
        private readonly KafkaConnector _kafkaConnector;
        private readonly EventHubsConnector _eventHubsConnector;
        
        public ConnectorController(
            ConnectorFactory connectorFactory,
            ILogger<ConnectorController> logger,
            SqlServerConnector sqlServerConnector,
            MySqlConnector mySqlConnector,
            PostgreSqlConnector postgreSqlConnector,
            LocalFileSystemConnector localFileSystemConnector,
            SftpConnector sftpConnector,
            RestApiConnector restApiConnector,
            KafkaConnector kafkaConnector,
            EventHubsConnector eventHubsConnector)
        {
            _connectorFactory = connectorFactory;
            _logger = logger;
            _sqlServerConnector = sqlServerConnector;
            _mySqlConnector = mySqlConnector;
            _postgreSqlConnector = postgreSqlConnector;
            _localFileSystemConnector = localFileSystemConnector;
            _sftpConnector = sftpConnector;
            _restApiConnector = restApiConnector;
            _kafkaConnector = kafkaConnector;
            _eventHubsConnector = eventHubsConnector;
        }
        
        [HttpGet("database")]
        public ActionResult<IEnumerable<ConnectorInfo>> GetDatabaseConnectors()
        {
            var connectors = new List<ConnectorInfo>
            {
                new ConnectorInfo
                {
                    Type = "SqlServer",
                    DisplayName = "SQL Server",
                    Description = "Connect to Microsoft SQL Server databases",
                    Category = "Database",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "server", DisplayName = "Server", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "database", DisplayName = "Database", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "username", DisplayName = "Username", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "password", DisplayName = "Password", Type = "password", IsRequired = false },
                        new ConnectorProperty { Name = "integratedSecurity", DisplayName = "Integrated Security", Type = "boolean", IsRequired = false },
                        new ConnectorProperty { Name = "connectionString", DisplayName = "Connection String", Type = "string", IsRequired = false }
                    }
                },
                new ConnectorInfo
                {
                    Type = "MySQL",
                    DisplayName = "MySQL",
                    Description = "Connect to MySQL databases",
                    Category = "Database",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "server", DisplayName = "Server", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "port", DisplayName = "Port", Type = "integer", IsRequired = false },
                        new ConnectorProperty { Name = "database", DisplayName = "Database", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "username", DisplayName = "Username", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "password", DisplayName = "Password", Type = "password", IsRequired = true },
                        new ConnectorProperty { Name = "connectionString", DisplayName = "Connection String", Type = "string", IsRequired = false }
                    }
                },
                new ConnectorInfo
                {
                    Type = "PostgreSQL",
                    DisplayName = "PostgreSQL",
                    Description = "Connect to PostgreSQL databases",
                    Category = "Database",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "host", DisplayName = "Host", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "port", DisplayName = "Port", Type = "integer", IsRequired = false },
                        new ConnectorProperty { Name = "database", DisplayName = "Database", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "username", DisplayName = "Username", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "password", DisplayName = "Password", Type = "password", IsRequired = true },
                        new ConnectorProperty { Name = "connectionString", DisplayName = "Connection String", Type = "string", IsRequired = false }
                    }
                }
            };
            
            return Ok(connectors);
        }
        
        [HttpGet("file-system")]
        public ActionResult<IEnumerable<ConnectorInfo>> GetFileSystemConnectors()
        {
            var connectors = new List<ConnectorInfo>
            {
                new ConnectorInfo
                {
                    Type = "LocalFileSystem",
                    DisplayName = "Local File System",
                    Description = "Connect to local file system",
                    Category = "FileSystem",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "basePath", DisplayName = "Base Path", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "filePattern", DisplayName = "File Pattern", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "recursive", DisplayName = "Recursive", Type = "boolean", IsRequired = false }
                    }
                },
                new ConnectorInfo
                {
                    Type = "SFTP",
                    DisplayName = "SFTP",
                    Description = "Connect to SFTP servers",
                    Category = "FileSystem",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "host", DisplayName = "Host", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "port", DisplayName = "Port", Type = "integer", IsRequired = false },
                        new ConnectorProperty { Name = "username", DisplayName = "Username", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "password", DisplayName = "Password", Type = "password", IsRequired = false },
                        new ConnectorProperty { Name = "privateKeyPath", DisplayName = "Private Key Path", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "privateKeyPassphrase", DisplayName = "Private Key Passphrase", Type = "password", IsRequired = false },
                        new ConnectorProperty { Name = "basePath", DisplayName = "Base Path", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "filePattern", DisplayName = "File Pattern", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "recursive", DisplayName = "Recursive", Type = "boolean", IsRequired = false }
                    }
                }
            };
            
            return Ok(connectors);
        }
        
        [HttpGet("streaming")]
        public ActionResult<IEnumerable<ConnectorInfo>> GetStreamingConnectors()
        {
            var connectors = new List<ConnectorInfo>
            {
                new ConnectorInfo
                {
                    Type = "Kafka",
                    DisplayName = "Apache Kafka",
                    Description = "Connect to Apache Kafka clusters",
                    Category = "Streaming",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "bootstrapServers", DisplayName = "Bootstrap Servers", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "topic", DisplayName = "Topic", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "groupId", DisplayName = "Group ID", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "autoOffsetReset", DisplayName = "Auto Offset Reset", Type = "string", IsRequired = false }
                    }
                },
                new ConnectorInfo
                {
                    Type = "EventHubs",
                    DisplayName = "Azure Event Hubs",
                    Description = "Connect to Azure Event Hubs",
                    Category = "Streaming",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "connectionString", DisplayName = "Connection String", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "fullyQualifiedNamespace", DisplayName = "Fully Qualified Namespace", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "eventHubName", DisplayName = "Event Hub Name", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "consumerGroup", DisplayName = "Consumer Group", Type = "string", IsRequired = false }
                    }
                }
            };
            
            return Ok(connectors);
        }
        
        [HttpGet("rest")]
        public ActionResult<IEnumerable<ConnectorInfo>> GetRestConnectors()
        {
            var connectors = new List<ConnectorInfo>
            {
                new ConnectorInfo
                {
                    Type = "RestApi",
                    DisplayName = "REST API",
                    Description = "Connect to REST APIs",
                    Category = "Rest",
                    RequiredProperties = new List<ConnectorProperty>
                    {
                        new ConnectorProperty { Name = "baseUrl", DisplayName = "Base URL", Type = "string", IsRequired = true },
                        new ConnectorProperty { Name = "authType", DisplayName = "Authentication Type", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "username", DisplayName = "Username", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "password", DisplayName = "Password", Type = "password", IsRequired = false },
                        new ConnectorProperty { Name = "apiKey", DisplayName = "API Key", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "apiKeyHeader", DisplayName = "API Key Header", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "bearerToken", DisplayName = "Bearer Token", Type = "string", IsRequired = false },
                        new ConnectorProperty { Name = "timeoutSeconds", DisplayName = "Timeout (seconds)", Type = "integer", IsRequired = false }
                    }
                }
            };
            
            return Ok(connectors);
        }
        
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateConnection([FromBody] DataSourceDefinition source)
        {
            try
            {
                var connector = _connectorFactory.CreateConnector(source);
                var isValid = await connector.ValidateConnectionAsync(source);
                
                if (isValid)
                {
                    return Ok(new { Success = true, Message = "Connection is valid" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Connection is invalid" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating connection");
                return StatusCode(500, new { Success = false, Message = $"Error validating connection: {ex.Message}" });
            }
        }
        
        [HttpPost("infer-schema")]
        public async Task<ActionResult<DataSchema>> InferSchema([FromBody] DataSourceDefinition source)
        {
            try
            {
                var connector = _connectorFactory.CreateConnector(source);
                var schema = await connector.InferSchemaAsync(source);
                
                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inferring schema");
                return StatusCode(500, new { Success = false, Message = $"Error inferring schema: {ex.Message}" });
            }
        }
        
        [HttpPost("fetch-data")]
        public async Task<ActionResult<IEnumerable<DataRecord>>> FetchData([FromBody] FetchDataRequest request)
        {
            try
            {
                var connector = _connectorFactory.CreateConnector(request.Source);
                var records = await connector.FetchDataAsync(request.Source, request.Parameters);
                
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data");
                return StatusCode(500, new { Success = false, Message = $"Error fetching data: {ex.Message}" });
            }
        }
    }
    
    public class ConnectorInfo
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<ConnectorProperty> RequiredProperties { get; set; }
    }
    
    public class ConnectorProperty
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
    }
    
    public class FetchDataRequest
    {
        public DataSourceDefinition Source { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
