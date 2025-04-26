# Database Service API Documentation

The Database Service provides APIs for managing database schemas, tables, and data, as well as schema evolution capabilities.

## Base URL

```
https://localhost:7084/api
```

## Authentication

All API requests require authentication using JWT (JSON Web Token). Include the token in the Authorization header:

```
Authorization: Bearer {your_jwt_token}
```

## Schemas

### List Schemas

Returns a list of all schemas.

```
GET /api/schemas
```

#### Response

```json
[
  {
    "id": "schema-001",
    "name": "Customers",
    "description": "Customer data schema",
    "fields": [...],
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z",
    "version": "1.0"
  },
  ...
]
```

### Get Schema

Returns details of a specific schema.

```
GET /api/schemas/{id}
```

#### Parameters

- `id` (path): The ID of the schema

#### Response

```json
{
  "id": "schema-001",
  "name": "Customers",
  "description": "Customer data schema",
  "fields": [
    {
      "name": "id",
      "type": "Integer",
      "isRequired": true,
      "isPrimaryKey": true,
      "description": "Customer ID"
    },
    {
      "name": "name",
      "type": "String",
      "isRequired": true,
      "maxLength": 100,
      "description": "Customer name"
    },
    {
      "name": "email",
      "type": "String",
      "isRequired": false,
      "maxLength": 255,
      "description": "Customer email",
      "validation": {
        "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
      }
    },
    {
      "name": "age",
      "type": "Integer",
      "isRequired": false,
      "description": "Customer age",
      "validation": {
        "minimum": 0,
        "maximum": 120
      }
    }
  ],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z",
  "version": "1.0"
}
```

### Create Schema

Creates a new schema.

```
POST /api/schemas
```

#### Request Body

```json
{
  "name": "Products",
  "description": "Product data schema",
  "fields": [
    {
      "name": "id",
      "type": "Integer",
      "isRequired": true,
      "isPrimaryKey": true,
      "description": "Product ID"
    },
    {
      "name": "name",
      "type": "String",
      "isRequired": true,
      "maxLength": 100,
      "description": "Product name"
    },
    {
      "name": "price",
      "type": "Decimal",
      "isRequired": true,
      "description": "Product price",
      "validation": {
        "minimum": 0
      }
    },
    {
      "name": "category",
      "type": "String",
      "isRequired": false,
      "maxLength": 50,
      "description": "Product category"
    }
  ]
}
```

#### Response

```json
{
  "id": "schema-002",
  "name": "Products",
  "description": "Product data schema",
  "fields": [...],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z",
  "version": "1.0"
}
```

### Update Schema

Updates an existing schema.

```
PUT /api/schemas/{id}
```

#### Parameters

- `id` (path): The ID of the schema to update

#### Request Body

```json
{
  "name": "Updated Products",
  "description": "Updated product data schema",
  "fields": [
    {
      "name": "id",
      "type": "Integer",
      "isRequired": true,
      "isPrimaryKey": true,
      "description": "Product ID"
    },
    {
      "name": "name",
      "type": "String",
      "isRequired": true,
      "maxLength": 150,
      "description": "Product name"
    },
    {
      "name": "price",
      "type": "Decimal",
      "isRequired": true,
      "description": "Product price",
      "validation": {
        "minimum": 0
      }
    },
    {
      "name": "category",
      "type": "String",
      "isRequired": false,
      "maxLength": 50,
      "description": "Product category"
    },
    {
      "name": "description",
      "type": "String",
      "isRequired": false,
      "maxLength": 1000,
      "description": "Product description"
    }
  ]
}
```

#### Response

```json
{
  "id": "schema-002",
  "name": "Updated Products",
  "description": "Updated product data schema",
  "fields": [...],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T13:00:00Z",
  "version": "1.1"
}
```

### Delete Schema

Deletes a schema.

```
DELETE /api/schemas/{id}
```

#### Parameters

- `id` (path): The ID of the schema to delete

#### Response

```
204 No Content
```

### Get Schema Versions

Returns all versions of a schema.

```
GET /api/schemas/{id}/versions
```

#### Parameters

- `id` (path): The ID of the schema

#### Response

```json
[
  {
    "id": "schema-002",
    "name": "Products",
    "description": "Product data schema",
    "fields": [...],
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z",
    "version": "1.0"
  },
  {
    "id": "schema-002",
    "name": "Updated Products",
    "description": "Updated product data schema",
    "fields": [...],
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T13:00:00Z",
    "version": "1.1"
  }
]
```

### Get Schema Version

Returns a specific version of a schema.

```
GET /api/schemas/{id}/versions/{version}
```

#### Parameters

- `id` (path): The ID of the schema
- `version` (path): The version of the schema

#### Response

```json
{
  "id": "schema-002",
  "name": "Products",
  "description": "Product data schema",
  "fields": [...],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z",
  "version": "1.0"
}
```

## Schema Evolution

### Compare Schemas

Compares two schemas and returns the differences.

