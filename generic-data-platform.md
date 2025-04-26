# Generic Data Platform - C# Implementation

This document outlines a comprehensive C# implementation for a generic data platform that can ingest, process, and serve any type of data. The platform uses a microservices architecture with gRPC for inter-service communication and is designed to be extensible, configurable, and adaptable to different data sources and processing requirements.

## Core Design Principles

1. **Source Agnostic**: Able to ingest data from any source through configurable connectors
2. **Schema Flexibility**: Supports both schema-on-write and schema-on-read approaches
3. **Extensible Pipeline**: Modular ETL components that can be arranged in different workflows
4. **Storage Tier Separation**: Clear separation between raw, processed, and specialized storage
5. **Pluggable Architecture**: Services can be added or removed based on requirements
6. **Self-Service API**: Comprehensive API for data access and management

## Solution Structure

```
GenericDataPlatform/
│
├── src/
│   ├── GenericDataPlatform.sln                # Solution file
│   │
│   ├── Protos/                                # Shared Protobuf files
│   │   ├── ingestion.proto                    # Data ingestion service definitions
│   │   ├── storage.proto                      # Raw storage service definitions
│   │   ├── database.proto                     # Database service definitions
│   │   ├── documents.proto                    # Document storage service definitions
│   │   ├── vector.proto                       # Vector DB service definitions
│   │   ├── features.proto                     # Feature store service definitions
│   │   ├── catalog.proto                      # Data catalog service definitions
│   │   ├── lineage.proto                      # Data lineage service definitions
│   │   ├── models.proto                       # Common data models
│   │   └── audit.proto                        # Audit and compliance service definitions
│   │
│   ├── GenericDataPlatform.Common/            # Shared library
│   │   ├── Models/                            # Shared data models
│   │   │   ├── DataSourceDefinition.cs        # Data source configuration model
│   │   │   ├── DataSchema.cs                  # Flexible schema definition model
│   │   │   ├── DataRecord.cs                  # Generic data record model
│   │   │   ├── Processing/                    # Processing related models
│   │   │   └── Storage/                       # Storage related models
│   │   ├── Utilities/                         # Utility classes
│   │   ├── Config/                            # Configuration management
│   │   ├── Extensions/                        # Extension methods
│   │   ├── Security/                          # Security-related utilities
│   │   └── Validation/                        # Data validation utilities
│   │
│   ├── GenericDataPlatform.IngestionService/  # Data ingestion microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Connectors/                        # Source-specific connectors
│   │   │   ├── Base/
│   │   │   │   ├── IDataConnector.cs          # Connector interface
│   │   │   │   └── BaseConnector.cs           # Base connector implementation
│   │   │   ├── Rest/                          # REST API connectors
│   │   │   ├── Database/                      # Database connectors
│   │   │   ├── FileSystem/                    # File system connectors
│   │   │   ├── Streaming/                     # Streaming connectors (Kafka, etc.)
│   │   │   ├── FTP/                           # FTP/SFTP connectors
│   │   │   └── Custom/                        # Custom connector implementations
│   │   ├── Validation/                        # Data validation components
│   │   ├── Configuration/                     # Connector configuration
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.StorageService/    # Raw storage microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Repositories/                      # Storage repositories
│   │   │   ├── IStorageRepository.cs          # Repository interface
│   │   │   ├── S3Repository.cs                # AWS S3 implementation
│   │   │   ├── AzureBlobRepository.cs         # Azure Blob Storage implementation
│   │   │   ├── GcpStorageRepository.cs        # Google Cloud Storage implementation
│   │   │   ├── MinioRepository.cs             # MinIO implementation
│   │   │   └── FileSystemRepository.cs        # Local file system implementation
│   │   ├── Partitioning/                      # Storage partitioning strategies
│   │   ├── Compression/                       # Data compression components
│   │   ├── Encryption/                        # Data encryption components
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.DatabaseService/   # Structured data microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Repositories/                      # Database repositories
│   │   │   ├── IDbRepository.cs               # Repository interface
│   │   │   ├── PostgresRepository.cs          # PostgreSQL implementation
│   │   │   ├── SqlServerRepository.cs         # SQL Server implementation
│   │   │   ├── MySqlRepository.cs             # MySQL implementation
│   │   │   └── TimescaleRepository.cs         # TimescaleDB implementation
│   │   ├── Entities/                          # Entity models
│   │   │   ├── BaseEntity.cs                  # Base entity with common properties
│   │   │   ├── DataRecord.cs                  # Generic data record entity
│   │   │   ├── DataSource.cs                  # Data source entity
│   │   │   ├── DataSchema.cs                  # Schema entity
│   │   │   └── Audit/                         # Audit-related entities
│   │   ├── Contexts/                          # DB contexts
│   │   ├── Migrations/                        # EF Core migrations
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.DocumentService/   # Document storage microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Repositories/                      # Document repositories
│   │   │   ├── IDocumentRepository.cs         # Repository interface
│   │   │   ├── ElasticsearchRepository.cs     # Elasticsearch implementation
│   │   │   ├── CosmosDbRepository.cs          # Azure Cosmos DB implementation
│   │   │   ├── MongoRepository.cs             # MongoDB implementation
│   │   │   └── SolrRepository.cs              # Solr implementation
│   │   ├── Indexing/                          # Document indexing components
│   │   ├── Search/                            # Search query builders
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.VectorService/     # Vector DB microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Repositories/                      # Vector repositories
│   │   │   ├── IVectorRepository.cs           # Repository interface
│   │   │   ├── PineconeRepository.cs          # Pinecone implementation
│   │   │   ├── WeaviateRepository.cs          # Weaviate implementation
│   │   │   ├── ChromaRepository.cs            # Chroma implementation
│   │   │   ├── PgVectorRepository.cs          # PostgreSQL with pgvector implementation
│   │   │   └── FaissRepository.cs             # FAISS implementation
│   │   ├── Embeddings/                        # Embedding generation components
│   │   ├── Vectors/                           # Vector operations
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.FeatureService/    # Feature store microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Repositories/                      # Feature repositories
│   │   │   ├── IFeatureRepository.cs          # Repository interface
│   │   │   ├── FeastRepository.cs             # Feast implementation
│   │   │   ├── HopsworksRepository.cs         # Hopsworks implementation
│   │   │   └── CustomRepository.cs            # Custom feature store implementation
│   │   ├── Features/                          # Feature definitions and registry
│   │   ├── Transformations/                   # Feature transformations
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.CatalogService/    # Data catalog microservice
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Repositories/                      # Catalog repositories
│   │   ├── Models/                            # Catalog models
│   │   ├── Search/                            # Catalog search
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.ETL/               # ETL processing service
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Processors/                        # Data processing pipeline
│   │   │   ├── IPipelineProcessor.cs          # Processor interface
│   │   │   ├── BasePipeline.cs                # Base pipeline implementation
│   │   │   └── PipelineFactory.cs             # Pipeline factory
│   │   ├── Extractors/                        # Data extraction components
│   │   │   ├── Base/
│   │   │   │   ├── IExtractor.cs              # Extractor interface
│   │   │   │   └── BaseExtractor.cs           # Base extractor implementation
│   │   │   ├── Rest/                          # REST API extractors
│   │   │   ├── Database/                      # Database extractors
│   │   │   ├── File/                          # File extractors
│   │   │   └── Custom/                        # Custom extractors
│   │   ├── Transformers/                      # Data transformation components
│   │   │   ├── Base/
│   │   │   │   ├── ITransformer.cs            # Transformer interface
│   │   │   │   └── BaseTransformer.cs         # Base transformer implementation
│   │   │   ├── Text/                          # Text transformers
│   │   │   ├── Json/                          # JSON transformers
│   │   │   ├── Csv/                           # CSV transformers
│   │   │   ├── Xml/                           # XML transformers
│   │   │   └── Custom/                        # Custom transformers
│   │   ├── Loaders/                           # Data loading components
│   │   │   ├── Base/
│   │   │   │   ├── ILoader.cs                 # Loader interface
│   │   │   │   └── BaseLoader.cs              # Base loader implementation
│   │   │   ├── Storage/                       # Storage loaders
│   │   │   ├── Database/                      # Database loaders
│   │   │   ├── Document/                      # Document loaders
│   │   │   ├── Vector/                        # Vector loaders
│   │   │   └── Custom/                        # Custom loaders
│   │   ├── Orchestration/                     # Pipeline orchestration
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.ML/                # ML components service
│   │   ├── Services/                          # gRPC service implementations
│   │   ├── Models/                            # ML model definitions
│   │   ├── Trainers/                          # Model training components
│   │   ├── Inference/                         # Model inference components
│   │   ├── Embeddings/                        # Text embedding components
│   │   │   ├── IEmbeddingGenerator.cs         # Embedding generator interface
│   │   │   ├── BaseEmbeddingGenerator.cs      # Base embedding generator
│   │   │   ├── HuggingFaceGenerator.cs        # HuggingFace embedding implementation
│   │   │   ├── OpenAIGenerator.cs             # OpenAI embedding implementation
│   │   │   └── CustomGenerator.cs             # Custom embedding implementation
│   │   ├── FeatureEngineering/                # Feature engineering pipelines
│   │   ├── Evaluation/                        # Model evaluation components
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   ├── GenericDataPlatform.API/               # API layer
│   │   ├── Controllers/                       # REST API controllers
│   │   │   ├── IngestionController.cs         # Data ingestion API
│   │   │   ├── StorageController.cs           # Storage API
│   │   │   ├── DataController.cs              # Data access API
│   │   │   ├── SchemaController.cs            # Schema management API
│   │   │   ├── CatalogController.cs           # Data catalog API
│   │   │   ├── PipelineController.cs          # ETL pipeline API
│   │   │   ├── MLController.cs                # ML API
│   │   │   └── AdminController.cs             # Administration API
│   │   ├── Filters/                           # Request filters
│   │   ├── Models/                            # API models
│   │   ├── Services/                          # API services
│   │   ├── GraphQL/                           # GraphQL API implementation
│   │   ├── Startup.cs                         # Service configuration
│   │   └── Program.cs                         # Entry point
│   │
│   └── GenericDataPlatform.Compliance/        # Compliance service
│       ├── Services/                          # gRPC service implementations
│       ├── Encryption/                        # Encryption utilities
│       ├── AccessControl/                     # Access control implementation
│       ├── Auditing/                          # Audit logging functionality
│       ├── Privacy/                           # Privacy controls and PII handling
│       ├── Startup.cs                         # Service configuration
│       └── Program.cs                         # Entry point
│
├── tests/                                     # Test projects
│   ├── GenericDataPlatform.Common.Tests/      # Common library tests
│   ├── GenericDataPlatform.Ingestion.Tests/   # Ingestion service tests
│   ├── GenericDataPlatform.Storage.Tests/     # Storage service tests
│   └── ...                                    # Other test projects
│
├── tools/                                     # Utility tools and scripts
│   ├── setup/                                 # Setup scripts
│   ├── migration/                             # Migration tools
│   └── monitoring/                            # Monitoring tools
│
├── deploy/                                    # Deployment configurations
│   ├── docker/                                # Docker compose files
│   ├── kubernetes/                            # Kubernetes manifests
│   └── terraform/                             # Infrastructure as code
│
└── docs/                                      # Documentation
    ├── architecture/                          # Architecture documentation
    ├── api/                                   # API documentation
    └── development/                           # Development guides
```

## Key Components Implementation

### 1. Data Source Configuration

The platform uses a flexible data source definition model to configure connections to any type of data source:

```csharp
// GenericDataPlatform.Common/Models/DataSourceDefinition.cs
public class DataSourceDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DataSourceType Type { get; set; }
    public Dictionary<string, string> ConnectionProperties { get; set; }
    public DataSchema Schema { get; set; }
    public DataIngestMode IngestMode { get; set; }
    public DataRefreshPolicy RefreshPolicy { get; set; }
    public Dictionary<string, string> ValidationRules { get; set; }
    public Dictionary<string, string> MetadataProperties { get; set; }
}

public enum DataSourceType
{
    RestApi,
    Database,
    FileSystem,
    Streaming,
    Ftp,
    Custom
}

public enum DataIngestMode
{
    FullLoad,
    Incremental,
    ChangeDataCapture
}

public enum DataRefreshPolicy
{
    Manual,
    Scheduled,
    EventDriven
}
```

### 2. Dynamic Schema Support

The platform supports dynamic schemas to handle any data structure:

```csharp
// GenericDataPlatform.Common/Models/DataSchema.cs
public class DataSchema
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public SchemaType Type { get; set; }
    public List<SchemaField> Fields { get; set; }
    public SchemaVersion Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum SchemaType
{
    Strict,      // Enforce schema validation
    Flexible,    // Allow additional fields
    Dynamic      // Infer schema from data
}

public class SchemaField
{
    public string Name { get; set; }
    public string Description { get; set; }
    public FieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsArray { get; set; }
    public string DefaultValue { get; set; }
    public ValidationRules Validation { get; set; }
    public List<SchemaField> NestedFields { get; set; } // For complex types
}

public enum FieldType
{
    String,
    Integer,
    Decimal,
    Boolean,
    DateTime,
    Json,
    Complex,
    Binary
}

public class ValidationRules
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string Pattern { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string[] AllowedValues { get; set; }
    public string CustomValidation { get; set; } // Expression or reference to validation function
}

public class SchemaVersion
{
    public string VersionNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string PreviousVersion { get; set; }
    public string ChangeDescription { get; set; }
}
```

### 3. Generic Data Record

