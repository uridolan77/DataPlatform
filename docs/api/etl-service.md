# ETL Service API Documentation

The ETL (Extract, Transform, Load) Service provides APIs for transforming, validating, enriching data, and creating complex ETL workflows.

## Base URL

```
https://localhost:7084/api
```

## Authentication

All API requests require authentication using JWT (JSON Web Token). Include the token in the Authorization header:

```
Authorization: Bearer {your_jwt_token}
```

## Transformers

### List Transformers

Returns a list of available transformers.

```
GET /api/transformers
```

#### Response

```json
[
  {
    "type": "Json",
    "displayName": "JSON Transformer",
    "description": "Transform JSON data with filtering, mapping, and aggregation operations",
    "supportedOperations": [
      {
        "name": "filter",
        "displayName": "Filter",
        "description": "Filter records based on conditions",
        "parameters": [
          {
            "name": "filterConditions",
            "displayName": "Filter Conditions",
            "type": "object",
            "isRequired": true
          }
        ]
      },
      {
        "name": "map",
        "displayName": "Map Fields",
        "description": "Map fields from source to target",
        "parameters": [...]
      },
      ...
    ]
  },
  {
    "type": "Csv",
    "displayName": "CSV Transformer",
    "description": "Transform CSV data with parsing, filtering, mapping, and aggregation operations",
    "supportedOperations": [...]
  },
  {
    "type": "Xml",
    "displayName": "XML Transformer",
    "description": "Transform XML data with XPath support for handling complex XML structures",
    "supportedOperations": [...]
  }
]
```

### Get Transformer

Returns details of a specific transformer.

```
GET /api/transformers/{type}
```

#### Parameters

- `type` (path): The type of the transformer (e.g., Json, Csv, Xml)

#### Response

```json
{
  "type": "Json",
  "displayName": "JSON Transformer",
  "description": "Transform JSON data with filtering, mapping, and aggregation operations",
  "supportedOperations": [
    {
      "name": "filter",
      "displayName": "Filter",
      "description": "Filter records based on conditions",
      "parameters": [
        {
          "name": "filterConditions",
          "displayName": "Filter Conditions",
          "type": "object",
          "isRequired": true
        }
      ]
    },
    {
      "name": "map",
      "displayName": "Map Fields",
      "description": "Map fields from source to target",
      "parameters": [...]
    },
    ...
  ]
}
```

### Transform Data

Transforms data using a specified transformer.

```
POST /api/transformers/transform
```

#### Request Body

```json
{
  "transformerType": "Json",
  "input": [
    {
      "id": "rec-001",
      "data": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com",
        "age": 30
      }
    },
    {
      "id": "rec-002",
      "data": {
        "id": 2,
        "name": "Jane Smith",
        "email": "jane@example.com",
        "age": 25
      }
    }
  ],
  "configuration": {
    "transformationType": "filter",
    "filterConditions": {
      "age": ">25"
    }
  },
  "source": {
    "id": "ds-001",
    "type": "Database"
  }
}
```

#### Response

```json
[
  {
    "id": "rec-001",
    "data": {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com",
      "age": 30
    }
  }
]
```

## Validators

### List Validators

Returns a list of available validators.

```
GET /api/validators
```

#### Response

```json
[
  {
    "type": "Schema",
    "displayName": "Schema Validator",
    "description": "Validate data against schema definitions with type checking, required fields, and custom validation rules",
    "supportedRules": [
      {
        "name": "typeCheck",
        "displayName": "Type Check",
        "description": "Validate field types against schema",
        "parameters": [
          {
            "name": "schema",
            "displayName": "Schema",
            "type": "object",
            "isRequired": true
          }
        ]
      },
      {
        "name": "requiredFields",
        "displayName": "Required Fields",
        "description": "Validate required fields are present and non-null",
        "parameters": [...]
      },
      ...
    ]
  },
  {
    "type": "DataQuality",
    "displayName": "Data Quality Validator",
    "description": "Validate data quality with checks for nulls, patterns, ranges, uniqueness, and more",
    "supportedRules": [...]
  }
]
```

### Get Validator

