# Ingestion Service API Documentation

The Ingestion Service provides APIs for connecting to various data sources, extracting data, and ingesting it into the Generic Data Platform.

## Base URL

```
https://localhost:7245/api
```

## Authentication

All API requests require authentication using JWT (JSON Web Token). Include the token in the Authorization header:

```
Authorization: Bearer {your_jwt_token}
```

## Data Sources

### List Data Sources

Returns a list of all data sources.

```
GET /api/data-sources
```

#### Response

```json
[
  {
    "id": "ds-001",
    "name": "Sample SQL Server Database",
    "description": "Sample SQL Server database for testing",
    "type": "Database",
    "connectionProperties": {
      "provider": "sqlserver",
      "server": "localhost",
      "database": "SampleDB",
      "username": "user",
      "password": "password"
    },
    "schema": {
      "id": "schema-001",
      "name": "Customers Schema",
      "fields": [...]
    }
  },
  ...
]
```

### Get Data Source

Returns details of a specific data source.

```
GET /api/data-sources/{id}
```

#### Parameters

- `id` (path): The ID of the data source

#### Response

```json
{
  "id": "ds-001",
  "name": "Sample SQL Server Database",
  "description": "Sample SQL Server database for testing",
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "SampleDB",
    "username": "user",
    "password": "password"
  },
  "schema": {
    "id": "schema-001",
    "name": "Customers Schema",
    "fields": [...]
  }
}
```

### Create Data Source

Creates a new data source.

```
POST /api/data-sources
```

#### Request Body

```json
{
  "name": "New SQL Server Database",
  "description": "New SQL Server database for testing",
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "NewDB",
    "username": "user",
    "password": "password"
  }
}
```

#### Response

```json
{
  "id": "ds-002",
  "name": "New SQL Server Database",
  "description": "New SQL Server database for testing",
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "NewDB",
    "username": "user",
    "password": "password"
  }
}
```

### Update Data Source

Updates an existing data source.

```
PUT /api/data-sources/{id}
```

#### Parameters

- `id` (path): The ID of the data source to update

#### Request Body

```json
{
  "name": "Updated SQL Server Database",
  "description": "Updated SQL Server database for testing",
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "UpdatedDB",
    "username": "user",
    "password": "password"
  }
}
```

#### Response

```json
{
  "id": "ds-001",
  "name": "Updated SQL Server Database",
  "description": "Updated SQL Server database for testing",
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "UpdatedDB",
    "username": "user",
    "password": "password"
  }
}
```

### Delete Data Source

Deletes a data source.

```
DELETE /api/data-sources/{id}
```

#### Parameters

- `id` (path): The ID of the data source to delete

#### Response

```
204 No Content
```

### Validate Connection

Validates the connection to a data source.

```
POST /api/data-sources/{id}/validate
```

#### Parameters

- `id` (path): The ID of the data source to validate

#### Response

```json
{
  "success": true,
  "message": "Connection is valid"
}
```

### Infer Schema

Infers the schema from a data source.

```
POST /api/data-sources/{id}/infer-schema
```

#### Parameters

- `id` (path): The ID of the data source

#### Response

```json
{
  "id": "schema-001",
  "name": "Inferred Schema",
  "fields": [
    {
      "name": "id",
      "type": "Integer",
      "isRequired": true,
      "isPrimaryKey": true
    },
    {
      "name": "name",
      "type": "String",
      "isRequired": true,
      "maxLength": 100
    },
    {
      "name": "email",
      "type": "String",
      "isRequired": false,
      "maxLength": 255
    }
  ]
}
```

### Fetch Data

Fetches data from a data source.

```
POST /api/data-sources/{id}/fetch-data
```

#### Parameters

- `id` (path): The ID of the data source

#### Request Body (Optional)

```json
{
  "limit": 100,
  "offset": 0,
  "filter": {
    "name": "John"
  },
  "sort": [
    {
      "field": "id",
      "direction": "asc"
    }
  ]
}
```

#### Response

```json
[
  {
    "id": "rec-001",
    "schemaId": "schema-001",
    "sourceId": "ds-001",
    "data": {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com"
    },
    "metadata": {
      "source": "SQL Server",
      "timestamp": "2023-06-01T12:00:00Z"
    },
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z",
    "version": "1.0"
  },
  ...
]
```

## Connectors

### List Database Connectors

Returns a list of available database connectors.

```
GET /api/connectors/database
```

#### Response

