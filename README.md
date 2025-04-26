# Generic Data Platform

A comprehensive C# implementation for a generic data platform that can ingest, process, and serve any type of data. The platform uses a microservices architecture with gRPC for inter-service communication and is designed to be extensible, configurable, and adaptable to different data sources and processing requirements.

## Core Design Principles

1. **Source Agnostic**: Able to ingest data from any source through configurable connectors
2. **Schema Flexibility**: Supports both schema-on-write and schema-on-read approaches
3. **Extensible Pipeline**: Modular ETL components that can be arranged in different workflows
4. **Storage Tier Separation**: Clear separation between raw, processed, and specialized storage
5. **Pluggable Architecture**: Services can be added or removed based on requirements
6. **Self-Service API**: Comprehensive API for data access and management

## Project Structure

- **src/**: Contains the source code for all services
  - **GenericDataPlatform.Common/**: Shared library with common models and utilities
  - **GenericDataPlatform.IngestionService/**: Service for data ingestion from various sources
  - **GenericDataPlatform.StorageService/**: Service for raw data storage
  - **GenericDataPlatform.DatabaseService/**: Service for structured data storage
  - **GenericDataPlatform.ETL/**: Service for data processing pipelines
  - **GenericDataPlatform.API/**: API layer for data access
  - **Protos/**: Shared Protocol Buffer definitions for service communication

- **tests/**: Contains test projects
  - **GenericDataPlatform.Common.Tests/**: Tests for the common library
  - **GenericDataPlatform.Ingestion.Tests/**: Tests for the ingestion service
  - **GenericDataPlatform.Storage.Tests/**: Tests for the storage service
  - **...**: Other test projects

- **deploy/**: Contains deployment configurations
  - **docker/**: Docker Compose files
  - **kubernetes/**: Kubernetes manifests
  - **terraform/**: Infrastructure as code

- **docs/**: Contains documentation
  - **architecture/**: Architecture documentation
  - **api/**: API documentation
  - **development/**: Development guides

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Docker (for running the services in containers)
- PostgreSQL (for structured data storage)
- MinIO (for object storage)
- Elasticsearch (for document storage)

### Building the Solution

```bash
cd src
dotnet build
```

### Running the Tests

```bash
cd tests
dotnet test
```

### Running the Services

Using Docker Compose:

```bash
cd deploy/docker
docker-compose up -d
```

## API Documentation

The API documentation is available at `http://localhost:5000/swagger` when the API service is running.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