Returns details of a specific validator.

```
GET /api/validators/{type}
```

#### Parameters

- `type` (path): The type of the validator (e.g., Schema, DataQuality)

#### Response

```json
{
  "type": "Schema",
  "displayName": "Schema Validator",
  "description": "Validate data against schema definitions with type checking, required fields, and custom validation rules",
  "supportedRules": [
    {
      "name": "typeCheck",
      "displayName": "Type Check",
      "description": "Validate field types against schema",
      "parameters": [
        {
          "name": "schema",
          "displayName": "Schema",
          "type": "object",
          "isRequired": true
        }
      ]
    },
    {
      "name": "requiredFields",
      "displayName": "Required Fields",
      "description": "Validate required fields are present and non-null",
      "parameters": [...]
    },
    ...
  ]
}
```

### Validate Data

Validates data using a specified validator.

```
POST /api/validators/validate
```

#### Request Body

```json
{
  "validatorType": "Schema",
  "input": [
    {
      "id": "rec-001",
      "data": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com"
      }
    },
    {
      "id": "rec-002",
      "data": {
        "id": 2,
        "name": null,
        "email": "invalid-email"
      }
    }
  ],
  "configuration": {
    "schema": {
      "fields": [
        {
          "name": "id",
          "type": "Integer",
          "isRequired": true
        },
        {
          "name": "name",
          "type": "String",
          "isRequired": true
        },
        {
          "name": "email",
          "type": "String",
          "isRequired": false,
          "validation": {
            "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
          }
        }
      ]
    },
    "failOnError": false
  },
  "source": {
    "id": "ds-001",
    "type": "Database"
  }
}
```

#### Response

```json
{
  "isValid": false,
  "validRecords": [
    {
      "id": "rec-001",
      "data": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com"
      }
    }
  ],
  "invalidRecords": [
    {
      "id": "rec-002",
      "data": {
        "id": 2,
        "name": null,
        "email": "invalid-email"
      }
    }
  ],
  "errors": [
    {
      "recordId": "rec-002",
      "fieldName": "name",
      "errorType": "NullValue",
      "message": "Required field 'name' has a null value"
    },
    {
      "recordId": "rec-002",
      "fieldName": "email",
      "errorType": "ValidationRuleViolation",
      "message": "Field 'email' does not match the required pattern"
    }
  ],
  "validationTime": "2023-06-01T12:00:00Z"
}
```

## Enrichers

### List Enrichers

Returns a list of available enrichers.

```
GET /api/enrichers
```

#### Response

```json
[
  {
    "type": "Data",
    "displayName": "Data Enricher",
    "description": "Enrich data with derived fields, transformations, and calculated values",
    "supportedRules": [
      {
        "name": "derived",
        "displayName": "Derived Field",
        "description": "Create a derived field based on an expression",
        "parameters": [
          {
            "name": "targetField",
            "displayName": "Target Field",
            "type": "string",
            "isRequired": true
          },
          {
            "name": "expression",
            "displayName": "Expression",
            "type": "string",
            "isRequired": true
          }
        ]
      },
      {
        "name": "transform",
        "displayName": "Transform",
        "description": "Apply transformations to field values",
        "parameters": [...]
      },
      ...
    ]
  },
  {
    "type": "Lookup",
    "displayName": "Lookup Enricher",
    "description": "Enrich data with values from reference datasets",
    "supportedRules": [...]
  }
]
```

### Get Enricher

Returns details of a specific enricher.

```
GET /api/enrichers/{type}
```

#### Parameters

- `type` (path): The type of the enricher (e.g., Data, Lookup)

#### Response

```json
{
  "type": "Data",
  "displayName": "Data Enricher",
  "description": "Enrich data with derived fields, transformations, and calculated values",
  "supportedRules": [
    {
      "name": "derived",
      "displayName": "Derived Field",
      "description": "Create a derived field based on an expression",
      "parameters": [
        {
          "name": "targetField",
          "displayName": "Target Field",
          "type": "string",
          "isRequired": true
        },
        {
          "name": "expression",
          "displayName": "Expression",
          "type": "string",
          "isRequired": true
        }
      ]
    },
    {
      "name": "transform",
      "displayName": "Transform",
      "description": "Apply transformations to field values",
      "parameters": [...]
    },
    ...
  ]
}
```