```
POST /api/schema-evolution/compare
```

#### Request Body

```json
{
  "sourceSchema": {
    "id": "schema-002",
    "name": "Products",
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
        "name": "price",
        "type": "Decimal",
        "isRequired": true
      }
    ]
  },
  "targetSchema": {
    "id": "schema-002",
    "name": "Updated Products",
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
        "maxLength": 150
      },
      {
        "name": "price",
        "type": "Decimal",
        "isRequired": true
      },
      {
        "name": "description",
        "type": "String",
        "isRequired": false,
        "maxLength": 1000
      }
    ]
  }
}
```

#### Response

```json
{
  "changes": [
    {
      "type": "FieldModified",
      "field": "name",
      "property": "maxLength",
      "oldValue": 100,
      "newValue": 150,
      "impact": "Low"
    },
    {
      "type": "FieldAdded",
      "field": "description",
      "impact": "Low"
    }
  ],
  "impactLevel": "Low",
  "isCompatible": true
}
```

### Generate Migration Plan

Generates a migration plan for evolving a schema.

```
POST /api/schema-evolution/generate-migration-plan
```

#### Request Body

```json
{
  "sourceSchema": {
    "id": "schema-002",
    "name": "Products",
    "fields": [...]
  },
  "targetSchema": {
    "id": "schema-002",
    "name": "Updated Products",
    "fields": [...]
  },
  "databaseType": "PostgreSQL",
  "options": {
    "preserveData": true,
    "validateBeforeMigration": true
  }
}
```

#### Response

```json
{
  "steps": [
    {
      "order": 1,
      "type": "AlterColumn",
      "table": "Products",
      "column": "name",
      "sql": "ALTER TABLE Products ALTER COLUMN name TYPE VARCHAR(150);",
      "description": "Increase the maximum length of the name column from 100 to 150"
    },
    {
      "order": 2,
      "type": "AddColumn",
      "table": "Products",
      "column": "description",
      "sql": "ALTER TABLE Products ADD COLUMN description VARCHAR(1000);",
      "description": "Add the description column"
    }
  ],
  "warnings": [],
  "estimatedDuration": "00:00:05"
}
```

### Execute Migration Plan

Executes a migration plan.

```
POST /api/schema-evolution/execute-migration-plan
```

#### Request Body

```json
{
  "migrationPlan": {
    "steps": [
      {
        "order": 1,
        "type": "AlterColumn",
        "table": "Products",
        "column": "name",
        "sql": "ALTER TABLE Products ALTER COLUMN name TYPE VARCHAR(150);",
        "description": "Increase the maximum length of the name column from 100 to 150"
      },
      {
        "order": 2,
        "type": "AddColumn",
        "table": "Products",
        "column": "description",
        "sql": "ALTER TABLE Products ADD COLUMN description VARCHAR(1000);",
        "description": "Add the description column"
      }
    ]
  },
  "connectionProperties": {
    "provider": "postgresql",
    "host": "localhost",
    "port": 5432,
    "database": "genericdataplatform",
    "username": "postgres",
    "password": "postgres"
  },
  "options": {
    "dryRun": false,
    "rollbackOnError": true
  }
}
```

#### Response

```json
{
  "success": true,
  "executedSteps": [
    {
      "order": 1,
      "type": "AlterColumn",
      "table": "Products",
      "column": "name",
      "sql": "ALTER TABLE Products ALTER COLUMN name TYPE VARCHAR(150);",
      "description": "Increase the maximum length of the name column from 100 to 150",
      "success": true,
      "duration": "00:00:01"
    },
    {
      "order": 2,
      "type": "AddColumn",
      "table": "Products",
      "column": "description",
      "sql": "ALTER TABLE Products ADD COLUMN description VARCHAR(1000);",
      "description": "Add the description column",
      "success": true,
      "duration": "00:00:01"
    }
  ],
  "duration": "00:00:02",
  "errors": []
}
```

### Validate Schema Compatibility

Validates if a schema is compatible with a database.

```
POST /api/schema-evolution/validate-compatibility
```

#### Request Body

```json
{
  "schema": {
    "id": "schema-002",
    "name": "Products",
    "fields": [...]
  },
  "connectionProperties": {
    "provider": "postgresql",
    "host": "localhost",
    "port": 5432,
    "database": "genericdataplatform",
    "username": "postgres",
    "password": "postgres"
  }
}
```

#### Response

```json
{
  "isCompatible": true,
  "differences": [
    {
      "type": "FieldModified",
      "field": "name",
      "property": "maxLength",
      "databaseValue": 100,
      "schemaValue": 150,
      "impact": "Low"
    }
  ],
  "warnings": []
}
```

## Tables

### List Tables

Returns a list of all tables in a database.

```
GET /api/tables
```

#### Query Parameters

- `provider` (query): The database provider (e.g., postgresql, sqlserver, mysql)
- `host` (query): The database host
- `port` (query, optional): The database port
- `database` (query): The database name
- `username` (query): The database username
- `password` (query): The database password