A flexible data record model that can hold any type of data:

```csharp
// GenericDataPlatform.Common/Models/DataRecord.cs
public class DataRecord
{
    public string Id { get; set; }
    public string SchemaId { get; set; }
    public string SourceId { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Version { get; set; }
    
    // Helper methods for accessing typed data
    public T GetValue<T>(string key, T defaultValue = default)
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
    
    public bool TryGetValue<T>(string key, out T value)
    {
        value = default;
        if (Data.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }
        return false;
    }
}
```

### 4. Data Connector Interface

A flexible interface for connecting to any data source:

```csharp
// GenericDataPlatform.IngestionService/Connectors/Base/IDataConnector.cs
public interface IDataConnector
{
    Task<bool> ValidateConnectionAsync();
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
```

### 5. Storage Repository Interface

A generic interface for raw data storage:

```csharp
// GenericDataPlatform.StorageService/Repositories/IStorageRepository.cs
public interface IStorageRepository
{
    Task<string> StoreAsync(Stream dataStream, StorageMetadata metadata);
    Task<Stream> RetrieveAsync(string path);
    Task<StorageMetadata> GetMetadataAsync(string path);
    Task<IEnumerable<StorageItem>> ListAsync(string prefix, bool recursive = false);
    Task<bool> DeleteAsync(string path);
    Task<string> CopyAsync(string sourcePath, string destinationPath);
    Task<StorageStatistics> GetStatisticsAsync(string prefix = null);
}

public class StorageMetadata
{
    public string Id { get; set; }
    public string SourceId { get; set; }
    public string ContentType { get; set; }
    public string Filename { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Checksum { get; set; }
    public string StorageTier { get; set; }
    public bool IsCompressed { get; set; }
    public bool IsEncrypted { get; set; }
    public Dictionary<string, string> CustomMetadata { get; set; }
}

public class StorageItem
{
    public string Path { get; set; }
    public bool IsDirectory { get; set; }
    public StorageMetadata Metadata { get; set; }
}

public class StorageStatistics
{
    public long TotalItems { get; set; }
    public long TotalSize { get; set; }
    public Dictionary<string, long> ItemsByType { get; set; }
    public Dictionary<string, long> SizeByType { get; set; }
}
```

### 6. ETL Pipeline Components

Flexible ETL components that can be assembled into customized pipelines:

```csharp
// GenericDataPlatform.ETL/Processors/IPipelineProcessor.cs
public interface IPipelineProcessor
{
    Task<PipelineResult> ProcessAsync(PipelineContext context);
    Task<PipelineStatus> GetStatusAsync(string pipelineId);
    Task<bool> CancelAsync(string pipelineId);
}

public class PipelineContext
{
    public string PipelineId { get; set; }
    public DataSourceDefinition Source { get; set; }
    public List<PipelineStage> Stages { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public class PipelineStage
{
    public string Id { get; set; }
    public string Name { get; set; }
    public StageType Type { get; set; }
    public Dictionary<string, object> Configuration { get; set; }
    public List<string> DependsOn { get; set; }
}

public enum StageType
{
    Extract,
    Transform,
    Load,
    Validate,
    Enrich,
    Custom
}

public class PipelineResult
{
    public string PipelineId { get; set; }
    public PipelineExecutionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long RecordsProcessed { get; set; }
    public List<StageResult> StageResults { get; set; }
    public List<string> Errors { get; set; }
    public Dictionary<string, object> OutputParameters { get; set; }
}

public class StageResult
{
    public string StageId { get; set; }
    public StageExecutionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long RecordsProcessed { get; set; }
    public List<string> Errors { get; set; }
}

public enum PipelineExecutionStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum StageExecutionStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Skipped
}
```

### 7. Generic Data API

The API layer that provides a unified interface for data access:

```csharp
// GenericDataPlatform.API/Controllers/DataController.cs
[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;
    
    public DataController(IDataService dataService)
    {
        _dataService = dataService;
    }
    
    [HttpGet("sources")]
    public async Task<ActionResult<IEnumerable<DataSourceDefinition>>> GetSources()
    {
        var sources = await _dataService.GetDataSourcesAsync();
        return Ok(sources);
    }
    
    [HttpGet("sources/{id}")]
    public async Task<ActionResult<DataSourceDefinition>> GetSource(string id)
    {
        var source = await _dataService.GetDataSourceAsync(id);
        if (source == null)
            return NotFound();
        return Ok(source);
    }
    
    [HttpPost("sources")]
    public async Task<ActionResult<DataSourceDefinition>> CreateSource(DataSourceDefinition source)
    {
        var result = await _dataService.CreateDataSourceAsync(source);
        return CreatedAtAction(nameof(GetSource), new { id = result.Id }, result);
    }
    
    [HttpGet("sources/{sourceId}/schema")]
    public async Task<ActionResult<DataSchema>> GetSchema(string sourceId)
    {
        var schema = await _dataService.GetSchemaAsync(sourceId);
        if (schema == null)
            return NotFound();
        return Ok(schema);
    }
    
    [HttpGet("records")]
    public async Task<ActionResult<PagedResult<DataRecord>>> GetRecords(
        [FromQuery] string sourceId, 
        [FromQuery] Dictionary<string, string> filters,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var result = await _dataService.GetRecordsAsync(sourceId, filters, page, pageSize);
        return Ok(result);
    }
    
    [HttpGet("records/{id}")]
    public async Task<ActionResult<DataRecord>> GetRecord(string id)
    {
        var record = await _dataService.GetRecordAsync(id);
        if (record == null)
            return NotFound();
        return Ok(record);
    }
    
    [HttpPost("query")]
    public async Task<ActionResult<QueryResult>> Query(DataQuery query)
    {
        var result = await _dataService.QueryAsync(query);
        return Ok(result);
    }
}
```

## Protocol Buffer Definitions

Key protocol buffer definitions for service communication:

### Ingestion Service

```protobuf
// Protos/ingestion.proto
syntax = "proto3";

option csharp_namespace = "GenericDataPlatform.Protos";

package ingestion;

service IngestionService {
  rpc GetDataSources (GetDataSourcesRequest) returns (GetDataSourcesResponse);
  rpc GetDataSource (GetDataSourceRequest) returns (DataSourceDefinition);
  rpc CreateDataSource (CreateDataSourceRequest) returns (DataSourceDefinition);
  rpc UpdateDataSource (UpdateDataSourceRequest) returns (DataSourceDefinition);
  rpc DeleteDataSource (DeleteDataSourceRequest) returns (DeleteDataSourceResponse);
  
  rpc StartIngestion (StartIngestionRequest) returns (StartIngestionResponse);
  rpc GetIngestionStatus (GetIngestionStatusRequest) returns (GetIngestionStatusResponse);
  rpc CancelIngestion (CancelIngestionRequest) returns (CancelIngestionResponse);
  
  rpc ValidateConnection (ValidateConnectionRequest) returns (ValidateConnectionResponse);
  rpc InferSchema (InferSchemaRequest) returns (InferSchemaResponse);
}

message GetDataSourcesRequest {
  optional string type_filter = 1;
  optional int32 page = 2;
  optional int32 page_size = 3;
}

message GetDataSourcesResponse {
  repeated DataSourceDefinition sources = 1;
  int32 total_count = 2;
}

message GetDataSourceRequest {
  string source_id = 1;
}

message CreateDataSourceRequest {
  DataSourceDefinition source = 1;
}

message UpdateDataSourceRequest {
  DataSourceDefinition source = 1;
}

message DeleteDataSourceRequest {
  string source_id = 1;
}

message DeleteDataSourceResponse {
  bool success = 1;
  string message = 2;
}

message StartIngestionRequest {
  string source_id = 1;
  map<string, string> parameters = 2;
  bool full_refresh = 3;
}

message StartIngestionResponse {
  string job_id = 1;
  string status = 2;
}

message GetIngestionStatusRequest {
  string job_id = 1;
}

message GetIngestionStatusResponse {
  string job_id = 1;
  string status = 2;
  int64 records_processed = 3;
  int64 errors = 4;
  string start_time = 5;
  string end_time = 6;
  repeated string error_messages = 7;
}

message CancelIngestionRequest {
  string job_id = 1;
}

message CancelIngestionResponse {
  bool success = 1;
  string message = 2;
}

message ValidateConnectionRequest {
  DataSourceDefinition source = 1;
}

message ValidateConnectionResponse {
  bool is_valid = 1;
  string message = 2;
}

message InferSchemaRequest {
  DataSourceDefinition source = 1;
  int32 sample_size = 2;
}

message InferSchemaResponse {
  DataSchema schema = 1;
}

message DataSourceDefinition {
  string id = 1;
  string name = 2;
  string description = 3;
  string type = 4;
  map<string, string> connection_properties = 5;
  DataSchema schema = 6;
  string ingest_mode = 7;
  string refresh_policy = 8;
  map<string, string> validation_rules = 9;
  map<string, string> metadata_properties = 10;
}

message DataSchema {
  string id = 1;
  string name = 2;
  string description = 3;
  string type = 4;
  repeated SchemaField fields = 5;
  SchemaVersion version = 6;
  string created_at = 7;
  string updated_at = 8;
}

message SchemaField {
  string name = 1;
  string description = 2;
  string type = 3;
  bool is_required = 4;
  bool is_array = 5;
  string default_value = 6;
  ValidationRules validation = 7;
  repeated SchemaField nested_fields = 8;
}

message ValidationRules {
  optional int32 min_length = 1;
  optional int32 max_length = 2;
  optional string pattern = 3;
  optional double min_value = 4;
  optional double max_value = 5;
  repeated string allowed_values = 6;
  optional string custom_validation = 7;
}

message SchemaVersion {
  string version_number = 1;
  string effective_date = 2;
  string previous_version = 3;
  string change_description = 4;
}
```

### Storage Service

```protobuf
// Protos/storage.proto
syntax = "proto3";

option csharp_namespace = "GenericDataPlatform.Protos";

package storage;

service StorageService {
  rpc UploadFile (stream UploadFileRequest) returns (UploadFileResponse);
  rpc DownloadFile (DownloadFileRequest) returns (stream DownloadFileResponse);
  rpc GetMetadata (GetMetadataRequest) returns (StorageMetadata);
  rpc ListFiles (ListFilesRequest) returns (ListFilesResponse);
  rpc DeleteFile (DeleteFileRequest) returns (DeleteFileResponse);
  rpc CopyFile (CopyFileRequest) returns (CopyFileResponse);
  rpc GetStatistics (GetStatisticsRequest) returns (StorageStatistics);
}

message UploadFileRequest {
  oneof request {
    FileMetadata metadata = 1;
    bytes chunk_data = 2;
  }
}

message FileMetadata {
  string source_id = 1;
  string filename = 2;
  string content_type = 3;
  int64 total_size = 4;
  map<string, string> custom_metadata = 5;
  bool compress = 6;
  bool encrypt = 7;
  string storage_tier = 8;
}

message UploadFileResponse {
  string file_id = 1;
  string path = 2;
  string checksum = 3;
}

message DownloadFileRequest {
  string path = 1;
  int64 start_position = 2;
  int64 chunk_size = 3;
}

message DownloadFileResponse {
  oneof response {
    StorageMetadata metadata = 1;
    bytes chunk_data = 2;
  }
  bool is_last_chunk = 3;
}

message GetMetadataRequest {
  string path = 1;
}

message ListFilesRequest {
  string prefix = 1;
  bool recursive = 2;
  int32 max_results = 3;
  string continuation_token = 4;
}

message ListFilesResponse {
  repeated StorageItem items = 1;
  string continuation_token = 2;
}

message StorageItem {
  string path = 1;
  bool is_directory = 2;
  StorageMetadata metadata = 3;
}

message StorageMetadata {
  string id = 1;
  string source_id = 2;
  string content_type = 3;
  string filename = 4;
  int64 size = 5;
  string created_at = 6;
  string checksum = 7;
  string storage_tier = 8;
  bool is_compressed = 9;
  bool is_encrypted = 10;
  map<string, string> custom_metadata = 11;
}

message DeleteFileRequest {
  string path = 1;
}

message DeleteFileResponse {
  bool success = 1;
  string message = 2;
}

message CopyFileRequest {
  string source_path = 1;
  string destination_path = 2;
}

message CopyFileResponse {
  string destination_path = 1;
}

message GetStatisticsRequest {
  string prefix = 1;
}

message StorageStatistics {
  int64 total_items = 1;
  int64 total_size = 2;
  map<string, int64> items_by_type = 3;
  map<string, int64> size_by_type = 4;
}
```

## Implementation of Specific Components

### 1. Generic REST API Connector