### Enrich Data

Enriches data using a specified enricher.

```
POST /api/enrichers/enrich
```

#### Request Body

```json
{
  "enricherType": "Data",
  "input": [
    {
      "id": "rec-001",
      "data": {
        "firstName": "John",
        "lastName": "Doe",
        "birthDate": "1990-01-01"
      }
    },
    {
      "id": "rec-002",
      "data": {
        "firstName": "Jane",
        "lastName": "Smith",
        "birthDate": "1995-05-15"
      }
    }
  ],
  "configuration": {
    "rules": [
      {
        "type": "derived",
        "targetField": "fullName",
        "parameters": {
          "expression": "$firstName + ' ' + $lastName"
        }
      },
      {
        "type": "derived",
        "targetField": "age",
        "parameters": {
          "expression": "YEAR_DIFF($birthDate, CURRENT_DATE())"
        }
      }
    ]
  },
  "source": {
    "id": "ds-001",
    "type": "Database"
  }
}
```

#### Response

```json
[
  {
    "id": "rec-001",
    "data": {
      "firstName": "John",
      "lastName": "Doe",
      "birthDate": "1990-01-01",
      "fullName": "John Doe",
      "age": 33
    },
    "metadata": {
      "enriched": "true",
      "enrichmentTime": "2023-06-01T12:00:00Z"
    }
  },
  {
    "id": "rec-002",
    "data": {
      "firstName": "Jane",
      "lastName": "Smith",
      "birthDate": "1995-05-15",
      "fullName": "Jane Smith",
      "age": 28
    },
    "metadata": {
      "enriched": "true",
      "enrichmentTime": "2023-06-01T12:00:00Z"
    }
  }
]
```

## Workflows

### List Workflows

Returns a list of all workflows.

```
GET /api/workflows
```

#### Response

```json
[
  {
    "id": "wf-001",
    "name": "Customer Data Processing",
    "description": "Process customer data from CSV files",
    "steps": [...],
    "parameters": {...},
    "triggers": [...],
    "errorHandling": {...},
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z",
    "version": "1.0"
  },
  ...
]
```

### Get Workflow

Returns details of a specific workflow.

```
GET /api/workflows/{id}
```

#### Parameters

- `id` (path): The ID of the workflow

#### Response

```json
{
  "id": "wf-001",
  "name": "Customer Data Processing",
  "description": "Process customer data from CSV files",
  "steps": [
    {
      "id": "step-001",
      "name": "Extract CSV Data",
      "description": "Extract data from CSV file",
      "type": "Extract",
      "configuration": {
        "extractorType": "FileSystem",
        "filePath": "/data/customers.csv",
        "fileFormat": "csv",
        "hasHeader": true
      },
      "dependsOn": [],
      "errorHandling": {
        "onError": "StopWorkflow"
      }
    },
    {
      "id": "step-002",
      "name": "Validate Data",
      "description": "Validate customer data",
      "type": "Validate",
      "configuration": {
        "validatorType": "Schema",
        "schema": {...}
      },
      "dependsOn": ["step-001"],
      "errorHandling": {
        "onError": "ContinueWorkflow"
      }
    },
    ...
  ],
  "parameters": {
    "inputPath": "/data",
    "outputPath": "/processed"
  },
  "triggers": [
    {
      "id": "trigger-001",
      "name": "Daily Schedule",
      "type": "Schedule",
      "configuration": {
        "schedule": "0 0 * * *"
      }
    }
  ],
  "errorHandling": {
    "defaultAction": "StopWorkflow",
    "maxErrors": 10,
    "notificationTargets": ["admin@example.com"]
  },
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z",
  "version": "1.0"
}
```

### Create Workflow

Creates a new workflow.

```
POST /api/workflows
```

#### Request Body

