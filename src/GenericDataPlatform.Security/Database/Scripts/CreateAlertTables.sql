-- Create AlertRules table
CREATE TABLE IF NOT EXISTS AlertRules (
    Id NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Severity INT NOT NULL,
    MetricName NVARCHAR(100) NOT NULL,
    Labels NVARCHAR(MAX) NULL, -- JSON string for labels dictionary
    Operator INT NOT NULL,
    Threshold FLOAT NOT NULL,
    Enabled BIT NOT NULL DEFAULT 1,
    AutoResolve BIT NOT NULL DEFAULT 1,
    Notifications NVARCHAR(MAX) NULL, -- JSON string for notifications list
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);

-- Create Alerts table
CREATE TABLE IF NOT EXISTS Alerts (
    Id NVARCHAR(50) PRIMARY KEY,
    RuleId NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Severity INT NOT NULL,
    Status INT NOT NULL,
    MetricName NVARCHAR(100) NOT NULL,
    MetricValue FLOAT NOT NULL,
    Labels NVARCHAR(MAX) NULL, -- JSON string for labels dictionary
    Timestamp DATETIME2 NOT NULL,
    ResolvedAt DATETIME2 NULL,
    Resolution NVARCHAR(500) NULL,
    FOREIGN KEY (RuleId) REFERENCES AlertRules(Id)
);

-- Create index on Status for faster filtering
CREATE INDEX IX_Alerts_Status ON Alerts(Status);

-- Create index on Severity for faster filtering
CREATE INDEX IX_Alerts_Severity ON Alerts(Severity);

-- Create index on RuleId for faster filtering
CREATE INDEX IX_Alerts_RuleId ON Alerts(RuleId);

-- Create index on MetricName for faster filtering
CREATE INDEX IX_AlertRules_MetricName ON AlertRules(MetricName);