```csharp
// GenericDataPlatform.IngestionService/Connectors/Rest/RestApiConnector.cs
public class RestApiConnector : BaseConnector, IDataConnector
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RestApiConnector> _logger;
    
    public RestApiConnector(
        IHttpClientFactory httpClientFactory,
        ILogger<RestApiConnector> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task<bool> ValidateConnectionAsync(DataSourceDefinition source)
    {
        try
        {
            // Extract connection properties
            if (!source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl))
                throw new ArgumentException("Base URL is required for REST API connection");
                
            // Optional properties
            source.ConnectionProperties.TryGetValue("authType", out var authType);
            
            // Create HTTP client
            var client = _httpClientFactory.CreateClient();
            
            // Apply authentication if needed
            ApplyAuthentication(client, source);
            
            // Make a test request to the base URL or health endpoint
            var testUrl = baseUrl;
            if (source.ConnectionProperties.TryGetValue("healthEndpoint", out var healthEndpoint))
                testUrl = CombineUrls(baseUrl, healthEndpoint);
                
            var response = await client.GetAsync(testUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating connection to REST API {source}", source.Name);
            return false;
        }
    }
    
    public async Task<IEnumerable<DataRecord>> FetchDataAsync(
        DataSourceDefinition source, 
        Dictionary<string, object> parameters = null)
    {
        try
        {
            // Extract connection properties
            if (!source.ConnectionProperties.TryGetValue("baseUrl", out var baseUrl))
                throw new ArgumentException("Base URL is required for REST API connection");
                
            if (!source.ConnectionProperties.TryGetValue("endpoint", out var endpoint))
                throw new ArgumentException("Endpoint is required for REST API connection");
            
            // Create HTTP client
            var client = _httpClientFactory.CreateClient();
            
            // Apply authentication if needed
            ApplyAuthentication(client, source);
            
            // Build the request URL with parameters
            var requestUrl = BuildRequestUrl(baseUrl, endpoint, parameters);
            
            // Make the request
            var response = await client.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            
            // Convert to data records based on response format
            if (source.ConnectionProperties.TryGetValue("responseFormat", out var format))
            {
                switch (format.ToLowerInvariant())
                {
                    case "json":
                        return ParseJsonResponse(content, source.Schema);
                    case "xml":
                        return ParseXmlResponse(content, source.Schema);
                    case "csv":
                        return ParseCsvResponse(content, source.Schema);
                    default:
                        throw new NotSupportedException($"Response format {format} is not supported");
                }
            }
            
            // Default to JSON
            return ParseJsonResponse(content, source.Schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from REST API {source}", source.Name);
            throw;
        }
    }
    
    public async Task<IAsyncEnumerable<DataRecord>> StreamDataAsync(
        DataSourceDefinition source, 
        Dictionary<string, object> parameters = null)
    {
        // Implementation for streaming data from paginated APIs
        // or other streaming sources
        throw new NotImplementedException();
    }
    
    public async Task<DataSchema> InferSchemaAsync(DataSourceDefinition source)
    {
        // Fetch a sample of data and infer the schema from it
        var sampleData = await FetchDataAsync(source, new Dictionary<string, object> 
        { 
            ["limit"] = 10 
        });
        
        return InferSchemaFromSample(sampleData);
    }
    
    private void ApplyAuthentication(HttpClient client, DataSourceDefinition source)
    {
        if (!source.ConnectionProperties.TryGetValue("authType", out var authType))
            return;
            
        switch (authType.ToLowerInvariant())
        {
            case "basic":
                if (source.ConnectionProperties.TryGetValue("username", out var username) &&
                    source.ConnectionProperties.TryGetValue("password", out var password))
                {
                    var credentials = Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{username}:{password}"));
                    client.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Basic", credentials);
                }
                break;
                
            case "bearer":
                if (source.ConnectionProperties.TryGetValue("token", out var token))
                {
                    client.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", token);
                }
                break;
                
            case "apikey":
                if (source.ConnectionProperties.TryGetValue("apiKey", out var apiKey) &&
                    source.ConnectionProperties.TryGetValue("apiKeyHeader", out var apiKeyHeader))
                {
                    client.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
                }
                break;
        }
    }
    
    private string BuildRequestUrl(string baseUrl, string endpoint, Dictionary<string, object> parameters)
    {
        var url = CombineUrls(baseUrl, endpoint);
        
        if (parameters == null || parameters.Count == 0)
            return url;
            
        var queryParams = parameters
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value?.ToString())}")
            .ToList();
            
        return url + (url.Contains("?") ? "&" : "?") + string.Join("&", queryParams);
    }
    
    private string CombineUrls(string baseUrl, string relativePath)
    {
        baseUrl = baseUrl.TrimEnd('/');
        relativePath = relativePath.TrimStart('/');
        return $"{baseUrl}/{relativePath}";
    }
    
    private IEnumerable<DataRecord> ParseJsonResponse(string content, DataSchema schema)
    {
        // Implementation for JSON parsing to DataRecord objects
        throw new NotImplementedException();
    }
    
    private IEnumerable<DataRecord> ParseXmlResponse(string content, DataSchema schema)
    {
        // Implementation for XML parsing to DataRecord objects
        throw new NotImplementedException();
    }
    
    private IEnumerable<DataRecord> ParseCsvResponse(string content, DataSchema schema)
    {
        // Implementation for CSV parsing to DataRecord objects
        throw new NotImplementedException();
    }
    
    private DataSchema InferSchemaFromSample(IEnumerable<DataRecord> sampleData)
    {
        // Implementation for schema inference from sample data
        throw new NotImplementedException();
    }
}
```

### 2. S3 Storage Repository

```csharp
// GenericDataPlatform.StorageService/Repositories/S3Repository.cs
public class S3Repository : IStorageRepository
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<S3Options> _options;
    private readonly ILogger<S3Repository> _logger;
    
    public S3Repository(
        IAmazonS3 s3Client,
        IOptions<S3Options> options,
        ILogger<S3Repository> logger)
    {
        _s3Client = s3Client;
        _options = options;
        _logger = logger;
    }
    
    public async Task<string> StoreAsync(Stream dataStream, StorageMetadata metadata)
    {
        try
        {
            // Generate a path based on metadata
            var path = GeneratePath(metadata);
            
            // Prepare upload request
            var putRequest = new PutObjectRequest
            {
                BucketName = _options.Value.BucketName,
                Key = path,
                InputStream = dataStream,
                ContentType = metadata.ContentType
            };
            
            // Add custom metadata
            if (metadata.CustomMetadata != null)
            {
                foreach (var item in metadata.CustomMetadata)
                {
                    putRequest.Metadata.Add($"x-amz-meta-{item.Key}", item.Value);
                }
            }
            
            // Set storage class if specified
            if (!string.IsNullOrEmpty(metadata.StorageTier))
            {
                putRequest.StorageClass = MapStorageTierToS3Class(metadata.StorageTier);
            }
            
            // Upload to S3
            await _s3Client.PutObjectAsync(putRequest);
            
            // Return the generated path
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing file in S3 {filename}", metadata.Filename);
            throw;
        }
    }
    
    public async Task<Stream> RetrieveAsync(string path)
    {
        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _options.Value.BucketName,
                Key = path
            };
            
            var response = await _s3Client.GetObjectAsync(getRequest);
            
            // Create a memory stream to detach from the response
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file from S3 {path}", path);
            throw;
        }
    }
    
    public async Task<StorageMetadata> GetMetadataAsync(string path)
    {
        try
        {
            var metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = _options.Value.BucketName,
                Key = path
            };
            
            var response = await _s3Client.GetObjectMetadataAsync(metadataRequest);
            
            // Extract standard metadata
            var metadata = new StorageMetadata
            {
                Id = path,
                ContentType = response.Headers.ContentType,
                Size = response.Headers.ContentLength,
                Checksum = response.ETag,
                CreatedAt = response.LastModified,
                StorageTier = MapS3ClassToStorageTier(response.StorageClass),
                IsCompressed = response.Headers.ContentEncoding?.Contains("gzip") ?? false,
                CustomMetadata = new Dictionary<string, string>()
            };
            
            // Extract custom metadata
            foreach (var key in response.Metadata.Keys)
            {
                if (key.StartsWith("x-amz-meta-"))
                {
                    var metadataKey = key.Substring("x-amz-meta-".Length);
                    metadata.CustomMetadata[metadataKey] = response.Metadata[key];
                }
            }
            
            // Extract filename from the path
            metadata.Filename = Path.GetFileName(path);
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata from S3 {path}", path);
            throw;
        }
    }
    
    public async Task<IEnumerable<StorageItem>> ListAsync(string prefix, bool recursive = false)
    {
        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _options.Value.BucketName,
                Prefix = prefix,
                Delimiter = recursive ? null : "/"
            };
            
            var items = new List<StorageItem>();
            ListObjectsV2Response response;
            
            do
            {
                response = await _s3Client.ListObjectsV2Async(listRequest);
                
                // Add files
                foreach (var s3Object in response.S3Objects)
                {
                    items.Add(new StorageItem
                    {
                        Path = s3Object.Key,
                        IsDirectory = s3Object.Key.EndsWith("/"),
                        Metadata = new StorageMetadata
                        {
                            Id = s3Object.Key,
                            Filename = Path.GetFileName(s3Object.Key),
                            Size = s3Object.Size,
                            Checksum = s3Object.ETag,
                            CreatedAt = s3Object.LastModified,
                            StorageTier = MapS3ClassToStorageTier(s3Object.StorageClass)
                        }
                    });
                }
                
                // Add directories if not recursive
                if (!recursive)
                {
                    foreach (var commonPrefix in response.CommonPrefixes)
                    {
                        items.Add(new StorageItem
                        {
                            Path = commonPrefix,
                            IsDirectory = true
                        });
                    }
                }
                
                listRequest.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);
            
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from S3 {prefix}", prefix);
            throw;
        }
    }
    
    public async Task<bool> DeleteAsync(string path)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.Value.BucketName,
                Key = path
            };
            
            await _s3Client.DeleteObjectAsync(deleteRequest);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3 {path}", path);
            return false;
        }
    }
    
    public async Task<string> CopyAsync(string sourcePath, string destinationPath)
    {
        try
        {
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = _options.Value.BucketName,
                SourceKey = sourcePath,
                DestinationBucket = _options.Value.BucketName,
                DestinationKey = destinationPath
            };
            
            await _s3Client.CopyObjectAsync(copyRequest);
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file in S3 from {sourcePath} to {destinationPath}", 
                sourcePath, destinationPath);
            throw;
        }
    }
    
    public async Task<StorageStatistics> GetStatisticsAsync(string prefix = null)
    {
        try
        {
            var items = await ListAsync(prefix ?? "", true);
            
            var statistics = new StorageStatistics
            {
                TotalItems = 0,
                TotalSize = 0,
                ItemsByType = new Dictionary<string, long>(),
                SizeByType = new Dictionary<string, long>()
            };
            
            foreach (var item in items)
            {
                if (item.IsDirectory)
                    continue;
                    
                statistics.TotalItems++;
                statistics.TotalSize += item.Metadata.Size;
                
                // Group by content type
                var contentType = item.Metadata.ContentType ?? "application/octet-stream";
                if (!statistics.ItemsByType.ContainsKey(contentType))
                {
                    statistics.ItemsByType[contentType] = 0;
                    statistics.SizeByType[contentType] = 0;
                }
                
                statistics.ItemsByType[contentType]++;
                statistics.SizeByType[contentType] += item.Metadata.Size;
            }
            
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage statistics for prefix {prefix}", prefix);
            throw;
        }
    }
    
    private string GeneratePath(StorageMetadata metadata)
    {
        // Generate a path based on source, date, and content type
        var datePart = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var sourceId = metadata.SourceId ?? "unknown";
        var fileName = metadata.Filename ?? Guid.NewGuid().ToString();
        
        // Extract content category from content type
        var contentCategory = "other";
        if (!string.IsNullOrEmpty(metadata.ContentType))
        {
            var parts = metadata.ContentType.Split('/');
            if (parts.Length > 0)
            {
                contentCategory = parts[0];
            }
        }
        
        return $"{sourceId}/{datePart}/{contentCategory}/{fileName}";
    }
    
    private S3StorageClass MapStorageTierToS3Class(string storageTier)
    {
        if (string.IsNullOrEmpty(storageTier))
            return S3StorageClass.Standard;
            
        switch (storageTier.ToLowerInvariant())
        {
            case "standard":
                return S3StorageClass.Standard;
            case "infrequent":
            case "infrequent_access":
                return S3StorageClass.StandardInfrequentAccess;
            case "archive":
                return S3StorageClass.Glacier;
            case "deep_archive":
                return S3StorageClass.DeepArchive;
            default:
                return S3StorageClass.Standard;
        }
    }
    
    private string MapS3ClassToStorageTier(S3StorageClass storageClass)
    {
        switch (storageClass)
        {
            case S3StorageClass.Standard:
                return "standard";
            case S3StorageClass.StandardInfrequentAccess:
                return "infrequent_access";
            case S3StorageClass.Glacier:
                return "archive";
            case S3StorageClass.DeepArchive:
                return "deep_archive";
            default:
                return "standard";
        }
    }
}

public class S3Options
{
    public string BucketName { get; set; }
    public string Region { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string ServiceUrl { get; set; }  // For MinIO or custom endpoints
    public bool ForcePathStyle { get; set; } // For MinIO compatibility
}
```

### 3. Dynamic ETL Pipeline

