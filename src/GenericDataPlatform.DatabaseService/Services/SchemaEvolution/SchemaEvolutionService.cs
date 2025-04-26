using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.DatabaseService.Services.SchemaEvolution.MigrationPlan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data.SqlClient;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution
{
    public class SchemaEvolutionService : ISchemaEvolutionService
    {
        private readonly SchemaComparer _schemaComparer;
        private readonly SchemaValidator _schemaValidator;
        private readonly MigrationPlanGeneratorFactory _migrationPlanGeneratorFactory;
        private readonly ILogger<SchemaEvolutionService> _logger;
        private readonly DatabaseOptions _databaseOptions;

        public SchemaEvolutionService(
            SchemaComparer schemaComparer,
            SchemaValidator schemaValidator,
            MigrationPlanGeneratorFactory migrationPlanGeneratorFactory,
            IOptions<DatabaseOptions> databaseOptions,
            ILogger<SchemaEvolutionService> logger)
        {
            _schemaComparer = schemaComparer;
            _schemaValidator = schemaValidator;
            _migrationPlanGeneratorFactory = migrationPlanGeneratorFactory;
            _databaseOptions = databaseOptions.Value;
            _logger = logger;
        }

        public async Task<List<SchemaChange>> CompareSchemas(DataSchema oldSchema, DataSchema newSchema)
        {
            try
            {
                return await _schemaComparer.CompareSchemas(oldSchema, newSchema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing schemas");
                throw;
            }
        }

        public async Task<SchemaValidationResult> ValidateSchemaCompatibility(DataSchema oldSchema, DataSchema newSchema)
        {
            try
            {
                return await _schemaValidator.ValidateSchemaCompatibility(oldSchema, newSchema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating schema compatibility");
                throw;
            }
        }

        public async Task<SchemaMigrationPlan> GenerateMigrationPlan(DataSchema oldSchema, DataSchema newSchema, DatabaseType databaseType)
        {
            try
            {
                // Create the appropriate migration plan generator
                var generator = _migrationPlanGeneratorFactory.CreateGenerator(databaseType);

                // Generate the migration plan
                return await generator.GenerateMigrationPlan(oldSchema, newSchema, databaseType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating migration plan");
                throw;
            }
        }

        public async Task<SchemaMigrationResult> ExecuteMigrationPlan(string sourceId, SchemaMigrationPlan migrationPlan)
        {
            var result = new SchemaMigrationResult
            {
                Success = true,
                StepsExecuted = 0,
                TotalSteps = migrationPlan.Steps.Count,
                RecordsAffected = 0
            };

            IDbConnection connection = null;
            IDbTransaction transaction = null;

            try
            {
                // Create connection based on database type
                connection = CreateConnection(migrationPlan.DatabaseType);
                await connection.OpenAsync();

                // Start a transaction
                transaction = connection.BeginTransaction();

                var startTime = DateTime.UtcNow;

                // Execute each migration step
                foreach (var step in migrationPlan.Steps.OrderBy(s => s.Order))
                {
                    _logger.LogInformation("Executing migration step {Order}: {Description}", step.Order, step.Description);

                    try
                    {
                        // Execute the SQL script
                        var rowsAffected = await connection.ExecuteAsync(step.SqlScript, transaction: transaction);
                        result.RecordsAffected += rowsAffected;
                        result.StepsExecuted++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing migration step {Order}: {Description}", step.Order, step.Description);

                        result.Success = false;
                        result.Errors.Add($"Step {step.Order}: {ex.Message}");

                        // Rollback the transaction
                        transaction.Rollback();
                        result.RolledBack = true;

                        return result;
                    }
                }

                // Execute data transformations
                foreach (var transformation in migrationPlan.DataTransformations.OrderBy(t => t.Order))
                {
                    _logger.LogInformation("Executing data transformation {Order}: {Description}", transformation.Order, transformation.Description);

                    try
                    {
                        // Execute the transformation script
                        var rowsAffected = await connection.ExecuteAsync(transformation.TransformationScript, transaction: transaction);
                        result.RecordsAffected += rowsAffected;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing data transformation {Order}: {Description}", transformation.Order, transformation.Description);

                        result.Success = false;
                        result.Errors.Add($"Transformation {transformation.Order}: {ex.Message}");

                        // Rollback the transaction
                        transaction.Rollback();
                        result.RolledBack = true;

                        return result;
                    }
                }

                // Commit the transaction
                transaction.Commit();

                var endTime = DateTime.UtcNow;
                result.DurationSeconds = (int)(endTime - startTime).TotalSeconds;

                // Update schema version in the schema_history table
                await UpdateSchemaHistory(sourceId, migrationPlan);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration plan");

                // Rollback the transaction if it exists
                if (transaction != null)
                {
                    transaction.Rollback();
                    result.RolledBack = true;
                }

                result.Success = false;
                result.Errors.Add(ex.Message);

                return result;
            }
            finally
            {
                // Dispose the transaction and connection
                transaction?.Dispose();
                connection?.Dispose();
            }
        }

        public async Task<List<DataSchema>> GetSchemaHistory(string sourceId)
        {
            try
            {
                // Get the default database type from options
                var databaseType = _databaseOptions.DefaultDatabaseType;

                using var connection = CreateConnection(databaseType);
                await connection.OpenAsync();

                // Ensure the schema_history table exists
                await EnsureSchemaHistoryTableExists(connection, databaseType);

                // Get all schema versions for the source
                var query = GetSchemaHistoryQuery(databaseType);

                var schemaJsonList = await connection.QueryAsync<string>(query, new { sourceId });

                // Deserialize the schema definitions
                var schemas = new List<DataSchema>();

                foreach (var schemaJson in schemaJsonList)
                {
                    var schema = System.Text.Json.JsonSerializer.Deserialize<DataSchema>(schemaJson);
                    schemas.Add(schema);
                }

                return schemas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schema history for source {sourceId}", sourceId);
                throw;
            }
        }

        public async Task<DataSchema> GetSchemaVersion(string sourceId, string versionNumber)
        {
            try
            {
                // Get the default database type from options
                var databaseType = _databaseOptions.DefaultDatabaseType;

                using var connection = CreateConnection(databaseType);
                await connection.OpenAsync();

                // Ensure the schema_history table exists
                await EnsureSchemaHistoryTableExists(connection, databaseType);

                // Get the specific schema version
                var query = GetSchemaVersionQuery(databaseType);

                var schemaJson = await connection.QueryFirstOrDefaultAsync<string>(query, new { sourceId, versionNumber });

                if (string.IsNullOrEmpty(schemaJson))
                {
                    return null;
                }

                // Deserialize the schema definition
                var schema = System.Text.Json.JsonSerializer.Deserialize<DataSchema>(schemaJson);

                return schema;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schema version {versionNumber} for source {sourceId}", versionNumber, sourceId);
                throw;
            }
        }

        private IDbConnection CreateConnection(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return new NpgsqlConnection(_databaseOptions.PostgresConnectionString);

                case DatabaseType.SQLServer:
                    return new SqlConnection(_databaseOptions.SqlServerConnectionString);

                case DatabaseType.MySQL:
                    return new MySqlConnection(_databaseOptions.MySqlConnectionString);

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }

        private async Task EnsureSchemaHistoryTableExists(IDbConnection connection, DatabaseType databaseType)
        {
            var query = GetCreateSchemaHistoryTableQuery(databaseType);
            await connection.ExecuteAsync(query);
        }

        private string GetCreateSchemaHistoryTableQuery(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return @"
                        CREATE TABLE IF NOT EXISTS schema_history (
                            id SERIAL PRIMARY KEY,
                            source_id VARCHAR(50) NOT NULL,
                            schema_id VARCHAR(50) NOT NULL,
                            version_number VARCHAR(50) NOT NULL,
                            effective_date TIMESTAMP NOT NULL,
                            author VARCHAR(100),
                            change_description TEXT,
                            schema_definition JSONB NOT NULL,
                            migration_plan JSONB,
                            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE INDEX IF NOT EXISTS idx_schema_history_source_id ON schema_history(source_id);
                        CREATE INDEX IF NOT EXISTS idx_schema_history_version ON schema_history(source_id, version_number);";

                case DatabaseType.SQLServer:
                    return @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[schema_history]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE schema_history (
                                id INT IDENTITY(1,1) PRIMARY KEY,
                                source_id NVARCHAR(50) NOT NULL,
                                schema_id NVARCHAR(50) NOT NULL,
                                version_number NVARCHAR(50) NOT NULL,
                                effective_date DATETIME2 NOT NULL,
                                author NVARCHAR(100),
                                change_description NVARCHAR(MAX),
                                schema_definition NVARCHAR(MAX) NOT NULL,
                                migration_plan NVARCHAR(MAX),
                                created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );

                            CREATE INDEX idx_schema_history_source_id ON schema_history(source_id);
                            CREATE INDEX idx_schema_history_version ON schema_history(source_id, version_number);
                        END";

                case DatabaseType.MySQL:
                    return @"
                        CREATE TABLE IF NOT EXISTS schema_history (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            source_id VARCHAR(50) NOT NULL,
                            schema_id VARCHAR(50) NOT NULL,
                            version_number VARCHAR(50) NOT NULL,
                            effective_date DATETIME NOT NULL,
                            author VARCHAR(100),
                            change_description TEXT,
                            schema_definition JSON NOT NULL,
                            migration_plan JSON,
                            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE INDEX IF NOT EXISTS idx_schema_history_source_id ON schema_history(source_id);
                        CREATE INDEX IF NOT EXISTS idx_schema_history_version ON schema_history(source_id, version_number);";

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }

        private string GetSchemaHistoryQuery(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return @"
                        SELECT schema_definition
                        FROM schema_history
                        WHERE source_id = @sourceId
                        ORDER BY effective_date DESC";

                case DatabaseType.SQLServer:
                    return @"
                        SELECT schema_definition
                        FROM schema_history
                        WHERE source_id = @sourceId
                        ORDER BY effective_date DESC";

                case DatabaseType.MySQL:
                    return @"
                        SELECT schema_definition
                        FROM schema_history
                        WHERE source_id = @sourceId
                        ORDER BY effective_date DESC";

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }

        private string GetSchemaVersionQuery(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return @"
                        SELECT schema_definition
                        FROM schema_history
                        WHERE source_id = @sourceId
                        AND version_number = @versionNumber";

                case DatabaseType.SQLServer:
                    return @"
                        SELECT schema_definition
                        FROM schema_history
                        WHERE source_id = @sourceId
                        AND version_number = @versionNumber";

                case DatabaseType.MySQL:
                    return @"
                        SELECT schema_definition
                        FROM schema_history
                        WHERE source_id = @sourceId
                        AND version_number = @versionNumber";

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }

        private async Task UpdateSchemaHistory(string sourceId, SchemaMigrationPlan migrationPlan)
        {
            try
            {
                // Get the database type from the migration plan
                var databaseType = migrationPlan.DatabaseType;

                using var connection = CreateConnection(databaseType);
                await connection.OpenAsync();

                // Ensure the schema_history table exists
                await EnsureSchemaHistoryTableExists(connection, databaseType);

                // Get the new schema
                var query = GetSchemaQuery(databaseType);

                var schemaJson = await connection.QueryFirstOrDefaultAsync<string>(query, new { sourceId });

                if (string.IsNullOrEmpty(schemaJson))
                {
                    _logger.LogWarning("Schema not found for source {sourceId}", sourceId);
                    return;
                }

                // Deserialize the schema
                var schema = System.Text.Json.JsonSerializer.Deserialize<DataSchema>(schemaJson);

                // Insert the schema into the history table
                var insertQuery = GetInsertSchemaHistoryQuery(databaseType);

                await connection.ExecuteAsync(insertQuery, new
                {
                    sourceId,
                    schemaId = schema.Id,
                    versionNumber = schema.Version?.VersionNumber,
                    effectiveDate = schema.Version?.EffectiveDate ?? DateTime.UtcNow,
                    author = schema.Version?.Author,
                    changeDescription = schema.Version?.ChangeDescription,
                    schemaDefinition = schemaJson,
                    migrationPlan = System.Text.Json.JsonSerializer.Serialize(migrationPlan)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schema history for source {sourceId}", sourceId);
                throw;
            }
        }

        private string GetSchemaQuery(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return @"
                        SELECT schema_definition
                        FROM data_sources
                        WHERE id = @sourceId";

                case DatabaseType.SQLServer:
                    return @"
                        SELECT schema_definition
                        FROM data_sources
                        WHERE id = @sourceId";

                case DatabaseType.MySQL:
                    return @"
                        SELECT schema_definition
                        FROM data_sources
                        WHERE id = @sourceId";

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }

        private string GetInsertSchemaHistoryQuery(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return @"
                        INSERT INTO schema_history (
                            source_id,
                            schema_id,
                            version_number,
                            effective_date,
                            author,
                            change_description,
                            schema_definition,
                            migration_plan,
                            created_at
                        )
                        VALUES (
                            @sourceId,
                            @schemaId,
                            @versionNumber,
                            @effectiveDate,
                            @author,
                            @changeDescription,
                            @schemaDefinition,
                            @migrationPlan,
                            CURRENT_TIMESTAMP
                        )";

                case DatabaseType.SQLServer:
                    return @"
                        INSERT INTO schema_history (
                            source_id,
                            schema_id,
                            version_number,
                            effective_date,
                            author,
                            change_description,
                            schema_definition,
                            migration_plan,
                            created_at
                        )
                        VALUES (
                            @sourceId,
                            @schemaId,
                            @versionNumber,
                            @effectiveDate,
                            @author,
                            @changeDescription,
                            @schemaDefinition,
                            @migrationPlan,
                            GETUTCDATE()
                        )";

                case DatabaseType.MySQL:
                    return @"
                        INSERT INTO schema_history (
                            source_id,
                            schema_id,
                            version_number,
                            effective_date,
                            author,
                            change_description,
                            schema_definition,
                            migration_plan,
                            created_at
                        )
                        VALUES (
                            @sourceId,
                            @schemaId,
                            @versionNumber,
                            @effectiveDate,
                            @author,
                            @changeDescription,
                            @schemaDefinition,
                            @migrationPlan,
                            CURRENT_TIMESTAMP
                        )";

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }
    }
}