#### Response

```json
[
  {
    "name": "Customers",
    "schema": "public",
    "columns": [
      {
        "name": "id",
        "type": "integer",
        "isNullable": false,
        "isPrimaryKey": true
      },
      {
        "name": "name",
        "type": "character varying",
        "length": 100,
        "isNullable": false,
        "isPrimaryKey": false
      },
      {
        "name": "email",
        "type": "character varying",
        "length": 255,
        "isNullable": true,
        "isPrimaryKey": false
      }
    ],
    "rowCount": 1000
  },
  ...
]
```

### Get Table

Returns details of a specific table.

```
GET /api/tables/{name}
```

#### Parameters

- `name` (path): The name of the table

#### Query Parameters

- `provider` (query): The database provider (e.g., postgresql, sqlserver, mysql)
- `host` (query): The database host
- `port` (query, optional): The database port
- `database` (query): The database name
- `username` (query): The database username
- `password` (query): The database password
- `schema` (query, optional): The database schema (default: public for PostgreSQL, dbo for SQL Server)

#### Response

```json
{
  "name": "Customers",
  "schema": "public",
  "columns": [
    {
      "name": "id",
      "type": "integer",
      "isNullable": false,
      "isPrimaryKey": true
    },
    {
      "name": "name",
      "type": "character varying",
      "length": 100,
      "isNullable": false,
      "isPrimaryKey": false
    },
    {
      "name": "email",
      "type": "character varying",
      "length": 255,
      "isNullable": true,
      "isPrimaryKey": false
    }
  ],
  "indexes": [
    {
      "name": "pk_customers",
      "columns": ["id"],
      "isUnique": true,
      "isPrimary": true
    },
    {
      "name": "idx_customers_email",
      "columns": ["email"],
      "isUnique": true,
      "isPrimary": false
    }
  ],
  "foreignKeys": [],
  "rowCount": 1000
}
```

### Create Table

Creates a new table.

```
POST /api/tables
```

#### Request Body

```json
{
  "name": "Products",
  "schema": "public",
  "columns": [
    {
      "name": "id",
      "type": "integer",
      "isNullable": false,
      "isPrimaryKey": true
    },
    {
      "name": "name",
      "type": "character varying",
      "length": 100,
      "isNullable": false
    },
    {
      "name": "price",
      "type": "numeric",
      "precision": 10,
      "scale": 2,
      "isNullable": false
    },
    {
      "name": "category",
      "type": "character varying",
      "length": 50,
      "isNullable": true
    }
  ],
  "indexes": [
    {
      "name": "idx_products_category",
      "columns": ["category"],
      "isUnique": false
    }
  ],
  "connectionProperties": {
    "provider": "postgresql",
    "host": "localhost",
    "port": 5432,
    "database": "genericdataplatform",
    "username": "postgres",
    "password": "postgres"
  }
}
```

#### Response

```json
{
  "success": true,
  "message": "Table 'Products' created successfully",
  "sql": "CREATE TABLE public.Products (...)"
}
```

### Drop Table

Drops a table.

```
DELETE /api/tables/{name}
```

#### Parameters

- `name` (path): The name of the table to drop

#### Query Parameters

- `provider` (query): The database provider (e.g., postgresql, sqlserver, mysql)
- `host` (query): The database host
- `port` (query, optional): The database port
- `database` (query): The database name
- `username` (query): The database username
- `password` (query): The database password
- `schema` (query, optional): The database schema (default: public for PostgreSQL, dbo for SQL Server)

#### Response

```json
{
  "success": true,
  "message": "Table 'Products' dropped successfully"
}
```

### Query Table

Queries a table.

```
POST /api/tables/{name}/query
```

#### Parameters

- `name` (path): The name of the table to query

#### Request Body

```json
{
  "columns": ["id", "name", "price"],
  "filter": {
    "price": ">100"
  },
  "sort": [
    {
      "column": "price",
      "direction": "desc"
    }
  ],
  "limit": 10,
  "offset": 0,
  "connectionProperties": {
    "provider": "postgresql",
    "host": "localhost",
    "port": 5432,
    "database": "genericdataplatform",
    "username": "postgres",
    "password": "postgres"
  }
}
```

#### Response

```json
{
  "columns": ["id", "name", "price"],
  "rows": [
    {
      "id": 5,
      "name": "Premium Widget",
      "price": 199.99
    },
    {
      "id": 3,
      "name": "Deluxe Gadget",
      "price": 149.99
    },
    {
      "id": 7,
      "name": "Super Gizmo",
      "price": 129.99
    }
  ],
  "totalRows": 3,
  "executionTime": "00:00:00.123"
}
```

## Health Check

### Get Health Status

Returns the health status of the Database Service.

```
GET /health
```

#### Response

```json
{
  "status": "Healthy",
  "service": "DatabaseService",
  "version": "1.0.0.0",
  "timestamp": "2023-06-01T12:00:00Z"
}
```