```csharp
// GenericDataPlatform.ETL/Processors/BasePipeline.cs
public class BasePipeline : IPipelineProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BasePipeline> _logger;
    private readonly Dictionary<string, PipelineExecution> _executions = new();
    
    public BasePipeline(
        IServiceProvider serviceProvider,
        ILogger<BasePipeline> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task<PipelineResult> ProcessAsync(PipelineContext context)
    {
        var pipelineId = context.PipelineId ?? Guid.NewGuid().ToString();
        var execution = new PipelineExecution
        {
            Id = pipelineId,
            Status = PipelineExecutionStatus.Running,
            StartTime = DateTime.UtcNow,
            Context = context,
            StageResults = new List<StageResult>(),
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken)
        };
        
        _executions[pipelineId] = execution;
        
        try
        {
            _logger.LogInformation("Starting pipeline {pipelineId} for source {sourceId}", 
                pipelineId, context.Source.Id);
            
            // Sort stages by dependencies
            var sortedStages = TopologicalSort(context.Stages);
            
            // Process each stage
            foreach (var stage in sortedStages)
            {
                // Check if pipeline was cancelled
                if (execution.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Pipeline {pipelineId} was cancelled", pipelineId);
                    execution.Status = PipelineExecutionStatus.Cancelled;
                    break;
                }
                
                var stageResult = new StageResult
                {
                    StageId = stage.Id,
                    Status = StageExecutionStatus.Running,
                    StartTime = DateTime.UtcNow
                };
                
                execution.StageResults.Add(stageResult);
                
                try
                {
                    _logger.LogInformation("Executing stage {stageId} of type {stageType}", 
                        stage.Id, stage.Type);
                    
                    // Get the appropriate processor for the stage
                    var processor = GetStageProcessor(stage.Type);
                    
                    // Create stage context
                    var stageContext = new StageContext
                    {
                        PipelineId = pipelineId,
                        StageId = stage.Id,
                        Source = context.Source,
                        Configuration = stage.Configuration,
                        Parameters = context.Parameters,
                        CancellationToken = execution.CancellationTokenSource.Token
                    };
                    
                    // If this stage depends on previous stages, add their results
                    if (stage.DependsOn != null && stage.DependsOn.Any())
                    {
                        foreach (var dependencyId in stage.DependsOn)
                        {
                            var dependencyResult = execution.StageResults.FirstOrDefault(r => r.StageId == dependencyId);
                            if (dependencyResult != null)
                            {
                                stageContext.DependencyResults[dependencyId] = dependencyResult;
                            }
                        }
                    }
                    
                    // Execute the stage
                    var result = await processor.ProcessAsync(stageContext);
                    
                    // Update stage result
                    stageResult.Status = StageExecutionStatus.Completed;
                    stageResult.EndTime = DateTime.UtcNow;
                    stageResult.RecordsProcessed = result.RecordsProcessed;
                    stageResult.Output = result.Output;
                    
                    _logger.LogInformation("Stage {stageId} completed successfully, processed {recordCount} records", 
                        stage.Id, result.RecordsProcessed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing stage {stageId}", stage.Id);
                    
                    stageResult.Status = StageExecutionStatus.Failed;
                    stageResult.EndTime = DateTime.UtcNow;
                    stageResult.Errors = new List<string> { ex.Message };
                    
                    // Determine if failure should stop the pipeline
                    var failBehavior = stage.Configuration.TryGetValue("failBehavior", out var behavior) 
                        ? behavior?.ToString() 
                        : "stop";
                        
                    if (failBehavior == "stop")
                    {
                        execution.Status = PipelineExecutionStatus.Failed;
                        execution.Errors = new List<string> { $"Stage {stage.Id} failed: {ex.Message}" };
                        break;
                    }
                }
            }
            
            // Finalize the pipeline if it wasn't failed or cancelled
            if (execution.Status == PipelineExecutionStatus.Running)
            {
                execution.Status = PipelineExecutionStatus.Completed;
            }
            
            execution.EndTime = DateTime.UtcNow;
            execution.RecordsProcessed = execution.StageResults.Sum(r => r.RecordsProcessed);
            
            return CreatePipelineResult(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pipeline {pipelineId}", pipelineId);
            
            execution.Status = PipelineExecutionStatus.Failed;
            execution.EndTime = DateTime.UtcNow;
            execution.Errors = new List<string> { ex.Message };
            
            return CreatePipelineResult(execution);
        }
    }
    
    public async Task<PipelineStatus> GetStatusAsync(string pipelineId)
    {
        if (_executions.TryGetValue(pipelineId, out var execution))
        {
            return new PipelineStatus
            {
                PipelineId = pipelineId,
                Status = execution.Status.ToString(),
                StartTime = execution.StartTime,
                EndTime = execution.EndTime,
                RecordsProcessed = execution.RecordsProcessed,
                StageStatuses = execution.StageResults.Select(s => new StageStatus
                {
                    StageId = s.StageId,
                    Status = s.Status.ToString(),
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    RecordsProcessed = s.RecordsProcessed
                }).ToList()
            };
        }
        
        return null;
    }
    
    public async Task<bool> CancelAsync(string pipelineId)
    {
        if (_executions.TryGetValue(pipelineId, out var execution))
        {
            execution.CancellationTokenSource.Cancel();
            return true;
        }
        
        return false;
    }
    
    private List<PipelineStage> TopologicalSort(List<PipelineStage> stages)
    {
        // Implementation of topological sort algorithm to order stages based on dependencies
        var result = new List<PipelineStage>();
        var visited = new HashSet<string>();
        var temp = new HashSet<string>();
        
        foreach (var stage in stages)
        {
            if (!visited.Contains(stage.Id))
            {
                TopologicalSortVisit(stages, stage, visited, temp, result);
            }
        }
        
        // Reverse the result since we want to start with nodes that have no dependencies
        result.Reverse();
        return result;
    }
    
    private void TopologicalSortVisit(
        List<PipelineStage> stages, 
        PipelineStage current, 
        HashSet<string> visited, 
        HashSet<string> temp, 
        List<PipelineStage> result)
    {
        if (temp.Contains(current.Id))
            throw new InvalidOperationException("Circular dependency detected in pipeline stages");
            
        if (visited.Contains(current.Id))
            return;
            
        temp.Add(current.Id);
        
        // Visit all dependencies
        if (current.DependsOn != null)
        {
            foreach (var dependencyId in current.DependsOn)
            {
                var dependency = stages.FirstOrDefault(s => s.Id == dependencyId);
                if (dependency == null)
                    throw new InvalidOperationException($"Stage {current.Id} depends on non-existent stage {dependencyId}");
                    
                TopologicalSortVisit(stages, dependency, visited, temp, result);
            }
        }
        
        temp.Remove(current.Id);
        visited.Add(current.Id);
        result.Add(current);
    }
    
    private IStageProcessor GetStageProcessor(StageType type)
    {
        switch (type)
        {
            case StageType.Extract:
                return _serviceProvider.GetRequiredService<IExtractorProcessor>();
            case StageType.Transform:
                return _serviceProvider.GetRequiredService<ITransformerProcessor>();
            case StageType.Load:
                return _serviceProvider.GetRequiredService<ILoaderProcessor>();
            case StageType.Validate:
                return _serviceProvider.GetRequiredService<IValidatorProcessor>();
            case StageType.Enrich:
                return _serviceProvider.GetRequiredService<IEnricherProcessor>();
            case StageType.Custom:
                var customType = context.Configuration.TryGetValue("processorType", out var type) 
                    ? type?.ToString() 
                    : null;
                if (string.IsNullOrEmpty(customType))
                    throw new ArgumentException("Custom processor type must be specified");
                return _serviceProvider.GetRequiredService<ICustomProcessorFactory>().Create(customType);
            default:
                throw new NotSupportedException($"Stage type {type} is not supported");
        }
    }
    
    private PipelineResult CreatePipelineResult(PipelineExecution execution)
    {
        return new PipelineResult
        {
            PipelineId = execution.Id,
            Status = execution.Status,
            StartTime = execution.StartTime,
            EndTime = execution.EndTime,
            RecordsProcessed = execution.RecordsProcessed,
            StageResults = execution.StageResults,
            Errors = execution.Errors
        };
    }
    
    private class PipelineExecution
    {
        public string Id { get; set; }
        public PipelineExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long RecordsProcessed { get; set; }
        public PipelineContext Context { get; set; }
        public List<StageResult> StageResults { get; set; }
        public List<string> Errors { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
```

### 4. PostgreSQL Database Repository

```csharp
// GenericDataPlatform.DatabaseService/Repositories/PostgresRepository.cs
public class PostgresRepository : IDbRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<PostgresRepository> _logger;
    
    public PostgresRepository(
        DbContext dbContext,
        ILogger<PostgresRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<IEnumerable<DataRecord>> GetRecordsAsync(
        string schemaId, 
        Dictionary<string, string> filters = null, 
        int page = 1, 
        int pageSize = 50)
    {
        try
        {
            // Get the entity type for the schema
            var entityType = GetEntityTypeForSchema(schemaId);
            
            // Create a query for the entity type
            var query = _dbContext.Set<DataRecordEntity>().AsQueryable();
            
            // Apply schema filter
            query = query.Where(e => e.SchemaId == schemaId);
            
            // Apply additional filters if provided
            if (filters != null && filters.Count > 0)
            {
                foreach (var filter in filters)
                {
                    // Handle special filter operators
                    if (filter.Key.Contains(":"))
                    {
                        var parts = filter.Key.Split(':');
                        var fieldName = parts[0];
                        var operation = parts[1];
                        
                        switch (operation.ToLowerInvariant())
                        {
                            case "eq":
                                query = query.Where(e => e.Data.RootElement.GetProperty(fieldName).ToString() == filter.Value);
                                break;
                            case "gt":
                                query = query.Where(e => e.Data.RootElement.GetProperty(fieldName).ValueKind == JsonValueKind.Number &&
                                           e.Data.RootElement.GetProperty(fieldName).GetDouble() > double.Parse(filter.Value));
                                break;
                            case "lt":
                                query = query.Where(e => e.Data.RootElement.GetProperty(fieldName).ValueKind == JsonValueKind.Number &&
                                           e.Data.RootElement.GetProperty(fieldName).GetDouble() < double.Parse(filter.Value));
                                break;
                            case "contains":
                                query = query.Where(e => e.Data.RootElement.GetProperty(fieldName).ValueKind == JsonValueKind.String &&
                                           e.Data.RootElement.GetProperty(fieldName).GetString().Contains(filter.Value));
                                break;
                            // Add more operators as needed
                            default:
                                _logger.LogWarning("Unsupported filter operation: {operation}", operation);
                                break;
                        }
                    }
                    else
                    {
                        // Default to equality
                        query = query.Where(e => e.Data.RootElement.GetProperty(filter.Key).ToString() == filter.Value);
                    }
                }
            }
            
            // Apply pagination
            var totalCount = await query.CountAsync();
            var pagedQuery = query
                .OrderByDescending(e => e.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
            
            // Execute query
            var entities = await pagedQuery.ToListAsync();
            
            // Map to data records
            var dataRecords = entities.Select(e => MapToDataRecord(e)).ToList();
            
            return dataRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving records for schema {schemaId}", schemaId);
            throw;
        }
    }
    
    public async Task<DataRecord> GetRecordAsync(string id)
    {
        try
        {
            var entity = await _dbContext.Set<DataRecordEntity>().FindAsync(id);
            
            if (entity == null)
                return null;
                
            return MapToDataRecord(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving record {id}", id);
            throw;
        }
    }
    
    public async Task<DataRecord> CreateRecordAsync(DataRecord record)
    {
        try
        {
            // Set ID if not provided
            if (string.IsNullOrEmpty(record.Id))
            {
                record.Id = Guid.NewGuid().ToString();
            }
            
            // Set timestamps
            var now = DateTime.UtcNow;
            record.CreatedAt = now;
            record.UpdatedAt = now;
            
            // Map to entity
            var entity = MapToEntity(record);
            
            // Add to context
            await _dbContext.Set<DataRecordEntity>().AddAsync(entity);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating record for schema {schemaId}", record.SchemaId);
            throw;
        }
    }
    
    public async Task<DataRecord> UpdateRecordAsync(DataRecord record)
    {
        try
        {
            // Find existing entity
            var entity = await _dbContext.Set<DataRecordEntity>().FindAsync(record.Id);
            
            if (entity == null)
                throw new KeyNotFoundException($"Record with ID {record.Id} not found");
                
            // Update timestamps
            record.CreatedAt = entity.CreatedAt;
            record.UpdatedAt = DateTime.UtcNow;
            
            // Update version if applicable
            if (!string.IsNullOrEmpty(record.Version))
            {
                var currentVersion = int.Parse(entity.Version);
                record.Version = (currentVersion + 1).ToString();
            }
            
            // Map to entity
            MapToEntity(record, entity);
            
            // Update entity
            _dbContext.Set<DataRecordEntity>().Update(entity);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating record {id}", record.Id);
            throw;
        }
    }
    
    public async Task<bool> DeleteRecordAsync(string id)
    {
        try
        {
            // Find entity
            var entity = await _dbContext.Set<DataRecordEntity>().FindAsync(id);
            
            if (entity == null)
                return false;
                
            // Remove entity
            _dbContext.Set<DataRecordEntity>().Remove(entity);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record {id}", id);
            throw;
        }
    }
    
    public async Task<IEnumerable<DataRecord>> BulkCreateAsync(IEnumerable<DataRecord> records)
    {
        try
        {
            // Set IDs and timestamps
            var now = DateTime.UtcNow;
            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Id))
                {
                    record.Id = Guid.NewGuid().ToString();
                }
                
                record.CreatedAt = now;
                record.UpdatedAt = now;
            }
            
            // Map to entities
            var entities = records.Select(r => MapToEntity(r)).ToList();
            
            // Add to context
            await _dbContext.Set<DataRecordEntity>().AddRangeAsync(entities);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            
            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating records");
            throw;
        }
    }
    
    public async Task<QueryResult> QueryAsync(DataQuery query)
    {
        try
        {
            // Implementation for complex queries
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query");
            throw;
        }
    }
    
    private Type GetEntityTypeForSchema(string schemaId)
    {
        // By default, use the generic DataRecordEntity
        return typeof(DataRecordEntity);
    }
    
    private DataRecord MapToDataRecord(DataRecordEntity entity)
    {
        var data = new Dictionary<string, object>();
        
        // Parse the JSON data
        if (entity.Data.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in entity.Data.RootElement.EnumerateObject())
            {
                data[property.Name] = JsonToObject(property.Value);
            }
        }
        
        return new DataRecord
        {
            Id = entity.Id,
            SchemaId = entity.SchemaId,
            SourceId = entity.SourceId,
            Data = data,
            Metadata = entity.Metadata != null 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata) 
                : new Dictionary<string, string>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Version = entity.Version
        };
    }
    
    private object JsonToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return intValue;
                if (element.TryGetInt64(out long longValue))
                    return longValue;
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    obj[property.Name] = JsonToObject(property.Value);
                }
                return obj;
            case JsonValueKind.Array:
                var array = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(JsonToObject(item));
                }
                return array;
            default:
                return null;
        }
    }
    
    private DataRecordEntity MapToEntity(DataRecord record, DataRecordEntity entity = null)
    {
        entity = entity ?? new DataRecordEntity();
        
        entity.Id = record.Id;
        entity.SchemaId = record.SchemaId;
        entity.SourceId = record.SourceId;
        entity.Data = JsonSerializer.SerializeToDocument(record.Data);
        entity.Metadata = record.Metadata != null 
            ? JsonSerializer.SerializeToElement(record.Metadata) 
            : null;
        entity.CreatedAt = record.CreatedAt;
        entity.UpdatedAt = record.UpdatedAt;
        entity.Version = record.Version;
        
        return entity;
    }
}

public class DataRecordEntity
{
    public string Id { get; set; }
    public string SchemaId { get; set; }
    public string SourceId { get; set; }
    public JsonDocument Data { get; set; }
    public JsonElement? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Version { get; set; }
}
```

