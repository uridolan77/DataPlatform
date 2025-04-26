using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution.MigrationPlan
{
    public abstract class BaseMigrationPlanGenerator
    {
        protected readonly SchemaComparer _schemaComparer;
        protected readonly ILogger _logger;
        
        protected BaseMigrationPlanGenerator(SchemaComparer schemaComparer, ILogger logger)
        {
            _schemaComparer = schemaComparer;
            _logger = logger;
        }
        
        public async Task<SchemaMigrationPlan> GenerateMigrationPlan(DataSchema oldSchema, DataSchema newSchema, DatabaseType databaseType)
        {
            try
            {
                // Create a new migration plan
                var plan = new SchemaMigrationPlan
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceId = newSchema.SourceId,
                    OldSchemaId = oldSchema?.Id,
                    NewSchemaId = newSchema.Id,
                    DatabaseType = databaseType,
                    RequiresDowntime = false,
                    EstimatedDurationSeconds = 0
                };
                
                // Get schema changes
                var changes = await _schemaComparer.CompareSchemas(oldSchema, newSchema);
                
                // Generate migration steps
                var migrationSteps = await GenerateMigrationSteps(oldSchema, newSchema, changes);
                plan.Steps.AddRange(migrationSteps);
                
                // Generate data transformation steps
                var dataTransformations = await GenerateDataTransformations(oldSchema, newSchema, changes);
                plan.DataTransformations.AddRange(dataTransformations);
                
                // Generate rollback script
                plan.RollbackScript = await GenerateRollbackScript(oldSchema, newSchema, changes);
                
                // Determine if downtime is required
                plan.RequiresDowntime = DetermineIfDowntimeRequired(changes);
                
                // Estimate duration
                plan.EstimatedDurationSeconds = EstimateDuration(changes);
                
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating migration plan");
                throw;
            }
        }
        
        protected abstract Task<List<SchemaMigrationStep>> GenerateMigrationSteps(
            DataSchema oldSchema, 
            DataSchema newSchema, 
            List<SchemaChange> changes);
        
        protected abstract Task<List<DataTransformationStep>> GenerateDataTransformations(
            DataSchema oldSchema, 
            DataSchema newSchema, 
            List<SchemaChange> changes);
        
        protected abstract Task<string> GenerateRollbackScript(
            DataSchema oldSchema, 
            DataSchema newSchema, 
            List<SchemaChange> changes);
        
        protected virtual bool DetermineIfDowntimeRequired(List<SchemaChange> changes)
        {
            // By default, assume downtime is required for breaking changes
            return changes.Any(c => c.IsBreakingChange);
        }
        
        protected virtual int EstimateDuration(List<SchemaChange> changes)
        {
            // Simple estimation based on number of changes
            // In a real implementation, this would be more sophisticated
            return changes.Count * 5; // 5 seconds per change
        }
        
        protected string GetSqlTypeForFieldType(FieldType fieldType, DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return GetPostgreSqlType(fieldType);
                
                case DatabaseType.SQLServer:
                    return GetSqlServerType(fieldType);
                
                case DatabaseType.MySQL:
                    return GetMySqlType(fieldType);
                
                case DatabaseType.Oracle:
                    return GetOracleType(fieldType);
                
                case DatabaseType.SQLite:
                    return GetSqliteType(fieldType);
                
                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }
        
        private string GetPostgreSqlType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return "TEXT";
                case FieldType.Integer:
                    return "INTEGER";
                case FieldType.Decimal:
                    return "NUMERIC";
                case FieldType.Boolean:
                    return "BOOLEAN";
                case FieldType.DateTime:
                    return "TIMESTAMP";
                case FieldType.Json:
                    return "JSONB";
                case FieldType.Complex:
                    return "JSONB";
                case FieldType.Binary:
                    return "BYTEA";
                case FieldType.Enum:
                    return "TEXT";
                case FieldType.Reference:
                    return "TEXT";
                case FieldType.Geometry:
                    return "GEOMETRY";
                case FieldType.Array:
                    return "JSONB";
                case FieldType.Map:
                    return "JSONB";
                default:
                    return "TEXT";
            }
        }
        
        private string GetSqlServerType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return "NVARCHAR(MAX)";
                case FieldType.Integer:
                    return "INT";
                case FieldType.Decimal:
                    return "DECIMAL(18, 6)";
                case FieldType.Boolean:
                    return "BIT";
                case FieldType.DateTime:
                    return "DATETIME2";
                case FieldType.Json:
                    return "NVARCHAR(MAX)";
                case FieldType.Complex:
                    return "NVARCHAR(MAX)";
                case FieldType.Binary:
                    return "VARBINARY(MAX)";
                case FieldType.Enum:
                    return "NVARCHAR(255)";
                case FieldType.Reference:
                    return "NVARCHAR(255)";
                case FieldType.Geometry:
                    return "GEOGRAPHY";
                case FieldType.Array:
                    return "NVARCHAR(MAX)";
                case FieldType.Map:
                    return "NVARCHAR(MAX)";
                default:
                    return "NVARCHAR(MAX)";
            }
        }
        
        private string GetMySqlType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return "TEXT";
                case FieldType.Integer:
                    return "INT";
                case FieldType.Decimal:
                    return "DECIMAL(18, 6)";
                case FieldType.Boolean:
                    return "TINYINT(1)";
                case FieldType.DateTime:
                    return "DATETIME";
                case FieldType.Json:
                    return "JSON";
                case FieldType.Complex:
                    return "JSON";
                case FieldType.Binary:
                    return "BLOB";
                case FieldType.Enum:
                    return "VARCHAR(255)";
                case FieldType.Reference:
                    return "VARCHAR(255)";
                case FieldType.Geometry:
                    return "GEOMETRY";
                case FieldType.Array:
                    return "JSON";
                case FieldType.Map:
                    return "JSON";
                default:
                    return "TEXT";
            }
        }
        
        private string GetOracleType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return "CLOB";
                case FieldType.Integer:
                    return "NUMBER(10)";
                case FieldType.Decimal:
                    return "NUMBER(18, 6)";
                case FieldType.Boolean:
                    return "NUMBER(1)";
                case FieldType.DateTime:
                    return "TIMESTAMP";
                case FieldType.Json:
                    return "CLOB";
                case FieldType.Complex:
                    return "CLOB";
                case FieldType.Binary:
                    return "BLOB";
                case FieldType.Enum:
                    return "VARCHAR2(255)";
                case FieldType.Reference:
                    return "VARCHAR2(255)";
                case FieldType.Geometry:
                    return "SDO_GEOMETRY";
                case FieldType.Array:
                    return "CLOB";
                case FieldType.Map:
                    return "CLOB";
                default:
                    return "CLOB";
            }
        }
        
        private string GetSqliteType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return "TEXT";
                case FieldType.Integer:
                    return "INTEGER";
                case FieldType.Decimal:
                    return "REAL";
                case FieldType.Boolean:
                    return "INTEGER";
                case FieldType.DateTime:
                    return "TEXT";
                case FieldType.Json:
                    return "TEXT";
                case FieldType.Complex:
                    return "TEXT";
                case FieldType.Binary:
                    return "BLOB";
                case FieldType.Enum:
                    return "TEXT";
                case FieldType.Reference:
                    return "TEXT";
                case FieldType.Geometry:
                    return "TEXT";
                case FieldType.Array:
                    return "TEXT";
                case FieldType.Map:
                    return "TEXT";
                default:
                    return "TEXT";
            }
        }
    }
}
