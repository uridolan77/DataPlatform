using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution.MigrationPlan
{
    public class SqliteMigrationPlanGenerator : BaseMigrationPlanGenerator
    {
        public SqliteMigrationPlanGenerator(SchemaComparer schemaComparer, ILogger<SqliteMigrationPlanGenerator> logger)
            : base(schemaComparer, logger)
        {
        }
        
        protected override async Task<List<SchemaMigrationStep>> GenerateMigrationSteps(
            DataSchema oldSchema, 
            DataSchema newSchema, 
            List<SchemaChange> changes)
        {
            var steps = new List<SchemaMigrationStep>();
            
            // Get the table name for this source
            var tableName = GetTableName(newSchema.SourceId);
            
            // Group changes by type
            var addFieldChanges = changes.Where(c => c.ChangeType == SchemaChangeType.AddField).ToList();
            var removeFieldChanges = changes.Where(c => c.ChangeType == SchemaChangeType.RemoveField).ToList();
            var renameFieldChanges = changes.Where(c => c.ChangeType == SchemaChangeType.RenameField).ToList();
            var changeTypeChanges = changes.Where(c => c.ChangeType == SchemaChangeType.ChangeType).ToList();
            var changeRequiredChanges = changes.Where(c => c.ChangeType == SchemaChangeType.ChangeRequired).ToList();
            var changeDefaultChanges = changes.Where(c => c.ChangeType == SchemaChangeType.ChangeDefault).ToList();
            
            // SQLite has limited ALTER TABLE support, so for most changes we need to:
            // 1. Create a new table with the desired schema
            // 2. Copy data from the old table to the new table
            // 3. Drop the old table
            // 4. Rename the new table to the old table name
            
            // Process add field changes - SQLite supports adding columns directly
            if (addFieldChanges.Any())
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = $"Add {addFieldChanges.Count} new field(s)",
                    IsBreakingChange = addFieldChanges.Any(c => c.IsBreakingChange)
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                foreach (var change in addFieldChanges)
                {
                    var fieldName = change.FieldName;
                    var fieldType = change.NewValue.TryGetValue("Type", out var typeObj) && typeObj is FieldType type
                        ? type
                        : FieldType.String;
                    
                    var isRequired = change.NewValue.TryGetValue("IsRequired", out var requiredObj) && requiredObj is bool required && required;
                    var defaultValue = change.NewValue.TryGetValue("DefaultValue", out var defaultObj) ? defaultObj?.ToString() : null;
                    
                    // SQLite ADD COLUMN syntax
                    var columnDef = $"{fieldName} {GetSqlTypeForFieldType(fieldType, DatabaseType.SQLite)}";
                    
                    if (isRequired)
                    {
                        columnDef += " NOT NULL";
                        
                        if (!string.IsNullOrEmpty(defaultValue))
                        {
                            columnDef += $" DEFAULT '{defaultValue}'";
                        }
                        else
                        {
                            // SQLite requires a default value for NOT NULL columns when adding to existing table
                            columnDef += " DEFAULT ''";
                        }
                    }
                    else if (!string.IsNullOrEmpty(defaultValue))
                    {
                        columnDef += $" DEFAULT '{defaultValue}'";
                    }
                    
                    sql.AppendLine($"ALTER TABLE {tableName} ADD COLUMN {columnDef};");
                    
                    // SQLite doesn't support DROP COLUMN directly, so we need to use the table recreation approach for rollback
                    rollbackSql.AppendLine($"-- SQLite doesn't support DROP COLUMN directly");
                    rollbackSql.AppendLine($"-- To remove column {fieldName}, you need to:");
                    rollbackSql.AppendLine($"-- 1. Create a new table without the column");
                    rollbackSql.AppendLine($"-- 2. Copy data from the old table to the new table");
                    rollbackSql.AppendLine($"-- 3. Drop the old table");
                    rollbackSql.AppendLine($"-- 4. Rename the new table to the old table name");
                }
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            // For all other changes, we need to use the table recreation approach
            var otherChanges = removeFieldChanges.Any() || renameFieldChanges.Any() || 
                               changeTypeChanges.Any() || changeRequiredChanges.Any() || 
                               changeDefaultChanges.Any();
            
            if (otherChanges)
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = "Apply schema changes using table recreation",
                    IsBreakingChange = true
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                // Get all fields from the old schema
                var oldFields = oldSchema?.Fields ?? new List<SchemaField>();
                
                // Get all fields from the new schema
                var newFields = newSchema.Fields;
                
                // Create a mapping of old field names to new field names for renamed fields
                var renamedFields = new Dictionary<string, string>();
                foreach (var change in renameFieldChanges)
                {
                    var oldFieldName = change.FieldName;
                    var newFieldName = change.NewValue.TryGetValue("Name", out var nameObj) ? nameObj?.ToString() : null;
                    
                    if (!string.IsNullOrEmpty(newFieldName))
                    {
                        renamedFields[oldFieldName] = newFieldName;
                    }
                }
                
                // Create a temporary table with the new schema
                var tempTableName = $"{tableName}_new";
                
                sql.AppendLine($"-- Create a new table with the updated schema");
                sql.AppendLine($"CREATE TABLE {tempTableName} (");
                
                // Add all fields from the new schema
                var fieldDefs = new List<string>();
                foreach (var field in newFields)
                {
                    var fieldType = GetSqlTypeForFieldType(field.Type, DatabaseType.SQLite);
                    var fieldDef = $"    {field.Name} {fieldType}";
                    
                    if (field.IsRequired)
                    {
                        fieldDef += " NOT NULL";
                    }
                    
                    if (!string.IsNullOrEmpty(field.DefaultValue))
                    {
                        fieldDef += $" DEFAULT '{field.DefaultValue}'";
                    }
                    
                    fieldDefs.Add(fieldDef);
                }
                
                sql.AppendLine(string.Join(",\n", fieldDefs));
                sql.AppendLine(");");
                
                // Copy data from the old table to the new table
                sql.AppendLine($"-- Copy data from the old table to the new table");
                
                // Build the list of fields to copy
                var sourceFields = new List<string>();
                var targetFields = new List<string>();
                
                foreach (var field in newFields)
                {
                    // Check if this is a new field
                    var isNewField = !oldFields.Any(f => f.Name == field.Name) && 
                                     !renamedFields.ContainsValue(field.Name);
                    
                    if (isNewField)
                    {
                        // Skip new fields in the INSERT statement
                        continue;
                    }
                    
                    // Check if this field was renamed
                    var oldFieldName = renamedFields.FirstOrDefault(r => r.Value == field.Name).Key;
                    
                    if (!string.IsNullOrEmpty(oldFieldName))
                    {
                        // This field was renamed, use the old name in the source
                        sourceFields.Add(oldFieldName);
                    }
                    else
                    {
                        // Use the same name
                        sourceFields.Add(field.Name);
                    }
                    
                    targetFields.Add(field.Name);
                }
                
                sql.AppendLine($"INSERT INTO {tempTableName} ({string.Join(", ", targetFields)})");
                sql.AppendLine($"SELECT {string.Join(", ", sourceFields)} FROM {tableName};");
                
                // Drop the old table and rename the new table
                sql.AppendLine($"-- Drop the old table and rename the new table");
                sql.AppendLine($"DROP TABLE {tableName};");
                sql.AppendLine($"ALTER TABLE {tempTableName} RENAME TO {tableName};");
                
                // Generate rollback script
                rollbackSql.AppendLine($"-- Rollback requires recreating the original table structure");
                rollbackSql.AppendLine($"-- This is a simplified example and may need to be adjusted");
                rollbackSql.AppendLine($"CREATE TABLE {tempTableName} (");
                
                // Add all fields from the old schema
                var oldFieldDefs = new List<string>();
                foreach (var field in oldFields)
                {
                    var fieldType = GetSqlTypeForFieldType(field.Type, DatabaseType.SQLite);
                    var fieldDef = $"    {field.Name} {fieldType}";
                    
                    if (field.IsRequired)
                    {
                        fieldDef += " NOT NULL";
                    }
                    
                    if (!string.IsNullOrEmpty(field.DefaultValue))
                    {
                        fieldDef += $" DEFAULT '{field.DefaultValue}'";
                    }
                    
                    oldFieldDefs.Add(fieldDef);
                }
                
                rollbackSql.AppendLine(string.Join(",\n", oldFieldDefs));
                rollbackSql.AppendLine(");");
                
                // Copy data back from the current table to the original structure
                rollbackSql.AppendLine($"-- Copy data back to the original structure");
                
                // Build the list of fields to copy for rollback
                var rollbackSourceFields = new List<string>();
                var rollbackTargetFields = new List<string>();
                
                foreach (var field in oldFields)
                {
                    // Check if this field was renamed
                    var newFieldName = renamedFields.TryGetValue(field.Name, out var renamed) ? renamed : null;
                    
                    if (!string.IsNullOrEmpty(newFieldName))
                    {
                        // This field was renamed, use the new name in the source
                        rollbackSourceFields.Add(newFieldName);
                    }
                    else if (newFields.Any(f => f.Name == field.Name))
                    {
                        // Field exists in both schemas
                        rollbackSourceFields.Add(field.Name);
                    }
                    else
                    {
                        // Field was removed, skip it
                        continue;
                    }
                    
                    rollbackTargetFields.Add(field.Name);
                }
                
                rollbackSql.AppendLine($"INSERT INTO {tempTableName} ({string.Join(", ", rollbackTargetFields)})");
                rollbackSql.AppendLine($"SELECT {string.Join(", ", rollbackSourceFields)} FROM {tableName};");
                
                // Drop the current table and rename the rollback table
                rollbackSql.AppendLine($"-- Drop the current table and rename the rollback table");
                rollbackSql.AppendLine($"DROP TABLE {tableName};");
                rollbackSql.AppendLine($"ALTER TABLE {tempTableName} RENAME TO {tableName};");
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            return steps;
        }
        
        protected override async Task<List<DataTransformationStep>> GenerateDataTransformations(
            DataSchema oldSchema, 
            DataSchema newSchema, 
            List<SchemaChange> changes)
        {
            var transformations = new List<DataTransformationStep>();
            
            // Get the table name for this source
            var tableName = GetTableName(newSchema.SourceId);
            
            // Find field type changes that require data transformation
            var typeChanges = changes.Where(c => c.ChangeType == SchemaChangeType.ChangeType).ToList();
            
            foreach (var change in typeChanges)
            {
                var fieldName = change.FieldName;
                var oldType = change.OldValue.TryGetValue("Type", out var oldTypeObj) && oldTypeObj is FieldType oldFieldType
                    ? oldFieldType
                    : FieldType.String;
                
                var newType = change.NewValue.TryGetValue("Type", out var newTypeObj) && newTypeObj is FieldType newFieldType
                    ? newFieldType
                    : FieldType.String;
                
                // Generate transformation script based on type conversion
                var transformationScript = GenerateTypeConversionScript(tableName, fieldName, oldType, newType);
                var rollbackScript = GenerateTypeConversionScript(tableName, fieldName, newType, oldType);
                
                if (!string.IsNullOrEmpty(transformationScript))
                {
                    transformations.Add(new DataTransformationStep
                    {
                        Order = transformations.Count + 1,
                        Description = $"Transform data for field '{fieldName}' from {oldType} to {newType}",
                        FieldName = fieldName,
                        TransformationScript = transformationScript,
                        RollbackScript = rollbackScript
                    });
                }
            }
            
            // Check for custom migration rules in the schema fields
            if (oldSchema != null && newSchema != null)
            {
                foreach (var newField in newSchema.Fields)
                {
                    if (newField.EvolutionBehavior == FieldEvolutionBehavior.Custom && 
                        newField.MigrationRules != null && 
                        newField.MigrationRules.Any())
                    {
                        var oldField = oldSchema.Fields.FirstOrDefault(f => f.Name == newField.Name || f.Id == newField.PreviousFieldId);
                        
                        if (oldField != null)
                        {
                            // Generate custom transformation based on migration rules
                            var transformationScript = GenerateCustomTransformationScript(tableName, oldField, newField);
                            
                            if (!string.IsNullOrEmpty(transformationScript))
                            {
                                transformations.Add(new DataTransformationStep
                                {
                                    Order = transformations.Count + 1,
                                    Description = $"Apply custom transformation for field '{newField.Name}'",
                                    FieldName = newField.Name,
                                    TransformationScript = transformationScript,
                                    RollbackScript = "-- Custom transformations may not be reversible automatically"
                                });
                            }
                        }
                    }
                }
            }
            
            return transformations;
        }
        
        protected override async Task<string> GenerateRollbackScript(
            DataSchema oldSchema, 
            DataSchema newSchema, 
            List<SchemaChange> changes)
        {
            // For rollback, we'll use the rollback scripts from each migration step
            var rollbackBuilder = new StringBuilder();
            
            rollbackBuilder.AppendLine("-- Rollback script for schema migration");
            rollbackBuilder.AppendLine($"-- From schema {newSchema.Id} to {oldSchema?.Id}");
            rollbackBuilder.AppendLine();
            
            // Generate migration steps (which include rollback scripts)
            var steps = await GenerateMigrationSteps(oldSchema, newSchema, changes);
            
            // Add rollback scripts in reverse order
            foreach (var step in steps.OrderByDescending(s => s.Order))
            {
                rollbackBuilder.AppendLine($"-- Rollback for: {step.Description}");
                rollbackBuilder.AppendLine(step.RollbackScript);
                rollbackBuilder.AppendLine();
            }
            
            return rollbackBuilder.ToString();
        }
        
        private string GetTableName(string sourceId)
        {
            // Sanitize the source ID to create a valid table name
            var sanitized = new string(sourceId.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return $"data_{sanitized}";
        }
        
        private string GenerateTypeConversionScript(string tableName, string fieldName, FieldType fromType, FieldType toType)
        {
            var script = new StringBuilder();
            
            // Generate conversion SQL based on the types
            switch (toType)
            {
                case FieldType.String:
                    // Most types can be converted to string
                    script.AppendLine($"UPDATE {tableName} SET {fieldName} = CAST({fieldName} AS TEXT) WHERE {fieldName} IS NOT NULL;");
                    break;
                
                case FieldType.Integer:
                    if (fromType == FieldType.String || fromType == FieldType.Decimal || fromType == FieldType.Boolean)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN {fieldName} GLOB '[0-9]*' THEN CAST({fieldName} AS INTEGER)");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL;");
                    }
                    break;
                
                case FieldType.Decimal:
                    if (fromType == FieldType.String || fromType == FieldType.Integer)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN {fieldName} GLOB '[0-9]*.[0-9]*' OR {fieldName} GLOB '[0-9]*' THEN CAST({fieldName} AS REAL)");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL;");
                    }
                    break;
                
                case FieldType.Boolean:
                    if (fromType == FieldType.String || fromType == FieldType.Integer)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN {fieldName} IN ('true', 't', 'yes', 'y', '1') THEN 1");
                        script.AppendLine($"    WHEN {fieldName} IN ('false', 'f', 'no', 'n', '0') THEN 0");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL;");
                    }
                    break;
                
                case FieldType.DateTime:
                    if (fromType == FieldType.String)
                    {
                        script.AppendLine($"-- SQLite stores dates as TEXT, ISO8601 strings or Julian day numbers");
                        script.AppendLine($"-- No conversion needed if the string is already in a valid date format");
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = {fieldName} WHERE {fieldName} IS NOT NULL;");
                    }
                    break;
                
                case FieldType.Json:
                    if (fromType == FieldType.String || fromType == FieldType.Complex)
                    {
                        script.AppendLine($"-- SQLite stores JSON as TEXT");
                        script.AppendLine($"-- No conversion needed if the string is already valid JSON");
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = {fieldName} WHERE {fieldName} IS NOT NULL;");
                    }
                    break;
            }
            
            return script.ToString();
        }
        
        private string GenerateCustomTransformationScript(string tableName, SchemaField oldField, SchemaField newField)
        {
            var script = new StringBuilder();
            
            // Apply custom migration rules
            foreach (var rule in newField.MigrationRules)
            {
                var ruleType = rule.Key;
                var ruleValue = rule.Value;
                
                switch (ruleType.ToLowerInvariant())
                {
                    case "replace":
                        // Format: "oldValue:newValue"
                        var parts = ruleValue.Split(':');
                        if (parts.Length == 2)
                        {
                            var oldValue = parts[0];
                            var newValue = parts[1];
                            
                            script.AppendLine($"UPDATE {tableName} SET {newField.Name} = REPLACE({newField.Name}, '{oldValue}', '{newValue}') WHERE {newField.Name} IS NOT NULL;");
                        }
                        break;
                    
                    case "transform":
                        // Custom SQL transformation
                        script.AppendLine(ruleValue);
                        break;
                    
                    case "map":
                        // Format: "oldValue1=newValue1,oldValue2=newValue2"
                        var mappings = ruleValue.Split(',');
                        
                        script.AppendLine($"UPDATE {tableName} SET {newField.Name} = CASE");
                        
                        foreach (var mapping in mappings)
                        {
                            var mapParts = mapping.Split('=');
                            if (mapParts.Length == 2)
                            {
                                var oldValue = mapParts[0];
                                var newValue = mapParts[1];
                                
                                script.AppendLine($"    WHEN {newField.Name} = '{oldValue}' THEN '{newValue}'");
                            }
                        }
                        
                        script.AppendLine($"    ELSE {newField.Name}");
                        script.AppendLine($"END WHERE {newField.Name} IS NOT NULL;");
                        break;
                }
            }
            
            return script.ToString();
        }
    }
}