### 5. Elasticsearch Document Repository

```csharp
// GenericDataPlatform.DocumentService/Repositories/ElasticsearchRepository.cs
public class ElasticsearchRepository : IDocumentRepository
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<ElasticsearchRepository> _logger;
    
    public ElasticsearchRepository(
        IElasticClient elasticClient,
        ILogger<ElasticsearchRepository> logger)
    {
        _elasticClient = elasticClient;
        _logger = logger;
    }
    
    public async Task<bool> IndexExistsAsync(string indexName)
    {
        var existsResponse = await _elasticClient.Indices.ExistsAsync(indexName);
        return existsResponse.Exists;
    }
    
    public async Task<bool> CreateIndexAsync(string indexName, DocumentIndexDefinition definition)
    {
        try
        {
            // Check if index already exists
            if (await IndexExistsAsync(indexName))
                return true;
                
            // Build index settings and mappings
            var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(definition.Shards ?? 1)
                    .NumberOfReplicas(definition.Replicas ?? 1)
                    .RefreshInterval(definition.RefreshInterval ?? "5s")
                    .Analysis(a => BuildAnalysis(a, definition))
                )
                .Map<DataDocument>(m => BuildMappings(m, definition))
            );
            
            return createIndexResponse.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Elasticsearch index {indexName}", indexName);
            throw;
        }
    }
    
    public async Task<string> IndexDocumentAsync(string indexName, DataDocument document)
    {
        try
        {
            // Set ID if not provided
            if (string.IsNullOrEmpty(document.Id))
            {
                document.Id = Guid.NewGuid().ToString();
            }
            
            // Set timestamps
            var now = DateTime.UtcNow;
            document.CreatedAt = document.CreatedAt == default ? now : document.CreatedAt;
            document.UpdatedAt = now;
            
            // Index the document
            var indexResponse = await _elasticClient.IndexAsync(document, i => i
                .Index(indexName)
                .Id(document.Id)
                .Refresh(Elasticsearch.Net.Refresh.True)
            );
            
            if (!indexResponse.IsValid)
            {
                _logger.LogError("Error indexing document: {error}", indexResponse.DebugInformation);
                throw new Exception($"Error indexing document: {indexResponse.DebugInformation}");
            }
            
            return document.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document in {indexName}", indexName);
            throw;
        }
    }
    
    public async Task<IEnumerable<string>> BulkIndexDocumentsAsync(string indexName, IEnumerable<DataDocument> documents)
    {
        try
        {
            // Set IDs and timestamps
            var now = DateTime.UtcNow;
            foreach (var document in documents)
            {
                if (string.IsNullOrEmpty(document.Id))
                {
                    document.Id = Guid.NewGuid().ToString();
                }
                
                document.CreatedAt = document.CreatedAt == default ? now : document.CreatedAt;
                document.UpdatedAt = now;
            }
            
            // Bulk index
            var bulkResponse = await _elasticClient.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(documents)
                .Refresh(Elasticsearch.Net.Refresh.True)
            );
            
            if (!bulkResponse.IsValid)
            {
                _logger.LogError("Error bulk indexing documents: {error}", bulkResponse.DebugInformation);
                throw new Exception($"Error bulk indexing documents: {bulkResponse.DebugInformation}");
            }
            
            return documents.Select(d => d.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing documents in {indexName}", indexName);
            throw;
        }
    }
    
    public async Task<DataDocument> GetDocumentAsync(string indexName, string id)
    {
        try
        {
            var getResponse = await _elasticClient.GetAsync<DataDocument>(id, g => g
                .Index(indexName)
            );
            
            if (!getResponse.IsValid)
            {
                if (getResponse.ApiCall?.HttpStatusCode == 404)
                    return null;
                    
                _logger.LogError("Error getting document: {error}", getResponse.DebugInformation);
                throw new Exception($"Error getting document: {getResponse.DebugInformation}");
            }
            
            return getResponse.Source;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {id} from {indexName}", id, indexName);
            throw;
        }
    }
    
    public async Task<bool> UpdateDocumentAsync(string indexName, DataDocument document)
    {
        try
        {
            // Set updated timestamp
            document.UpdatedAt = DateTime.UtcNow;
            
            // Update the document
            var updateResponse = await _elasticClient.UpdateAsync<DataDocument, object>(
                DocumentPath<DataDocument>.Id(document.Id),
                u => u.Index(indexName).Doc(document).Refresh(Elasticsearch.Net.Refresh.True)
            );
            
            if (!updateResponse.IsValid)
            {
                _logger.LogError("Error updating document: {error}", updateResponse.DebugInformation);
                throw new Exception($"Error updating document: {updateResponse.DebugInformation}");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {id} in {indexName}", document.Id, indexName);
            throw;
        }
    }
    
    public async Task<bool> DeleteDocumentAsync(string indexName, string id)
    {
        try
        {
            var deleteResponse = await _elasticClient.DeleteAsync<DataDocument>(id, d => d
                .Index(indexName)
                .Refresh(Elasticsearch.Net.Refresh.True)
            );
            
            if (!deleteResponse.IsValid && deleteResponse.ApiCall?.HttpStatusCode != 404)
            {
                _logger.LogError("Error deleting document: {error}", deleteResponse.DebugInformation);
                throw new Exception($"Error deleting document: {deleteResponse.DebugInformation}");
            }
            
            return deleteResponse.IsValid || deleteResponse.ApiCall?.HttpStatusCode == 404;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {id} from {indexName}", id, indexName);
            throw;
        }
    }
    
    public async Task<SearchResult<DataDocument>> SearchAsync(string indexName, SearchQuery query)
    {
        try
        {
            // Build search request
            var searchResponse = await _elasticClient.SearchAsync<DataDocument>(s =>
            {
                s.Index(indexName);
                
                // Add query
                if (query.Query != null)
                {
                    s.Query(q => BuildQuery(q, query.Query));
                }
                
                // Add filters
                if (query.Filters != null && query.Filters.Any())
                {
                    s.PostFilter(f => BuildFilter(f, query.Filters));
                }
                
                // Add sorting
                if (query.Sort != null && query.Sort.Any())
                {
                    s.Sort(sort =>
                    {
                        foreach (var sortField in query.Sort)
                        {
                            if (sortField.Descending)
                                sort = sort.Descending(sortField.Field);
                            else
                                sort = sort.Ascending(sortField.Field);
                        }
                        return sort;
                    });
                }
                
                // Add pagination
                s.From((query.Page - 1) * query.PageSize)
                 .Size(query.PageSize);
                
                // Add aggregations
                if (query.Aggregations != null && query.Aggregations.Any())
                {
                    s.Aggregations(a => BuildAggregations(a, query.Aggregations));
                }
                
                return s;
            });
            
            if (!searchResponse.IsValid)
            {
                _logger.LogError("Error searching documents: {error}", searchResponse.DebugInformation);
                throw new Exception($"Error searching documents: {searchResponse.DebugInformation}");
            }
            
            // Map search results
            var result = new SearchResult<DataDocument>
            {
                Items = searchResponse.Documents.ToList(),
                TotalCount = searchResponse.Total,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling(searchResponse.Total / (double)query.PageSize)
            };
            
            // Map aggregations if any
            if (searchResponse.Aggregations != null && searchResponse.Aggregations.Count > 0)
            {
                result.Aggregations = new Dictionary<string, object>();
                
                foreach (var agg in searchResponse.Aggregations)
                {
                    result.Aggregations[agg.Key] = ExtractAggregationValues(agg.Value);
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents in {indexName}", indexName);
            throw;
        }
    }
    
    private AnalysisDescriptor BuildAnalysis(AnalysisDescriptor analysis, DocumentIndexDefinition definition)
    {
        // Add custom analyzers if defined
        if (definition.Analyzers != null && definition.Analyzers.Any())
        {
            analysis.Analyzers(a =>
            {
                foreach (var analyzer in definition.Analyzers)
                {
                    a.Custom(analyzer.Name, ca => ca
                        .Tokenizer(analyzer.Tokenizer)
                        .Filters(analyzer.Filters)
                    );
                }
                return a;
            });
        }
        
        return analysis;
    }
    
    private TypeMappingDescriptor<DataDocument> BuildMappings(TypeMappingDescriptor<DataDocument> mapping, DocumentIndexDefinition definition)
    {
        return mapping
            .Dynamic(DynamicMapping.Strict)
            .Properties(p => p
                .Keyword(k => k
                    .Name(n => n.Id)
                )
                .Keyword(k => k
                    .Name(n => n.SchemaId)
                )
                .Keyword(k => k
                    .Name(n => n.SourceId)
                )
                .Date(d => d
                    .Name(n => n.CreatedAt)
                )
                .Date(d => d
                    .Name(n => n.UpdatedAt)
                )
                .Object<Dictionary<string, object>>(o => o
                    .Name(n => n.Data)
                    .Properties(dataProps => BuildDataProperties(dataProps, definition))
                )
                .Object<Dictionary<string, string>>(o => o
                    .Name(n => n.Metadata)
                )
            );
    }
    
    private PropertiesDescriptor<Dictionary<string, object>> BuildDataProperties(
        PropertiesDescriptor<Dictionary<string, object>> properties, 
        DocumentIndexDefinition definition)
    {
        if (definition.FieldMappings != null && definition.FieldMappings.Any())
        {
            foreach (var field in definition.FieldMappings)
            {
                switch (field.Type.ToLowerInvariant())
                {
                    case "text":
                        properties.Text(t => t
                            .Name(field.Name)
                            .Analyzer(field.Analyzer)
                        );
                        break;
                    case "keyword":
                        properties.Keyword(k => k
                            .Name(field.Name)
                        );
                        break;
                    case "integer":
                        properties.Number(n => n
                            .Name(field.Name)
                            .Type(NumberType.Integer)
                        );
                        break;
                    case "long":
                        properties.Number(n => n
                            .Name(field.Name)
                            .Type(NumberType.Long)
                        );
                        break;
                    case "float":
                        properties.Number(n => n
                            .Name(field.Name)
                            .Type(NumberType.Float)
                        );
                        break;
                    case "double":
                        properties.Number(n => n
                            .Name(field.Name)
                            .Type(NumberType.Double)
                        );
                        break;
                    case "date":
                        properties.Date(d => d
                            .Name(field.Name)
                            .Format("strict_date_optional_time||epoch_millis")
                        );
                        break;
                    case "boolean":
                        properties.Boolean(b => b
                            .Name(field.Name)
                        );
                        break;
                    case "object":
                        properties.Object<Dictionary<string, object>>(o => o
                            .Name(field.Name)
                        );
                        break;
                    case "nested":
                        properties.Nested<Dictionary<string, object>>(n => n
                            .Name(field.Name)
                        );
                        break;
                }
            }
        }
        
        return properties;
    }
    
    private QueryContainer BuildQuery(QueryContainerDescriptor<DataDocument> query, string queryString)
    {
        return query
            .MultiMatch(m => m
                .Query(queryString)
                .Type(TextQueryType.BestFields)
                .Fields(f => f.Field("data.*"))
            );
    }
    
    private QueryContainer BuildFilter(QueryContainerDescriptor<DataDocument> filter, List<FilterCondition> filters)
    {
        var filterQueries = new List<QueryContainer>();
        
        foreach (var condition in filters)
        {
            switch (condition.Operator.ToLowerInvariant())
            {
                case "eq":
                    filterQueries.Add(filter
                        .Term(t => t.Field($"data.{condition.Field}").Value(condition.Value))
                    );
                    break;
                case "gt":
                    filterQueries.Add(filter
                        .Range(r => r.Field($"data.{condition.Field}").GreaterThan(condition.Value))
                    );
                    break;
                case "lt":
                    filterQueries.Add(filter
                        .Range(r => r.Field($"data.{condition.Field}").LessThan(condition.Value))
                    );
                    break;
                case "gte":
                    filterQueries.Add(filter
                        .Range(r => r.Field($"data.{condition.Field}").GreaterThanOrEquals(condition.Value))
                    );
                    break;
                case "lte":
                    filterQueries.Add(filter
                        .Range(r => r.Field($"data.{condition.Field}").LessThanOrEquals(condition.Value))
                    );
                    break;
                case "contains":
                    filterQueries.Add(filter
                        .Wildcard(w => w.Field($"data.{condition.Field}").Value($"*{condition.Value}*"))
                    );
                    break;
                case "exists":
                    filterQueries.Add(filter
                        .Exists(e => e.Field($"data.{condition.Field}"))
                    );
                    break;
                default:
                    _logger.LogWarning("Unsupported filter operation: {operation}", condition.Operator);
                    break;
            }
        }
        
        return filter.Bool(b => b.Must(filterQueries.ToArray()));
    }
    
    private AggregationContainerDescriptor<DataDocument> BuildAggregations(
        AggregationContainerDescriptor<DataDocument> aggregations, 
        List<AggregationRequest> requests)
    {
        foreach (var agg in requests)
        {
            switch (agg.Type.ToLowerInvariant())
            {
                case "terms":
                    aggregations.Terms(agg.Name, t => t
                        .Field($"data.{agg.Field}")
                        .Size(agg.Size ?? 10)
                    );
                    break;
                case "range":
                    aggregations.Range(agg.Name, r => r
                        .Field($"data.{agg.Field}")
                        .Ranges(agg.Ranges.Select(range => new Nest.Range
                        {
                            From = range.From,
                            To = range.To,
                            Key = range.Key
                        }))
                    );
                    break;
                case "date_histogram":
                    aggregations.DateHistogram(agg.Name, d => d
                        .Field($"data.{agg.Field}")
                        .CalendarInterval(agg.Interval)
                        .Format("yyyy-MM-dd")
                    );
                    break;
                case "sum":
                    aggregations.Sum(agg.Name, s => s
                        .Field($"data.{agg.Field}")
                    );
                    break;
                case "avg":
                    aggregations.Average(agg.Name, a => a
                        .Field($"data.{agg.Field}")
                    );
                    break;
                case "min":
                    aggregations.Min(agg.Name, m => m
                        .Field($"data.{agg.Field}")
                    );
                    break;
                case "max":
                    aggregations.Max(agg.Name, m => m
                        .Field($"data.{agg.Field}")
                    );
                    break;
                case "stats":
                    aggregations.Stats(agg.Name, s => s
                        .Field($"data.{agg.Field}")
                    );
                    break;
            }
        }
        
        return aggregations;
    }
    
    private object ExtractAggregationValues(IAggregate aggregate)
    {
        if (aggregate is BucketAggregate bucketAggregate)
        {
            return bucketAggregate.Items.Select(bucket =>
            {
                var keyedBucket = bucket as KeyedBucket<object>;
                return new
                {
                    Key = keyedBucket?.Key,
                    DocCount = keyedBucket?.DocCount,
                    SubAggregations = keyedBucket?.InnerAggregations?.Count > 0
                        ? keyedBucket.InnerAggregations.ToDictionary(
                            a => a.Key,
                            a => ExtractAggregationValues(a.Value))
                        : null
                };
            }).ToList();
        }
        else if (aggregate is ValueAggregate valueAggregate)
        {
            return valueAggregate.Value;
        }
        else if (aggregate is StatsAggregate statsAggregate)
        {
            return new
            {
                Count = statsAggregate.Count,
                Min = statsAggregate.Min,
                Max = statsAggregate.Max,
                Average = statsAggregate.Average,
                Sum = statsAggregate.Sum
            };
        }
        
        return null;
    }
}

public class DataDocument
{
    public string Id { get; set; }
    public string SchemaId { get; set; }
    public string SourceId { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DocumentIndexDefinition
{
    public int? Shards { get; set; }
    public int? Replicas { get; set; }
    public string RefreshInterval { get; set; }
    public List<AnalyzerDefinition> Analyzers { get; set; }
    public List<FieldMapping> FieldMappings { get; set; }
}

public class AnalyzerDefinition
{
    public string Name { get; set; }
    public string Tokenizer { get; set; }
    public List<string> Filters { get; set; }
}

public class FieldMapping
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Analyzer { get; set; }
}

public class SearchQuery
{
    public string Query { get; set; }
    public List<FilterCondition> Filters { get; set; }
    public List<SortCondition> Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public List<AggregationRequest> Aggregations { get; set; }
}

public class AggregationRequest
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Field { get; set; }
    public int? Size { get; set; }
    public string Interval { get; set; }
    public List<AggregationRange> Ranges { get; set; }
}

public class AggregationRange
{
    public double? From { get; set; }
    public double? To { get; set; }
    public string Key { get; set; }
}

public class SearchResult<T>
{
    public List<T> Items { get; set; }
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, object> Aggregations { get; set; }
}
```

