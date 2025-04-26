# Storage Service API Documentation

The Storage Service provides APIs for storing and retrieving files and objects in various storage systems, including local file system, cloud storage, and object storage.

## Base URL

```
https://localhost:7227/api
```

## Authentication

All API requests require authentication using JWT (JSON Web Token). Include the token in the Authorization header:

```
Authorization: Bearer {your_jwt_token}
```

## Storage Providers

### List Storage Providers

Returns a list of available storage providers.

```
GET /api/storage-providers
```

#### Response

```json
[
  {
    "type": "LocalFileSystem",
    "displayName": "Local File System",
    "description": "Store files on the local file system",
    "requiredProperties": [
      {
        "name": "basePath",
        "displayName": "Base Path",
        "type": "string",
        "isRequired": true
      }
    ]
  },
  {
    "type": "AzureBlobStorage",
    "displayName": "Azure Blob Storage",
    "description": "Store files in Azure Blob Storage",
    "requiredProperties": [
      {
        "name": "connectionString",
        "displayName": "Connection String",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "containerName",
        "displayName": "Container Name",
        "type": "string",
        "isRequired": true
      }
    ]
  },
  {
    "type": "AmazonS3",
    "displayName": "Amazon S3",
    "description": "Store files in Amazon S3",
    "requiredProperties": [
      {
        "name": "accessKey",
        "displayName": "Access Key",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "secretKey",
        "displayName": "Secret Key",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "bucketName",
        "displayName": "Bucket Name",
        "type": "string",
        "isRequired": true
      },
      {
        "name": "region",
        "displayName": "Region",
        "type": "string",
        "isRequired": true
      }
    ]
  }
]
```

## Storage Locations

### List Storage Locations

Returns a list of all storage locations.

```
GET /api/storage-locations
```

#### Response

```json
[
  {
    "id": "loc-001",
    "name": "Local Data Files",
    "description": "Local storage for data files",
    "type": "LocalFileSystem",
    "connectionProperties": {
      "basePath": "C:/data/files"
    },
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z"
  },
  {
    "id": "loc-002",
    "name": "Azure Blob Storage",
    "description": "Azure storage for data files",
    "type": "AzureBlobStorage",
    "connectionProperties": {
      "connectionString": "DefaultEndpointsProtocol=https;AccountName=...",
      "containerName": "data-files"
    },
    "createdAt": "2023-06-01T12:00:00Z",
    "updatedAt": "2023-06-01T12:00:00Z"
  }
]
```

### Get Storage Location

Returns details of a specific storage location.

```
GET /api/storage-locations/{id}
```

#### Parameters

- `id` (path): The ID of the storage location

#### Response

```json
{
  "id": "loc-001",
  "name": "Local Data Files",
  "description": "Local storage for data files",
  "type": "LocalFileSystem",
  "connectionProperties": {
    "basePath": "C:/data/files"
  },
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z"
}
```

### Create Storage Location

Creates a new storage location.

```
POST /api/storage-locations
```

#### Request Body

```json
{
  "name": "Amazon S3 Storage",
  "description": "S3 storage for data files",
  "type": "AmazonS3",
  "connectionProperties": {
    "accessKey": "AKIAIOSFODNN7EXAMPLE",
    "secretKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "bucketName": "data-files",
    "region": "us-west-2"
  }
}
```

#### Response

```json
{
  "id": "loc-003",
  "name": "Amazon S3 Storage",
  "description": "S3 storage for data files",
  "type": "AmazonS3",
  "connectionProperties": {
    "accessKey": "AKIAIOSFODNN7EXAMPLE",
    "secretKey": "********",
    "bucketName": "data-files",
    "region": "us-west-2"
  },
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T12:00:00Z"
}
```

### Update Storage Location

Updates an existing storage location.

```
PUT /api/storage-locations/{id}
```

#### Parameters

- `id` (path): The ID of the storage location to update

#### Request Body

```json
{
  "name": "Updated Amazon S3 Storage",
  "description": "Updated S3 storage for data files",
  "type": "AmazonS3",
  "connectionProperties": {
    "accessKey": "AKIAIOSFODNN7EXAMPLE",
    "secretKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "bucketName": "updated-data-files",
    "region": "us-west-2"
  }
}
```

#### Response

```json
{
  "id": "loc-003",
  "name": "Updated Amazon S3 Storage",
  "description": "Updated S3 storage for data files",
  "type": "AmazonS3",
  "connectionProperties": {
    "accessKey": "AKIAIOSFODNN7EXAMPLE",
    "secretKey": "********",
    "bucketName": "updated-data-files",
    "region": "us-west-2"
  },
  "createdAt": "2023-06-01T12:00:00Z",
  "updatedAt": "2023-06-01T13:00:00Z"
}
```

### Delete Storage Location

Deletes a storage location.

```
DELETE /api/storage-locations/{id}
```

#### Parameters

- `id` (path): The ID of the storage location to delete

#### Response

```
204 No Content
```

### Validate Storage Location

Validates the connection to a storage location.

```
POST /api/storage-locations/{id}/validate
```

#### Parameters

- `id` (path): The ID of the storage location to validate

#### Response

```json
{
  "success": true,
  "message": "Connection is valid"
}
```

## Files

### List Files

Returns a list of files in a storage location.

```
GET /api/files
```

#### Query Parameters

- `locationId` (query): The ID of the storage location
- `path` (query, optional): The path to list files from (default: root)
- `recursive` (query, optional): Whether to list files recursively (default: false)
- `pageSize` (query, optional): The number of files to return per page (default: 100)
- `pageToken` (query, optional): The token for the next page of results

#### Response