```json
{
  "name": "New Customer Data Processing",
  "description": "Process new customer data from CSV files",
  "steps": [
    {
      "id": "step-001",
      "name": "Extract CSV Data",
      "description": "Extract data from CSV file",
      "type": "Extract",
      "configuration": {
        "extractorType": "FileSystem",
        "filePath": "/data/new-customers.csv",
        "fileFormat": "csv",
        "hasHeader": true
      },
      "dependsOn": [],
      "errorHandling": {
        "onError": "StopWorkflow"
      }
    },
    ...
  ],
  "parameters": {
    "inputPath": "/data",
    "outputPath": "/processed"
  },
  "triggers": [
    {
      "id": "trigger-001",
      "name": "Daily Schedule",
      "type": "Schedule",
      "configuration": {
        "schedule": "0 0 * * *"
      }
    }
  ],
  "errorHandling": {
    "defaultAction": "StopWorkflow",
    "maxErrors": 10,
    "notificationTargets": ["admin@example.com"]
  }
}
```

#### Response

```json
{
  "id": "wf-002",
  "name": "New Customer Data Processing",
  "description": "Process new customer data from CSV files",
  "steps": [...],
  "parameters": {...},
  "triggers": [...],
  "errorHandling": {...},
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z",
  "version": "1.0"
}
```

### Update Workflow

Updates an existing workflow.

```
PUT /api/workflows/{id}
```

#### Parameters

- `id` (path): The ID of the workflow to update

#### Request Body

```json
{
  "name": "Updated Customer Data Processing",
  "description": "Process updated customer data from CSV files",
  "steps": [...],
  "parameters": {...},
  "triggers": [...],
  "errorHandling": {...}
}
```

#### Response

```json
{
  "id": "wf-001",
  "name": "Updated Customer Data Processing",
  "description": "Process updated customer data from CSV files",
  "steps": [...],
  "parameters": {...},
  "triggers": [...],
  "errorHandling": {...},
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T13:00:00Z",
  "version": "1.1"
}
```

### Delete Workflow

Deletes a workflow.

```
DELETE /api/workflows/{id}
```

#### Parameters

- `id` (path): The ID of the workflow to delete

#### Response

```
204 No Content
```

### Execute Workflow

Executes a workflow.

```
POST /api/workflows/{id}/execute
```

#### Parameters

- `id` (path): The ID of the workflow to execute

#### Request Body (Optional)

```json
{
  "inputPath": "/data/special",
  "outputPath": "/processed/special"
}
```

#### Response

```json
{
  "id": "exec-001",
  "workflowId": "wf-001",
  "status": "Running",
  "startTime": "2023-06-01T12:00:00Z",
  "parameters": {
    "inputPath": "/data/special",
    "outputPath": "/processed/special"
  },
  "stepExecutions": []
}
```

### Get Execution Status

Returns the status of a workflow execution.

```
GET /api/workflows/executions/{executionId}
```

#### Parameters

- `executionId` (path): The ID of the execution

#### Response

```json
{
  "id": "exec-001",
  "workflowId": "wf-001",
  "status": "Completed",
  "startTime": "2023-06-01T12:00:00Z",
  "endTime": "2023-06-01T12:05:00Z",
  "parameters": {
    "inputPath": "/data/special",
    "outputPath": "/processed/special"
  },
  "output": {
    "recordsProcessed": 1000,
    "recordsRejected": 5
  },
  "stepExecutions": [
    {
      "id": "step-exec-001",
      "stepId": "step-001",
      "status": "Completed",
      "startTime": "2023-06-01T12:00:00Z",
      "endTime": "2023-06-01T12:01:00Z",
      "input": {...},
      "output": {...}
    },
    ...
  ],
  "errors": []
}
```

### Cancel Execution

Cancels a running workflow execution.

```
POST /api/workflows/executions/{executionId}/cancel
```

#### Parameters

- `executionId` (path): The ID of the execution to cancel

#### Response

```json
{
  "message": "Execution cancelled successfully"
}
```

### Get Execution History

Returns the execution history of a workflow.

```
GET /api/workflows/{id}/history
```

#### Parameters

- `id` (path): The ID of the workflow
- `limit` (query, optional): Maximum number of executions to return (default: 10)

#### Response