### 6. Generic Data API Implementation

```csharp
// GenericDataPlatform.API/Controllers/DataController.cs
[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly ILogger<DataController> _logger;
    
    public DataController(
        IDataService dataService,
        ILogger<DataController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }
    
    [HttpGet("sources")]
    public async Task<ActionResult<IEnumerable<DataSourceDefinition>>> GetSources(
        [FromQuery] string typeFilter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var sources = await _dataService.GetDataSourcesAsync(typeFilter, page, pageSize);
            return Ok(sources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data sources");
            return StatusCode(500, "An error occurred while retrieving data sources");
        }
    }
    
    [HttpGet("sources/{id}")]
    public async Task<ActionResult<DataSourceDefinition>> GetSource(string id)
    {
        try
        {
            var source = await _dataService.GetDataSourceAsync(id);
            if (source == null)
                return NotFound($"Data source with ID {id} not found");
                
            return Ok(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data source {id}", id);
            return StatusCode(500, $"An error occurred while retrieving data source {id}");
        }
    }
    
    [HttpPost("sources")]
    public async Task<ActionResult<DataSourceDefinition>> CreateSource(DataSourceDefinition source)
    {
        try
        {
            // Validate connection before creating
            var validationResult = await _dataService.ValidateConnectionAsync(source);
            if (!validationResult.IsValid)
                return BadRequest($"Connection validation failed: {validationResult.Message}");
                
            var result = await _dataService.CreateDataSourceAsync(source);
            return CreatedAtAction(nameof(GetSource), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data source");
            return StatusCode(500, "An error occurred while creating the data source");
        }
    }
    
    [HttpPut("sources/{id}")]
    public async Task<ActionResult<DataSourceDefinition>> UpdateSource(string id, DataSourceDefinition source)
    {
        try
        {
            if (id != source.Id)
                return BadRequest("ID in URL must match ID in body");
                
            var existingSource = await _dataService.GetDataSourceAsync(id);
            if (existingSource == null)
                return NotFound($"Data source with ID {id} not found");
                
            var result = await _dataService.UpdateDataSourceAsync(source);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data source {id}", id);
            return StatusCode(500, $"An error occurred while updating data source {id}");
        }
    }
    
    [HttpDelete("sources/{id}")]
    public async Task<ActionResult> DeleteSource(string id)
    {
        try
        {
            var existingSource = await _dataService.GetDataSourceAsync(id);
            if (existingSource == null)
                return NotFound($"Data source with ID {id} not found");
                
            var result = await _dataService.DeleteDataSourceAsync(id);
            if (result)
                return NoContent();
                
            return StatusCode(500, $"Failed to delete data source {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data source {id}", id);
            return StatusCode(500, $"An error occurred while deleting data source {id}");
        }
    }
    
    [HttpGet("sources/{id}/schema")]
    public async Task<ActionResult<DataSchema>> GetSchema(string id)
    {
        try
        {
            var source = await _dataService.GetDataSourceAsync(id);
            if (source == null)
                return NotFound($"Data source with ID {id} not found");
                
            var schema = await _dataService.GetSchemaAsync(id);
            return Ok(schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema for data source {id}", id);
            return StatusCode(500, $"An error occurred while retrieving schema for data source {id}");
        }
    }
    
    [HttpPost("sources/{id}/schema/infer")]
    public async Task<ActionResult<DataSchema>> InferSchema(
        string id, 
        [FromQuery] int sampleSize = 100)
    {
        try
        {
            var source = await _dataService.GetDataSourceAsync(id);
            if (source == null)
                return NotFound($"Data source with ID {id} not found");
                
            var schema = await _dataService.InferSchemaAsync(id, sampleSize);
            return Ok(schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inferring schema for data source {id}", id);
            return StatusCode(500, $"An error occurred while inferring schema for data source {id}");
        }
    }
    
    [HttpPost("sources/{id}/ingest")]
    public async Task<ActionResult<StartIngestionResponse>> StartIngestion(
        string id, 
        [FromBody] StartIngestionRequest request)
    {
        try
        {
            var source = await _dataService.GetDataSourceAsync(id);
            if (source == null)
                return NotFound($"Data source with ID {id} not found");
                
            var result = await _dataService.StartIngestionAsync(id, request.Parameters, request.FullRefresh);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting ingestion for data source {id}", id);
            return StatusCode(500, $"An error occurred while starting ingestion for data source {id}");
        }
    }
    
    [HttpGet("sources/{id}/ingestion/{jobId}")]
    public async Task<ActionResult<GetIngestionStatusResponse>> GetIngestionStatus(string id, string jobId)
    {
        try
        {
            var source = await _dataService.GetDataSourceAsync(id);
            if (source == null)
                return NotFound($"Data source with ID {id} not found");
                
            var status = await _dataService.GetIngestionStatusAsync(jobId);
            if (status == null)
                return NotFound($"Ingestion job with ID {jobId} not found");
                
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingestion status for job {jobId}", jobId);
            return StatusCode(500, $"An error occurred while getting ingestion status for job {jobId}");
        }
    }
    
    [HttpGet("records")]
    public async Task<ActionResult<PagedResult<DataRecord>>> GetRecords(
        [FromQuery] string sourceId,
        [FromQuery] Dictionary<string, string> filters,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _dataService.GetRecordsAsync(sourceId, filters, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving records for source {sourceId}", sourceId);
            return StatusCode(500, $"An error occurred while retrieving records for source {sourceId}");
        }
    }
    
    [HttpGet("records/{id}")]
    public async Task<ActionResult<DataRecord>> GetRecord(string id)
    {
        try
        {
            var record = await _dataService.GetRecordAsync(id);
            if (record == null)
                return NotFound($"Record with ID {id} not found");
                
            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving record {id}", id);
            return StatusCode(500, $"An error occurred while retrieving record {id}");
        }
    }
    
    [HttpPost("query")]
    public async Task<ActionResult<QueryResult>> Query(DataQuery query)
    {
        try
        {
            var result = await _dataService.QueryAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query");
            return StatusCode(500, "An error occurred while executing the query");
        }
    }
}

public class StartIngestionRequest
{
    public Dictionary<string, string> Parameters { get; set; }
    public bool FullRefresh { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class DataQuery
{
    public string SourceId { get; set; }
    public List<string> Fields { get; set; }
    public List<FilterCondition> Filters { get; set; }
    public List<SortCondition> Sort { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public AggregationOptions Aggregation { get; set; }
}

public class FilterCondition
{
    public string Field { get; set; }
    public string Operator { get; set; }
    public object Value { get; set; }
}

public class SortCondition
{
    public string Field { get; set; }
    public bool Descending { get; set; }
}

public class AggregationOptions
{
    public List<string> GroupBy { get; set; }
    public List<AggregateFunction> Functions { get; set; }
}

public class AggregateFunction
{
    public string Function { get; set; }
    public string Field { get; set; }
    public string Alias { get; set; }
}

public class QueryResult
{
    public List<Dictionary<string, object>> Data { get; set; }
    public int TotalCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## Configuration and Dependency Injection

```csharp
// GenericDataPlatform.StorageService/Startup.cs
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        services.Configure<S3Options>(Configuration.GetSection("S3"));
        services.Configure<AzureBlobOptions>(Configuration.GetSection("AzureBlob"));
        services.Configure<GcpStorageOptions>(Configuration.GetSection("GcpStorage"));
        services.Configure<FileSystemOptions>(Configuration.GetSection("FileSystem"));
        
        // Add AWS S3 client
        services.AddAWSService<IAmazonS3>(Configuration.GetAWSOptions("AWS"));
        
        // Add storage repositories based on configuration
        var storageType = Configuration.GetValue<string>("Storage:Type")?.ToLowerInvariant() ?? "s3";
        
        switch (storageType)
        {
            case "s3":
                services.AddScoped<IStorageRepository, S3Repository>();
                break;
            case "azureblob":
                services.AddScoped<IStorageRepository, AzureBlobRepository>();
                break;
            case "gcpstorage":
                services.AddScoped<IStorageRepository, GcpStorageRepository>();
                break;
            case "minio":
                services.AddScoped<IStorageRepository, MinioRepository>();
                break;
            case "filesystem":
                services.AddScoped<IStorageRepository, FileSystemRepository>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported storage type: {storageType}");
        }
        
        // Add gRPC services
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
        });
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<StorageHealthCheck>("storage_health");
            
        // Add telemetry
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddGrpcClientInstrumentation())
            .WithMetrics(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<StorageService>();
            endpoints.MapHealthChecks("/health");
        });
    }
}
```

## Example Usage Scenarios

### 1. Setting Up a Data Pipeline for CSV Files

This example shows how to set up a complete data pipeline for processing CSV files on an FTP server:

```csharp
// Define the data source
var csvSource = new DataSourceDefinition
{
    Name = "Sales Data",
    Description = "Daily sales data from FTP server",
    Type = DataSourceType.Ftp,
    ConnectionProperties = new Dictionary<string, string>
    {
        ["host"] = "ftp.example.com",
        ["username"] = "ftpuser",
        ["password"] = "ftppassword",
        ["directory"] = "/sales/daily",
        ["filePattern"] = "sales_*.csv"
    },
    IngestMode = DataIngestMode.Incremental,
    RefreshPolicy = DataRefreshPolicy.Scheduled,
    ValidationRules = new Dictionary<string, string>
    {
        ["required_headers"] = "date,product_id,quantity,price,customer_id"
    }
};

