using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution
{
    public interface ISchemaEvolutionService
    {
        /// <summary>
        /// Compares two schemas and generates a list of changes
        /// </summary>
        /// <param name="oldSchema">The old schema</param>
        /// <param name="newSchema">The new schema</param>
        /// <returns>A list of schema changes</returns>
        Task<List<SchemaChange>> CompareSchemas(DataSchema oldSchema, DataSchema newSchema);
        
        /// <summary>
        /// Validates if the new schema is compatible with the old schema based on the evolution strategy
        /// </summary>
        /// <param name="oldSchema">The old schema</param>
        /// <param name="newSchema">The new schema</param>
        /// <returns>A validation result with details about compatibility</returns>
        Task<SchemaValidationResult> ValidateSchemaCompatibility(DataSchema oldSchema, DataSchema newSchema);
        
        /// <summary>
        /// Generates a migration plan for evolving from the old schema to the new schema
        /// </summary>
        /// <param name="oldSchema">The old schema</param>
        /// <param name="newSchema">The new schema</param>
        /// <param name="databaseType">The type of database</param>
        /// <returns>A migration plan with SQL scripts and data transformation steps</returns>
        Task<SchemaMigrationPlan> GenerateMigrationPlan(DataSchema oldSchema, DataSchema newSchema, DatabaseType databaseType);
        
        /// <summary>
        /// Executes a migration plan to evolve the schema
        /// </summary>
        /// <param name="sourceId">The source ID</param>
        /// <param name="migrationPlan">The migration plan to execute</param>
        /// <returns>A result indicating success or failure</returns>
        Task<SchemaMigrationResult> ExecuteMigrationPlan(string sourceId, SchemaMigrationPlan migrationPlan);
        
        /// <summary>
        /// Gets the schema history for a source
        /// </summary>
        /// <param name="sourceId">The source ID</param>
        /// <returns>A list of schema versions</returns>
        Task<List<DataSchema>> GetSchemaHistory(string sourceId);
        
        /// <summary>
        /// Gets a specific schema version for a source
        /// </summary>
        /// <param name="sourceId">The source ID</param>
        /// <param name="versionNumber">The version number</param>
        /// <returns>The schema version</returns>
        Task<DataSchema> GetSchemaVersion(string sourceId, string versionNumber);
    }
    
    public enum DatabaseType
    {
        PostgreSQL,
        SQLServer,
        MySQL,
        Oracle,
        SQLite
    }
    
    public class SchemaValidationResult
    {
        public bool IsValid { get; set; }
        public List<SchemaValidationIssue> Issues { get; set; } = new List<SchemaValidationIssue>();
        public bool HasBreakingChanges { get; set; }
        public bool RequiresMigration { get; set; }
    }
    
    public class SchemaValidationIssue
    {
        public SchemaValidationIssueType Type { get; set; }
        public string Message { get; set; }
        public string FieldName { get; set; }
        public bool IsBreakingChange { get; set; }
    }
    
    public enum SchemaValidationIssueType
    {
        FieldRemoved,
        FieldTypeChanged,
        RequiredAdded,
        ValidationChanged,
        IncompatibleEvolutionStrategy,
        Other
    }
    
    public class SchemaMigrationPlan
    {
        public string Id { get; set; }
        public string SourceId { get; set; }
        public string OldSchemaId { get; set; }
        public string NewSchemaId { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public List<SchemaMigrationStep> Steps { get; set; } = new List<SchemaMigrationStep>();
        public List<DataTransformationStep> DataTransformations { get; set; } = new List<DataTransformationStep>();
        public string RollbackScript { get; set; }
        public bool RequiresDowntime { get; set; }
        public int EstimatedDurationSeconds { get; set; }
    }
    
    public class SchemaMigrationStep
    {
        public int Order { get; set; }
        public string Description { get; set; }
        public string SqlScript { get; set; }
        public string RollbackScript { get; set; }
        public bool IsBreakingChange { get; set; }
    }
    
    public class DataTransformationStep
    {
        public int Order { get; set; }
        public string Description { get; set; }
        public string FieldName { get; set; }
        public string TransformationScript { get; set; }
        public string RollbackScript { get; set; }
    }
    
    public class SchemaMigrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public int StepsExecuted { get; set; }
        public int TotalSteps { get; set; }
        public bool RolledBack { get; set; }
        public int RecordsAffected { get; set; }
        public int DurationSeconds { get; set; }
    }
}
