-- Create DataEntities table
CREATE TABLE IF NOT EXISTS DataEntities (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    Schema NVARCHAR(MAX) NULL, -- JSON schema
    Location NVARCHAR(255) NOT NULL,
    Owner NVARCHAR(100) NULL,
    Tags NVARCHAR(MAX) NULL, -- JSON array of tags
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    LastAccessedAt DATETIME2 NULL
);

-- Create DataLineage table
CREATE TABLE IF NOT EXISTS DataLineage (
    Id NVARCHAR(50) PRIMARY KEY,
    SourceEntityId NVARCHAR(50) NOT NULL,
    TargetEntityId NVARCHAR(50) NOT NULL,
    ProcessId NVARCHAR(50) NULL,
    ProcessName NVARCHAR(100) NULL,
    ProcessType NVARCHAR(50) NULL,
    TransformationDetails NVARCHAR(MAX) NULL, -- JSON details of transformation
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (SourceEntityId) REFERENCES DataEntities(Id),
    FOREIGN KEY (TargetEntityId) REFERENCES DataEntities(Id)
);

-- Create DataProcesses table
CREATE TABLE IF NOT EXISTS DataProcesses (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    Owner NVARCHAR(100) NULL,
    Schedule NVARCHAR(100) NULL,
    LastRunAt DATETIME2 NULL,
    Status NVARCHAR(50) NULL,
    Configuration NVARCHAR(MAX) NULL, -- JSON configuration
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);

-- Create DataQuality table
CREATE TABLE IF NOT EXISTS DataQuality (
    Id NVARCHAR(50) PRIMARY KEY,
    EntityId NVARCHAR(50) NOT NULL,
    MetricName NVARCHAR(100) NOT NULL,
    MetricValue FLOAT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    FOREIGN KEY (EntityId) REFERENCES DataEntities(Id)
);

-- Create indexes
CREATE INDEX IX_DataEntities_Type ON DataEntities(Type);
CREATE INDEX IX_DataEntities_Location ON DataEntities(Location);
CREATE INDEX IX_DataLineage_SourceEntityId ON DataLineage(SourceEntityId);
CREATE INDEX IX_DataLineage_TargetEntityId ON DataLineage(TargetEntityId);
CREATE INDEX IX_DataLineage_ProcessId ON DataLineage(ProcessId);
CREATE INDEX IX_DataProcesses_Type ON DataProcesses(Type);
CREATE INDEX IX_DataQuality_EntityId ON DataQuality(EntityId);
CREATE INDEX IX_DataQuality_MetricName ON DataQuality(MetricName);