// Create the data source
var sourceId = await _dataService.CreateDataSourceAsync(csvSource);

// Define the schema
var salesSchema = new DataSchema
{
    Name = "SalesData",
    Description = "Schema for daily sales data",
    Type = SchemaType.Strict,
    Fields = new List<SchemaField>
    {
        new SchemaField
        {
            Name = "date",
            Description = "Sale date",
            Type = FieldType.DateTime,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "product_id",
            Description = "Product identifier",
            Type = FieldType.String,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "quantity",
            Description = "Quantity sold",
            Type = FieldType.Integer,
            IsRequired = true,
            Validation = new ValidationRules
            {
                MinValue = 1
            }
        },
        new SchemaField
        {
            Name = "price",
            Description = "Unit price",
            Type = FieldType.Decimal,
            IsRequired = true,
            Validation = new ValidationRules
            {
                MinValue = 0
            }
        },
        new SchemaField
        {
            Name = "customer_id",
            Description = "Customer identifier",
            Type = FieldType.String,
            IsRequired = true
        }
    }
};

// Associate schema with data source
await _dataService.SetSchemaAsync(sourceId, salesSchema);

// Define the ETL pipeline
var pipelineContext = new PipelineContext
{
    Source = csvSource,
    Stages = new List<PipelineStage>
    {
        new PipelineStage
        {
            Id = "extract",
            Name = "Extract from FTP",
            Type = StageType.Extract,
            Configuration = new Dictionary<string, object>
            {
                ["extractorType"] = "FtpExtractor",
                ["delimiter"] = ",",
                ["hasHeader"] = true
            }
        },
        new PipelineStage
        {
            Id = "transform",
            Name = "Transform Sales Data",
            Type = StageType.Transform,
            DependsOn = new List<string> { "extract" },
            Configuration = new Dictionary<string, object>
            {
                ["transformerType"] = "CsvTransformer",
                ["mappings"] = new Dictionary<string, string>
                {
                    ["date"] = "date",
                    ["product_id"] = "product_id",
                    ["quantity"] = "quantity",
                    ["price"] = "price",
                    ["customer_id"] = "customer_id",
                    ["total"] = "#calculate(quantity * price)"
                },
                ["dataTypeConversions"] = new Dictionary<string, string>
                {
                    ["date"] = "datetime",
                    ["quantity"] = "int",
                    ["price"] = "decimal"
                }
            }
        },
        new PipelineStage
        {
            Id = "validate",
            Name = "Validate Sales Data",
            Type = StageType.Validate,
            DependsOn = new List<string> { "transform" },
            Configuration = new Dictionary<string, object>
            {
                ["validatorType"] = "SchemaValidator",
                ["schemaId"] = salesSchema.Id
            }
        },
        new PipelineStage
        {
            Id = "load-database",
            Name = "Load to Database",
            Type = StageType.Load,
            DependsOn = new List<string> { "validate" },
            Configuration = new Dictionary<string, object>
            {
                ["loaderType"] = "DatabaseLoader",
                ["tableName"] = "sales",
                ["batchSize"] = 1000,
                ["createIfNotExists"] = true
            }
        },
        new PipelineStage
        {
            Id = "load-document-db",
            Name = "Load to Document DB",
            Type = StageType.Load,
            DependsOn = new List<string> { "validate" },
            Configuration = new Dictionary<string, object>
            {
                ["loaderType"] = "DocumentLoader",
                ["indexName"] = "sales",
                ["idField"] = "#concat(date,'-',product_id,'-',customer_id)"
            }
        },
        new PipelineStage
        {
            Id = "feature-engineering",
            Name = "Create Features",
            Type = StageType.Enrich,
            DependsOn = new List<string> { "validate" },
            Configuration = new Dictionary<string, object>
            {
                ["enricherType"] = "FeatureEnricher",
                ["featureNamespace"] = "sales_features",
                ["features"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "daily_sales_by_product",
                        ["keyField"] = "product_id",
                        ["valueField"] = "total",
                        ["aggregation"] = "sum",
                        ["timeField"] = "date",
                        ["timeWindow"] = "1d"
                    },
                    new Dictionary<string, object>
                    {
                        ["name"] = "customer_purchase_frequency",
                        ["keyField"] = "customer_id",
                        ["valueField"] = "1",
                        ["aggregation"] = "count",
                        ["timeField"] = "date",
                        ["timeWindow"] = "30d"
                    }
                }
            }
        }
    }
};

// Schedule the pipeline to run daily
var schedule = new PipelineSchedule
{
    PipelineId = "sales-pipeline",
    Schedule = "0 0 1 * * ?", // Run at 1:00 AM daily
    Enabled = true
};

await _schedulerService.CreateScheduleAsync(schedule);
```

### 2. Real-time Data Processing with Kafka

This example shows how to set up a real-time data processing pipeline using Kafka:

```csharp
// Define the Kafka data source
var kafkaSource = new DataSourceDefinition
{
    Name = "User Activity Stream",
    Description = "Real-time user activity events from Kafka",
    Type = DataSourceType.Streaming,
    ConnectionProperties = new Dictionary<string, string>
    {
        ["provider"] = "kafka",
        ["bootstrapServers"] = "kafka1:9092,kafka2:9092",
        ["topic"] = "user-activity",
        ["groupId"] = "data-platform-consumer",
        ["autoOffsetReset"] = "latest"
    },
    IngestMode = DataIngestMode.Incremental,
    RefreshPolicy = DataRefreshPolicy.EventDriven
};

// Define the schema for user activity events
var activitySchema = new DataSchema
{
    Name = "UserActivity",
    Description = "Schema for user activity events",
    Type = SchemaType.Flexible, // Allow additional fields
    Fields = new List<SchemaField>
    {
        new SchemaField
        {
            Name = "userId",
            Description = "User identifier",
            Type = FieldType.String,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "event",
            Description = "Event type",
            Type = FieldType.String,
            IsRequired = true,
            Validation = new ValidationRules
            {
                AllowedValues = new[] { "login", "logout", "view", "click", "purchase" }
            }
        },
        new SchemaField
        {
            Name = "timestamp",
            Description = "Event timestamp",
            Type = FieldType.DateTime,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "properties",
            Description = "Event properties",
            Type = FieldType.Complex,
            IsRequired = false,
            NestedFields = new List<SchemaField>
            {
                new SchemaField
                {
                    Name = "page",
                    Description = "Page URL",
                    Type = FieldType.String,
                    IsRequired = false
                },
                new SchemaField
                {
                    Name = "itemId",
                    Description = "Item identifier",
                    Type = FieldType.String,
                    IsRequired = false
                },
                new SchemaField
                {
                    Name = "value",
                    Description = "Event value",
                    Type = FieldType.Decimal,
                    IsRequired = false
                }
            }
        }
    }
};

// Create the streaming processor
var streamProcessor = new StreamProcessorConfiguration
{
    SourceId = kafkaSource.Id,
    SchemaId = activitySchema.Id,
    BufferSize = 1000,
    BatchInterval = TimeSpan.FromSeconds(5),
    Processor = new StreamProcessorPipeline
    {
        Steps = new List<StreamProcessingStep>
        {
            new StreamProcessingStep
            {
                Type = "Filter",
                Configuration = new Dictionary<string, object>
                {
                    ["condition"] = "event != 'heartbeat'"
                }
            },
            new StreamProcessingStep
            {
                Type = "Enrich",
                Configuration = new Dictionary<string, object>
                {
                    ["fields"] = new Dictionary<string, string>
                    {
                        ["sessionId"] = "#sessionId(userId, timestamp)",
                        ["processingTime"] = "#now()"
                    }
                }
            },
            new StreamProcessingStep
            {
                Type = "Route",
                Configuration = new Dictionary<string, object>
                {
                    ["routes"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "purchases",
                            ["condition"] = "event == 'purchase'",
                            ["destination"] = new Dictionary<string, object>
                            {
                                ["type"] = "document",
                                ["indexName"] = "user-purchases"
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "pageviews",
                            ["condition"] = "event == 'view' && properties.page != null",
                            ["destination"] = new Dictionary<string, object>
                            {
                                ["type"] = "timeseries",
                                ["tableName"] = "pageviews"
                            }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "all-events",
                            ["condition"] = "true",
                            ["destination"] = new Dictionary<string, object>
                            {
                                ["type"] = "storage",
                                ["path"] = "user-activity/{yyyy}/{MM}/{dd}/{HH}/"
                            }
                        }
                    ]
                }
            }
        }
    },
    ErrorHandling = new StreamErrorHandling
    {
        RetryCount = 3,
        RetryInterval = TimeSpan.FromSeconds(5),
        DeadLetterDestination = new Dictionary<string, object>
        {
            ["type"] = "document",
            ["indexName"] = "processing-errors"
        }
    }
};

// Start the stream processor
await _streamProcessorService.StartProcessorAsync(streamProcessor);
```

### 3. Creating a Machine Learning Pipeline

This example demonstrates how to build a machine learning pipeline for product recommendations:

```csharp
// Define the ML pipeline
var mlPipeline = new MLPipelineDefinition
{
    Name = "Product Recommendation Pipeline",
    Description = "Generates product recommendations based on user behavior",
    Stages = new List<MLPipelineStage>
    {
        new MLPipelineStage
        {
            Name = "Feature Generation",
            Type = MLStageType.FeatureGeneration,
            Configuration = new Dictionary<string, object>
            {
                ["sourceType"] = "document",
                ["sourceName"] = "user-purchases",
                ["featureNamespace"] = "product_recommendations",
                ["features"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "user_purchase_history",
                        ["type"] = "list",
                        ["valueField"] = "properties.itemId",
                        ["groupByField"] = "userId",
                        ["timeField"] = "timestamp",
                        ["timeWindow"] = "90d"
                    },
                    new Dictionary<string, object>
                    {
                        ["name"] = "product_popularity",
                        ["type"] = "numeric",
                        ["valueField"] = "1",
                        ["aggregation"] = "count",
                        ["groupByField"] = "properties.itemId",
                        ["timeField"] = "timestamp",
                        ["timeWindow"] = "30d"
                    },
                    new Dictionary<string, object>
                    {
                        ["name"] = "product_copurchases",
                        ["type"] = "matrix",
                        ["valueField"] = "1",
                        ["aggregation"] = "count",
                        ["rowField"] = "properties.itemId",
                        ["columnField"] = "properties.itemId",
                        ["windowField"] = "sessionId",
                        ["timeField"] = "timestamp",
                        ["timeWindow"] = "90d"
                    }
                }
            }
        },
        new MLPipelineStage
        {
            Name = "Product Embeddings",
            Type = MLStageType.Embedding,
            DependsOn = new List<string> { "Feature Generation" },
            Configuration = new Dictionary<string, object>
            {
                ["algorithm"] = "matrix_factorization",
                ["inputFeature"] = "product_copurchases",
                ["dimensions"] = 50,
                ["learningRate"] = 0.01,
                ["iterations"] = 100,
                ["outputFeature"] = "product_embeddings"
            }
        },
        new MLPipelineStage
        {
            Name = "Recommendation Model",
            Type = MLStageType.ModelTraining,
            DependsOn = new List<string> { "Product Embeddings" },
            Configuration = new Dictionary<string, object>
            {
                ["algorithm"] = "item_similarity",
                ["inputFeatures"] = new List<string>
                {
                    "product_embeddings",
                    "product_popularity"
                },
                ["modelName"] = "product_recommender",
                ["hyperparameters"] = new Dictionary<string, object>
                {
                    ["topK"] = 10,
                    ["similarityMetric"] = "cosine",
                    ["weightPopularity"] = 0.2
                }
            }
        },
        new MLPipelineStage
        {
            Name = "Model Deployment",
            Type = MLStageType.ModelDeployment,
            DependsOn = new List<string> { "Recommendation Model" },
            Configuration = new Dictionary<string, object>
            {
                ["deploymentName"] = "product-recommender",
                ["modelName"] = "product_recommender",
                ["version"] = "1.0",
                ["resources"] = new Dictionary<string, object>
                {
                    ["cpu"] = 1,
                    ["memory"] = "1Gi"
                },
                ["scaling"] = new Dictionary<string, object>
                {
                    ["minReplicas"] = 2,
                    ["maxReplicas"] = 5,
                    ["targetCpuUtilization"] = 70
                }
            }
        }
    },
    Schedule = new MLPipelineSchedule
    {
        Type = "cron",
        Expression = "0 0 2 * * ?", // Run at 2:00 AM daily
        Enabled = true
    }
};