```json
[
  {
    "id": "exec-001",
    "workflowId": "wf-001",
    "status": "Completed",
    "startTime": "2023-06-01T12:00:00Z",
    "endTime": "2023-06-01T12:05:00Z",
    "parameters": {...},
    "output": {...}
  },
  {
    "id": "exec-002",
    "workflowId": "wf-001",
    "status": "Failed",
    "startTime": "2023-06-02T12:00:00Z",
    "endTime": "2023-06-02T12:01:00Z",
    "parameters": {...},
    "errors": [...]
  },
  ...
]
```

## Pipelines (Legacy)

### List Pipelines

Returns a list of all pipelines.

```
GET /api/pipelines
```

#### Response

```json
[
  {
    "id": "pipe-001",
    "name": "Customer Data Pipeline",
    "description": "Process customer data",
    "stages": [...],
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z"
  },
  ...
]
```

### Get Pipeline

Returns details of a specific pipeline.

```
GET /api/pipelines/{id}
```

#### Parameters

- `id` (path): The ID of the pipeline

#### Response

```json
{
  "id": "pipe-001",
  "name": "Customer Data Pipeline",
  "description": "Process customer data",
  "stages": [
    {
      "id": "stage-001",
      "name": "Extract",
      "type": "Extract",
      "configuration": {
        "source": {
          "type": "Database",
          "connectionProperties": {...}
        }
      }
    },
    {
      "id": "stage-002",
      "name": "Transform",
      "type": "Transform",
      "configuration": {
        "transformerType": "Json",
        "transformationType": "map",
        "fieldMappings": {...}
      }
    },
    {
      "id": "stage-003",
      "name": "Load",
      "type": "Load",
      "configuration": {
        "target": {
          "type": "Database",
          "connectionProperties": {...}
        }
      }
    }
  ],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z"
}
```

### Create Pipeline

Creates a new pipeline.

```
POST /api/pipelines
```

#### Request Body

```json
{
  "name": "New Customer Data Pipeline",
  "description": "Process new customer data",
  "stages": [
    {
      "name": "Extract",
      "type": "Extract",
      "configuration": {
        "source": {
          "type": "Database",
          "connectionProperties": {...}
        }
      }
    },
    ...
  ]
}
```

#### Response

```json
{
  "id": "pipe-002",
  "name": "New Customer Data Pipeline",
  "description": "Process new customer data",
  "stages": [...],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z"
}
```

### Update Pipeline

Updates an existing pipeline.

```
PUT /api/pipelines/{id}
```

#### Parameters

- `id` (path): The ID of the pipeline to update

#### Request Body

```json
{
  "name": "Updated Customer Data Pipeline",
  "description": "Process updated customer data",
  "stages": [...]
}
```

#### Response

```json
{
  "id": "pipe-001",
  "name": "Updated Customer Data Pipeline",
  "description": "Process updated customer data",
  "stages": [...],
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T13:00:00Z"
}
```

### Delete Pipeline

Deletes a pipeline.

```
DELETE /api/pipelines/{id}
```

#### Parameters

- `id` (path): The ID of the pipeline to delete

#### Response

```
204 No Content
```

### Execute Pipeline

Executes a pipeline.

```
POST /api/pipelines/{id}/execute
```

#### Parameters

- `id` (path): The ID of the pipeline to execute

#### Request Body (Optional)

```json
{
  "parameters": {
    "limit": 1000,
    "filter": {
      "country": "US"
    }
  }
}
```

#### Response

```json
{
  "id": "exec-001",
  "pipelineId": "pipe-001",
  "status": "Completed",
  "startTime": "2023-06-01T12:00:00Z",
  "endTime": "2023-06-01T12:05:00Z",
  "results": {
    "recordsProcessed": 1000,
    "recordsRejected": 5,
    "stageResults": [...]
  }
}
```

## Health Check

### Get Health Status

Returns the health status of the ETL Service.

```
GET /health
```

#### Response

```json
{
  "status": "Healthy",
  "service": "ETLService",
  "version": "1.0.0.0",
  "timestamp": "2023-06-01T12:00:00Z"
}
```