```json
{
  "files": [
    {
      "name": "data.csv",
      "path": "/data.csv",
      "size": 1024,
      "contentType": "text/csv",
      "lastModified": "2023-06-01T12:00:00Z",
      "isDirectory": false
    },
    {
      "name": "images",
      "path": "/images",
      "lastModified": "2023-06-01T12:00:00Z",
      "isDirectory": true
    }
  ],
  "nextPageToken": "token123"
}
```

### Get File Metadata

Returns metadata for a specific file.

```
GET /api/files/{path}
```

#### Parameters

- `path` (path): The path to the file

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Response

```json
{
  "name": "data.csv",
  "path": "/data.csv",
  "size": 1024,
  "contentType": "text/csv",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": false,
  "metadata": {
    "createdBy": "user1",
    "description": "Sample data file"
  }
}
```

### Upload File

Uploads a file to a storage location.

```
POST /api/files/{path}
```

#### Parameters

- `path` (path): The path to upload the file to

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Request Body

The file content as multipart/form-data.

#### Response

```json
{
  "name": "data.csv",
  "path": "/data.csv",
  "size": 1024,
  "contentType": "text/csv",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": false
}
```

### Download File

Downloads a file from a storage location.

```
GET /api/files/{path}/download
```

#### Parameters

- `path` (path): The path to the file to download

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Response

The file content with the appropriate content type.

### Delete File

Deletes a file from a storage location.

```
DELETE /api/files/{path}
```

#### Parameters

- `path` (path): The path to the file to delete

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Response

```
204 No Content
```

### Copy File

Copies a file from one location to another.

```
POST /api/files/{path}/copy
```

#### Parameters

- `path` (path): The path to the file to copy

#### Query Parameters

- `locationId` (query): The ID of the source storage location

#### Request Body

```json
{
  "destinationLocationId": "loc-002",
  "destinationPath": "/backup/data.csv"
}
```

#### Response

```json
{
  "name": "data.csv",
  "path": "/backup/data.csv",
  "size": 1024,
  "contentType": "text/csv",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": false
}
```

### Move File

Moves a file from one location to another.

```
POST /api/files/{path}/move
```

#### Parameters

- `path` (path): The path to the file to move

#### Query Parameters

- `locationId` (query): The ID of the source storage location

#### Request Body

```json
{
  "destinationLocationId": "loc-002",
  "destinationPath": "/archive/data.csv"
}
```

#### Response

```json
{
  "name": "data.csv",
  "path": "/archive/data.csv",
  "size": 1024,
  "contentType": "text/csv",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": false
}
```

## Directories

### Create Directory

Creates a new directory in a storage location.

```
POST /api/directories/{path}
```

#### Parameters

- `path` (path): The path to create the directory at

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Response

```json
{
  "name": "new-folder",
  "path": "/new-folder",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": true
}
```

### Delete Directory

Deletes a directory from a storage location.

```
DELETE /api/directories/{path}
```

#### Parameters

- `path` (path): The path to the directory to delete

#### Query Parameters

- `locationId` (query): The ID of the storage location
- `recursive` (query, optional): Whether to delete the directory recursively (default: false)

#### Response

```
204 No Content
```

## File Metadata

### Get File Metadata

Returns metadata for a specific file.

```
GET /api/file-metadata/{path}
```

#### Parameters

- `path` (path): The path to the file

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Response

```json
{
  "name": "data.csv",
  "path": "/data.csv",
  "size": 1024,
  "contentType": "text/csv",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": false,
  "metadata": {
    "createdBy": "user1",
    "description": "Sample data file",
    "tags": ["data", "csv", "sample"]
  }
}
```

### Update File Metadata

Updates metadata for a specific file.

```
PUT /api/file-metadata/{path}
```

#### Parameters

- `path` (path): The path to the file

#### Query Parameters

- `locationId` (query): The ID of the storage location

#### Request Body

```json
{
  "metadata": {
    "description": "Updated sample data file",
    "tags": ["data", "csv", "sample", "updated"]
  }
}
```

#### Response

```json
{
  "name": "data.csv",
  "path": "/data.csv",
  "size": 1024,
  "contentType": "text/csv",
  "lastModified": "2023-06-01T12:00:00Z",
  "isDirectory": false,
  "metadata": {
    "createdBy": "user1",
    "description": "Updated sample data file",
    "tags": ["data", "csv", "sample", "updated"]
  }
}
```

## File Search

### Search Files

Searches for files in a storage location.

```
POST /api/files/search
```

#### Request Body

```json
{
  "locationId": "loc-001",
  "query": "*.csv",
  "recursive": true,
  "metadata": {
    "tags": ["data"]
  },
  "modifiedAfter": "2023-01-01T00:00:00Z",
  "modifiedBefore": "2023-06-01T00:00:00Z",
  "minSize": 1000,
  "maxSize": 10000,
  "pageSize": 100,
  "pageToken": null
}
```

#### Response

```json
{
  "files": [
    {
      "name": "data.csv",
      "path": "/data.csv",
      "size": 1024,
      "contentType": "text/csv",
      "lastModified": "2023-05-01T12:00:00Z",
      "isDirectory": false,
      "metadata": {
        "tags": ["data", "csv", "sample"]
      }
    },
    {
      "name": "customers.csv",
      "path": "/customers.csv",
      "size": 2048,
      "contentType": "text/csv",
      "lastModified": "2023-04-01T12:00:00Z",
      "isDirectory": false,
      "metadata": {
        "tags": ["data", "csv", "customers"]
      }
    }
  ],
  "nextPageToken": null,
  "totalCount": 2
}
```

## Health Check

### Get Health Status

Returns the health status of the Storage Service.

```
GET /health
```

#### Response

```json
{
  "status": "Healthy",
  "service": "StorageService",
  "version": "1.0.0.0",
  "timestamp": "2023-06-01T12:00:00Z"
}
```