// Create and start the ML pipeline
var pipelineId = await _mlService.CreatePipelineAsync(mlPipeline);
await _mlService.StartPipelineAsync(pipelineId);
```

## Platform Configuration

### Environment-Specific Configuration

The platform supports different configuration for various environments:

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Storage": {
    "Type": "minio",
    "ConnectionString": "S3"
  },
  "S3": {
    "BucketName": "data-platform-dev",
    "Region": "us-east-1",
    "ServiceUrl": "http://localhost:9000",
    "ForcePathStyle": true,
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  },
  "Database": {
    "Provider": "postgresql",
    "ConnectionString": "Host=localhost;Database=dataplatform;Username=postgres;Password=postgres"
  },
  "Document": {
    "Provider": "elasticsearch",
    "ConnectionString": "http://localhost:9200"
  },
  "Vector": {
    "Provider": "pgvector",
    "ConnectionString": "Host=localhost;Database=vectordb;Username=postgres;Password=postgres"
  },
  "Feature": {
    "Provider": "custom",
    "ConnectionString": "Host=localhost;Database=featurestore;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  },
  "Security": {
    "Authentication": {
      "JwtBearer": {
        "Authority": "https://localhost:5001",
        "Audience": "data-platform-api"
      }
    }
  }
}
```

### Kubernetes ConfigMap

For Kubernetes deployments, configuration can be managed using ConfigMaps:

```yaml
# deploy/kubernetes/configmaps/data-platform-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: data-platform-config
  namespace: data-platform
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "Microsoft.Hosting.Lifetime": "Information"
        }
      },
      "Storage": {
        "Type": "s3",
        "ConnectionString": "S3"
      },
      "S3": {
        "BucketName": "data-platform-prod",
        "Region": "us-east-1"
      },
      "Database": {
        "Provider": "postgresql",
        "ConnectionString": "Database:ConnectionString"
      },
      "Document": {
        "Provider": "elasticsearch",
        "ConnectionString": "Elasticsearch:ConnectionString"
      },
      "Vector": {
        "Provider": "pgvector",
        "ConnectionString": "Database:ConnectionString"
      },
      "Feature": {
        "Provider": "custom",
        "ConnectionString": "Database:ConnectionString"
      },
      "Kafka": {
        "BootstrapServers": "kafka-headless.kafka.svc.cluster.local:9092"
      },
      "Cors": {
        "AllowedOrigins": ["https://app.example.com"]
      },
      "Security": {
        "Authentication": {
          "JwtBearer": {
            "Authority": "https://auth.example.com",
            "Audience": "data-platform-api"
          }
        }
      },
      "OpenTelemetry": {
        "Enabled": true,
        "ServiceName": "data-platform",
        "Endpoint": "otel-collector:4317"
      }
    }
```

## How to Use the Platform

### Creating a Data Source

```csharp
// Example of creating a REST API data source
var restApiSource = new DataSourceDefinition
{
    Name = "Weather API",
    Description = "Current weather data from OpenWeatherMap API",
    Type = DataSourceType.RestApi,
    ConnectionProperties = new Dictionary<string, string>
    {
        ["baseUrl"] = "https://api.openweathermap.org/data/2.5",
        ["endpoint"] = "weather",
        ["authType"] = "apikey",
        ["apiKeyHeader"] = "appid",
        ["apiKey"] = "your-api-key",
        ["responseFormat"] = "json"
    },
    IngestMode = DataIngestMode.FullLoad,
    RefreshPolicy = DataRefreshPolicy.Scheduled,
    ValidationRules = new Dictionary<string, string>
    {
        ["required_fields"] = "main,weather,wind,name"
    }
};

// Example of creating a database data source
var databaseSource = new DataSourceDefinition
{
    Name = "Product Database",
    Description = "Product catalog from PostgreSQL database",
    Type = DataSourceType.Database,
    ConnectionProperties = new Dictionary<string, string>
    {
        ["provider"] = "postgresql",
        ["connectionString"] = "Host=localhost;Database=products;Username=user;Password=password",
        ["table"] = "products",
        ["schema"] = "public"
    },
    IngestMode = DataIngestMode.Incremental,
    RefreshPolicy = DataRefreshPolicy.Manual
};
```

### Defining a Dynamic Schema

```csharp
// Example of defining a schema for weather data
var weatherSchema = new DataSchema
{
    Name = "WeatherData",
    Description = "Schema for weather API data",
    Type = SchemaType.Flexible,
    Fields = new List<SchemaField>
    {
        new SchemaField
        {
            Name = "city",
            Description = "City name",
            Type = FieldType.String,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "country",
            Description = "Country code",
            Type = FieldType.String,
            IsRequired = true,
            Validation = new ValidationRules
            {
                MinLength = 2,
                MaxLength = 2
            }
        },
        new SchemaField
        {
            Name = "temperature",
            Description = "Current temperature in Celsius",
            Type = FieldType.Decimal,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "humidity",
            Description = "Current humidity percentage",
            Type = FieldType.Integer,
            IsRequired = true,
            Validation = new ValidationRules
            {
                MinValue = 0,
                MaxValue = 100
            }
        },
        new SchemaField
        {
            Name = "wind",
            Description = "Wind information",
            Type = FieldType.Complex,
            IsRequired = true,
            NestedFields = new List<SchemaField>
            {
                new SchemaField
                {
                    Name = "speed",
                    Description = "Wind speed in m/s",
                    Type = FieldType.Decimal,
                    IsRequired = true
                },
                new SchemaField
                {
                    Name = "direction",
                    Description = "Wind direction in degrees",
                    Type = FieldType.Integer,
                    IsRequired = true,
                    Validation = new ValidationRules
                    {
                        MinValue = 0,
                        MaxValue = 360
                    }
                }
            }
        },
        new SchemaField
        {
            Name = "condition",
            Description = "Weather condition",
            Type = FieldType.String,
            IsRequired = true
        },
        new SchemaField
        {
            Name = "timestamp",
            Description = "Measurement timestamp",
            Type = FieldType.DateTime,
            IsRequired = true
        }
    },
    Version = new SchemaVersion
    {
        VersionNumber = "1.0.0",
        EffectiveDate = DateTime.UtcNow
    }
};
```

### Creating an ETL Pipeline

```csharp
// Example of creating an ETL pipeline for transforming and loading weather data
var pipelineContext = new PipelineContext
{
    PipelineId = "weather-pipeline-1",
    Source = restApiSource,
    Stages = new List<PipelineStage>
    {
        new PipelineStage
        {
            Id = "extract",
            Name = "Extract from Weather API",
            Type = StageType.Extract,
            Configuration = new Dictionary<string, object>
            {
                ["extractorType"] = "RestApiExtractor",
                ["parameters"] = new Dictionary<string, object>
                {
                    ["q"] = "London,uk",
                    ["units"] = "metric"
                }
            }
        },
        new PipelineStage
        {
            Id = "transform",
            Name = "Transform Weather Data",
            Type = StageType.Transform,
            DependsOn = new List<string> { "extract" },
            Configuration = new Dictionary<string, object>
            {
                ["transformerType"] = "JsonTransformer",
                ["mappings"] = new Dictionary<string, string>
                {
                    ["city"] = "$.name",
                    ["country"] = "$.sys.country",
                    ["temperature"] = "$.main.temp",
                    ["humidity"] = "$.main.humidity",
                    ["wind.speed"] = "$.wind.speed",
                    ["wind.direction"] = "$.wind.deg",
                    ["condition"] = "$.weather[0].main",
                    ["timestamp"] = "#utcnow"
                }
            }
        },
        new PipelineStage
        {
            Id = "validate",
            Name = "Validate Weather Data",
            Type = StageType.Validate,
            DependsOn = new List<string> { "transform" },
            Configuration = new Dictionary<string, object>
            {
                ["validatorType"] = "SchemaValidator",
                ["schemaId"] = weatherSchema.Id
            }
        },
        new PipelineStage
        {
            Id = "load-db",
            Name = "Load to Database",
            Type = StageType.Load,
            DependsOn = new List<string> { "validate" },
            Configuration = new Dictionary<string, object>
            {
                ["loaderType"] = "DatabaseLoader",
                ["tableName"] = "weather_data",
                ["createIfNotExists"] = true,
                ["batchSize"] = 100
            }
        },
        new PipelineStage
        {
            Id = "load-document-db",
            Name = "Load to Document DB",
            Type = StageType.Load,
            DependsOn = new List<string> { "validate" },
            Configuration = new Dictionary<string, object>
            {
                ["loaderType"] = "DocumentLoader",
                ["indexName"] = "weather-data",
                ["idField"] = "city"
            }
        }
    },
    Parameters = new Dictionary<string, object>
    {
        ["runId"] = Guid.NewGuid().ToString(),
        ["timestamp"] = DateTime.UtcNow
    }
};

// Execute the pipeline
var pipelineProcessor = serviceProvider.GetRequiredService<IPipelineProcessor>();
var result = await pipelineProcessor.ProcessAsync(pipelineContext);
```

## Kubernetes Deployment

The platform can also be deployed to Kubernetes using the following manifests:

```yaml
# deploy/kubernetes/database-service.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: database-service
  namespace: data-platform
  labels:
    app: database-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: database-service
  template:
    metadata:
      labels:
        app: database-service
    spec:
      containers:
      - name: database-service
        image: generic-data-platform/database:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: Database__Provider
          value: postgresql
        - name: Database__ConnectionString
          valueFrom:
            secretKeyRef:
              name: database-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "200m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 30
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 60
---
apiVersion: v1
kind: Service
metadata:
  name: database-service
  namespace: data-platform
spec:
  selector:
    app: database-service
  ports:
  - port: 80
    targetPort: 80
  type: ClusterIP
```

Similar deployment manifests are created for each microservice in the platform.

## Docker Deployment

```yaml
# deploy/docker/docker-compose.yml
version: '3.8'

services:
  ingestion-service:
    image: generic-data-platform/ingestion:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.IngestionService/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
    ports:
      - "5001:80"
      - "5002:443"
    volumes:
      - ingestion-data:/app/data
    depends_on:
      - storage-service
      - database-service
    networks:
      - data-platform-network

  storage-service:
    image: generic-data-platform/storage:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.StorageService/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - Storage__Type=s3
      - S3__BucketName=data-platform-storage
      - S3__Region=us-east-1
      - S3__ForcePathStyle=true
      - S3__ServiceUrl=http://minio:9000
    ports:
      - "5003:80"
      - "5004:443"
    depends_on:
      - minio
    networks:
      - data-platform-network

  database-service:
    image: generic-data-platform/database:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.DatabaseService/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - Database__Provider=postgresql
      - Database__ConnectionString=Host=postgres;Database=dataplatform;Username=postgres;Password=postgres
    ports:
      - "5005:80"
      - "5006:443"
    depends_on:
      - postgres
    networks:
      - data-platform-network

  document-service:
    image: generic-data-platform/document:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.DocumentService/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - Document__Provider=elasticsearch
      - Document__ConnectionString=http://elasticsearch:9200
    ports:
      - "5007:80"
      - "5008:443"
    depends_on:
      - elasticsearch
    networks:
      - data-platform-network

  vector-service:
    image: generic-data-platform/vector:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.VectorService/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - Vector__Provider=pgvector
      - Vector__ConnectionString=Host=postgres;Database=vectordb;Username=postgres;Password=postgres
    ports:
      - "5009:80"
      - "5010:443"
    depends_on:
      - postgres
    networks:
      - data-platform-network

  feature-service:
    image: generic-data-platform/feature:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.FeatureService/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
    ports:
      - "5011:80"
      - "5012:443"
    depends_on:
      - redis
    networks:
      - data-platform-network

  etl-service:
    image: generic-data-platform/etl:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.ETL/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
    ports:
      - "5013:80"
      - "5014:443"
    depends_on:
      - storage-service
      - database-service
      - document-service
    networks:
      - data-platform-network

  ml-service:
    image: generic-data-platform/ml:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.ML/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
    ports:
      - "5015:80"
      - "5016:443"
    depends_on:
      - feature-service
    networks:
      - data-platform-network

  api:
    image: generic-data-platform/api:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
    ports:
      - "8080:80"
      - "8443:443"
    depends_on:
      - ingestion-service
      - storage-service
      - database-service
      - document-service
      - vector-service
      - feature-service
      - etl-service
      - ml-service
    networks:
      - data-platform-network

  compliance-service:
    image: generic-data-platform/compliance:latest
    build:
      context: ../../
      dockerfile: src/GenericDataPlatform.Compliance/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
    ports:
      - "5017:80"
      - "5018:443"
    networks:
      - data-platform-network

  # Infrastructure services
  postgres:
    image: postgres:15-alpine
    environment:
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_USER=postgres
      - POSTGRES_DB=dataplatform
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - data-platform-network

  timescaledb:
    image: timescale/timescaledb:latest-pg15
    environment:
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_USER=postgres
      - POSTGRES_DB=timeseries
    volumes:
      - timescale-data:/var/lib/postgresql/data
    ports:
      - "5433:5432"
    networks:
      - data-platform-network

  minio:
    image: minio/minio
    command: server /data --console-address ":9001"
    environment:
      - MINIO_ROOT_USER=minioadmin
      - MINIO_ROOT_PASSWORD=minioadmin
    volumes:
      - minio-data:/data
    ports:
      - "9000:9000"
      - "9001:9001"
    networks:
      - data-platform-network

  elasticsearch:
    image: elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data
    ports:
      - "9200:9200"
    networks:
      - data-platform-network

  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
    ports:
      - "6379:6379"
    networks:
      - data-platform-network

volumes:
  postgres-data:
  timescale-data:
  minio-data:
  elasticsearch-data:
  redis-data:
  ingestion-data:

networks:
  data-platform-network:
    driver: bridge