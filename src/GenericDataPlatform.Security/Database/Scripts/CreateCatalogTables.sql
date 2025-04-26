-- Create DataAssets table
CREATE TABLE IF NOT EXISTS DataAssets (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Type NVARCHAR(50) NOT NULL,
    Format NVARCHAR(50) NULL,
    Location NVARCHAR(255) NOT NULL,
    Owner NVARCHAR(100) NULL,
    Steward NVARCHAR(100) NULL,
    Tags NVARCHAR(MAX) NULL, -- JSON array of tags
    Schema NVARCHAR(MAX) NULL, -- JSON array of fields
    Metadata NVARCHAR(MAX) NULL, -- JSON dictionary
    QualityMetrics NVARCHAR(MAX) NULL, -- JSON dictionary
    SensitivityClassification NVARCHAR(50) NULL,
    RetentionPolicy NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    LastAccessedAt DATETIME2 NULL
);

-- Create GlossaryTerms table
CREATE TABLE IF NOT EXISTS GlossaryTerms (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Definition NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(100) NULL,
    Abbreviation NVARCHAR(50) NULL,
    Synonyms NVARCHAR(MAX) NULL, -- JSON array of synonyms
    RelatedTerms NVARCHAR(MAX) NULL, -- JSON array of related term IDs
    Examples NVARCHAR(MAX) NULL, -- JSON array of examples
    Owner NVARCHAR(100) NULL,
    Steward NVARCHAR(100) NULL,
    Status NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);

-- Create AssetTermRelations table for linking assets to glossary terms
CREATE TABLE IF NOT EXISTS AssetTermRelations (
    Id NVARCHAR(50) PRIMARY KEY,
    AssetId NVARCHAR(50) NOT NULL,
    TermId NVARCHAR(50) NOT NULL,
    FieldName NVARCHAR(100) NULL, -- If the term is linked to a specific field
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (AssetId) REFERENCES DataAssets(Id),
    FOREIGN KEY (TermId) REFERENCES GlossaryTerms(Id)
);

-- Create indexes
CREATE INDEX IX_DataAssets_Name ON DataAssets(Name);
CREATE INDEX IX_DataAssets_Type ON DataAssets(Type);
CREATE INDEX IX_DataAssets_Owner ON DataAssets(Owner);
CREATE INDEX IX_DataAssets_Location ON DataAssets(Location);
CREATE INDEX IX_DataAssets_SensitivityClassification ON DataAssets(SensitivityClassification);

CREATE INDEX IX_GlossaryTerms_Name ON GlossaryTerms(Name);
CREATE INDEX IX_GlossaryTerms_Category ON GlossaryTerms(Category);
CREATE INDEX IX_GlossaryTerms_Status ON GlossaryTerms(Status);

CREATE INDEX IX_AssetTermRelations_AssetId ON AssetTermRelations(AssetId);
CREATE INDEX IX_AssetTermRelations_TermId ON AssetTermRelations(TermId);
