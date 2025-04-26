# API Gateway Documentation

The API Gateway serves as the central entry point for all API requests to the Generic Data Platform. It routes requests to the appropriate microservices and provides a unified interface for clients.

## Base URL

```
https://localhost:7080/api/gateway
```

## Authentication

All API requests require authentication using JWT (JSON Web Token). Include the token in the Authorization header:

```
Authorization: Bearer {your_jwt_token}
```

To obtain a token, use the `/api/auth/login` endpoint.

## Available Services

The API Gateway provides access to the following services:

- **Ingestion Service**: Data ingestion from various sources
- **ETL Service**: Data transformation, validation, and enrichment
- **Database Service**: Schema management and data storage
- **Storage Service**: File and object storage

## Endpoints

### Get Available Services

Returns a list of available services and their endpoints.

```
GET /api/gateway/services
```

#### Response

```json
{
  "ingestion": "http://localhost:5064",
  "storage": "http://localhost:5227",
  "database": "http://localhost:5099",
  "etl": "http://localhost:5064"
}
```

### Check Service Health

Checks the health status of a specific service.

```
GET /api/gateway/{service}/health
```

#### Parameters

- `service` (path): The name of the service to check (ingestion, storage, database, etl)

#### Response

```json
{
  "status": "Healthy",
  "service": "IngestionService",
  "version": "1.0.0.0",
  "timestamp": "2023-06-01T12:00:00Z"
}
```

### Proxy GET Request

Proxies a GET request to a specific service.

```
GET /api/gateway/{service}/{path}
```

#### Parameters

- `service` (path): The name of the service to route to
- `path` (path): The path to the endpoint on the target service
- Query parameters are passed through to the target service

#### Example

```
GET /api/gateway/ingestion/api/connectors/database
```

### Proxy POST Request

Proxies a POST request to a specific service.

```
POST /api/gateway/{service}/{path}
```

#### Parameters

- `service` (path): The name of the service to route to
- `path` (path): The path to the endpoint on the target service
- Request body is passed through to the target service

#### Example

```
POST /api/gateway/etl/api/transformers/transform
```

```json
{
  "transformerType": "Json",
  "input": [...],
  "configuration": {...}
}
```

### Proxy PUT Request

Proxies a PUT request to a specific service.

```
PUT /api/gateway/{service}/{path}
```

#### Parameters

- `service` (path): The name of the service to route to
- `path` (path): The path to the endpoint on the target service
- Request body is passed through to the target service

### Proxy DELETE Request

Proxies a DELETE request to a specific service.

```
DELETE /api/gateway/{service}/{path}
```

#### Parameters

- `service` (path): The name of the service to route to
- `path` (path): The path to the endpoint on the target service

## Error Handling

The API Gateway returns standard HTTP status codes:

- `200 OK`: The request was successful
- `400 Bad Request`: The request was invalid
- `401 Unauthorized`: Authentication is required
- `403 Forbidden`: The authenticated user doesn't have permission
- `404 Not Found`: The requested resource was not found
- `500 Internal Server Error`: An error occurred on the server

Error responses include a JSON object with details:

```json
{
  "error": "Error message",
  "details": "Additional details about the error"
}
```