```json
[
  {
    "type": "SqlServer",
    "displayName": "SQL Server",
    "description": "Connect to Microsoft SQL Server databases",
    "category": "Database",
    "requiredProperties": [
      {
        "name": "server",
        "displayName": "Server",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "database",
        "displayName": "Database",
        "type": "string",
        "isRequired": true
      },
      ...
    ]
  },
  {
    "type": "MySQL",
    "displayName": "MySQL",
    "description": "Connect to MySQL databases",
    "category": "Database",
    "requiredProperties": [...]
  },
  {
    "type": "PostgreSQL",
    "displayName": "PostgreSQL",
    "description": "Connect to PostgreSQL databases",
    "category": "Database",
    "requiredProperties": [...]
  }
]
```

### List File System Connectors

Returns a list of available file system connectors.

```
GET /api/connectors/file-system
```

#### Response

```json
[
  {
    "type": "LocalFileSystem",
    "displayName": "Local File System",
    "description": "Connect to local file system",
    "category": "FileSystem",
    "requiredProperties": [
      {
        "name": "basePath",
        "displayName": "Base Path",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "filePattern",
        "displayName": "File Pattern",
        "type": "string",
        "isRequired": false
      },
      ...
    ]
  },
  {
    "type": "SFTP",
    "displayName": "SFTP",
    "description": "Connect to SFTP servers",
    "category": "FileSystem",
    "requiredProperties": [...]
  }
]
```

### List Streaming Connectors

Returns a list of available streaming connectors.

```
GET /api/connectors/streaming
```

#### Response

```json
[
  {
    "type": "Kafka",
    "displayName": "Apache Kafka",
    "description": "Connect to Apache Kafka clusters",
    "category": "Streaming",
    "requiredProperties": [
      {
        "name": "bootstrapServers",
        "displayName": "Bootstrap Servers",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "topic",
        "displayName": "Topic",
        "type": "string",
        "isRequired": true
      },
      ...
    ]
  },
  {
    "type": "EventHubs",
    "displayName": "Azure Event Hubs",
    "description": "Connect to Azure Event Hubs",
    "category": "Streaming",
    "requiredProperties": [...]
  }
]
```

### List REST Connectors

Returns a list of available REST API connectors.

```
GET /api/connectors/rest
```

#### Response

```json
[
  {
    "type": "RestApi",
    "displayName": "REST API",
    "description": "Connect to REST APIs",
    "category": "Rest",
    "requiredProperties": [
      {
        "name": "baseUrl",
        "displayName": "Base URL",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "authType",
        "displayName": "Authentication Type",
        "type": "string",
        "isRequired": false
      },
      ...
    ]
  }
]
```

### Validate Connection

Validates a connection to a data source.

```
POST /api/connectors/validate
```

#### Request Body

```json
{
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "SampleDB",
    "username": "user",
    "password": "password"
  }
}
```

#### Response

```json
{
  "success": true,
  "message": "Connection is valid"
}
```

### Infer Schema

Infers the schema from a data source.

```
POST /api/connectors/infer-schema
```

#### Request Body

```json
{
  "type": "Database",
  "connectionProperties": {
    "provider": "sqlserver",
    "server": "localhost",
    "database": "SampleDB",
    "username": "user",
    "password": "password"
  }
}
```

#### Response

```json
{
  "id": "schema-001",
  "name": "Inferred Schema",
  "fields": [
    {
      "name": "id",
      "type": "Integer",
      "isRequired": true,
      "isPrimaryKey": true
    },
    {
      "name": "name",
      "type": "String",
      "isRequired": true,
      "maxLength": 100
    },
    {
      "name": "email",
      "type": "String",
      "isRequired": false,
      "maxLength": 255
    }
  ]
}
```

### Fetch Data

Fetches data from a data source.

```
POST /api/connectors/fetch-data
```

#### Request Body

```json
{
  "source": {
    "type": "Database",
    "connectionProperties": {
      "provider": "sqlserver",
      "server": "localhost",
      "database": "SampleDB",
      "username": "user",
      "password": "password"
    }
  },
  "parameters": {
    "query": "SELECT * FROM Customers",
    "limit": 100
  }
}
```

#### Response

```json
[
  {
    "id": "rec-001",
    "schemaId": null,
    "sourceId": null,
    "data": {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com"
    },
    "metadata": {
      "source": "SQL Server",
      "timestamp": "2023-06-01T12:00:00Z"
    },
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z",
    "version": "1.0"
  },
  ...
]
```

## Health Check

### Get Health Status

Returns the health status of the Ingestion Service.

```
GET /health
```

#### Response

```json
{
  "status": "Healthy",
  "service": "IngestionService",
  "version": "1.0.0.0",
  "timestamp": "2023-06-01T12:00:00Z"
}
```
