# Generic Data Platform API Documentation

Welcome to the Generic Data Platform API documentation. This documentation provides comprehensive information about the APIs available in the Generic Data Platform.

## Overview

The Generic Data Platform is a flexible and extensible platform for ingesting, processing, storing, and analyzing data from various sources. It provides a set of microservices that work together to provide a complete data platform solution.

## Services

The Generic Data Platform consists of the following microservices:

- [API Gateway](api-gateway.md): The central entry point for all API requests
- [Ingestion Service](ingestion-service.md): For connecting to and ingesting data from various sources
- [ETL Service](etl-service.md): For transforming, validating, and enriching data
- [Database Service](database-service.md): For managing database schemas and data
- [Storage Service](storage-service.md): For storing and retrieving files and objects

## Authentication

All API requests require authentication using JWT (JSON Web Token). Include the token in the Authorization header:

```
Authorization: Bearer {your_jwt_token}
```

To obtain a token, use the `/api/auth/login` endpoint.

## Common Response Formats

All API responses follow a consistent format:

### Success Response

```json
{
  "data": {
    // Response data
  },
  "metadata": {
    // Optional metadata
  }
}
```

### Error Response

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Error message",
    "details": {
      // Optional error details
    }
  }
}
```

## Common HTTP Status Codes

- `200 OK`: The request was successful
- `201 Created`: The resource was successfully created
- `204 No Content`: The request was successful, but there is no content to return
- `400 Bad Request`: The request was invalid
- `401 Unauthorized`: Authentication is required
- `403 Forbidden`: The authenticated user doesn't have permission
- `404 Not Found`: The requested resource was not found
- `409 Conflict`: The request conflicts with the current state of the resource
- `500 Internal Server Error`: An error occurred on the server

## Rate Limiting

The API implements rate limiting to prevent abuse. The rate limits are as follows:

- 100 requests per minute per user
- 1000 requests per hour per user

When a rate limit is exceeded, the API returns a `429 Too Many Requests` status code.

## Pagination

Many endpoints that return collections support pagination. The pagination parameters are:

- `pageSize`: The number of items to return per page (default: 100, max: 1000)
- `pageToken`: The token for the next page of results

The response includes a `nextPageToken` field that can be used to retrieve the next page of results.

## Versioning

The API is versioned using the URL path. The current version is `v1`.

Example:

```
https://api.genericdataplatform.com/v1/api/...
```

## Getting Started

To get started with the Generic Data Platform API, follow these steps:

1. Obtain an API token by authenticating with the `/api/auth/login` endpoint
2. Use the API Gateway to access the various services
3. Explore the API documentation for each service to learn about the available endpoints

## API Reference

For detailed information about each service's API, refer to the following documentation:

- [API Gateway](api-gateway.md)
- [Ingestion Service](ingestion-service.md)
- [ETL Service](etl-service.md)
- [Database Service](database-service.md)
- [Storage Service](storage-service.md)

## Support

If you need help with the Generic Data Platform API, please contact our support team at support@genericdataplatform.com.
