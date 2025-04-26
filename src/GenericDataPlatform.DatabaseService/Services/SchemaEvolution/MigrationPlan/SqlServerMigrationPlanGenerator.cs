using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution.MigrationPlan
{
    public class SqlServerMigrationPlanGenerator : BaseMigrationPlanGenerator
    {
        public SqlServerMigrationPlanGenerator(SchemaComparer schemaComparer, ILogger<SqlServerMigrationPlanGenerator> logger)
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
            
            // Process add field changes
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
                    
                    // In SQL Server, we need to handle NULL/NOT NULL and DEFAULT in the ADD COLUMN statement
                    sql.Append($"ALTER TABLE {tableName} ADD {fieldName} {GetSqlTypeForFieldType(fieldType, DatabaseType.SQLServer)}");
                    
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        sql.Append($" DEFAULT '{defaultValue}'");
                    }
                    
                    if (isRequired)
                    {
                        sql.Append(" NOT NULL");
                    }
                    else
                    {
                        sql.Append(" NULL");
                    }
                    
                    sql.AppendLine(";");
                    
                    rollbackSql.AppendLine($"ALTER TABLE {tableName} DROP COLUMN {fieldName};");
                }
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            // Process remove field changes
            if (removeFieldChanges.Any())
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = $"Remove {removeFieldChanges.Count} field(s)",
                    IsBreakingChange = true
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                foreach (var change in removeFieldChanges)
                {
                    var fieldName = change.FieldName;
                    var fieldType = change.OldValue.TryGetValue("Type", out var typeObj) && typeObj is FieldType type
                        ? type
                        : FieldType.String;
                    
                    var isRequired = change.OldValue.TryGetValue("IsRequired", out var requiredObj) && requiredObj is bool required && required;
                    var defaultValue = change.OldValue.TryGetValue("DefaultValue", out var defaultObj) ? defaultObj?.ToString() : null;
                    
                    // Check if there's a default constraint to drop first
                    sql.AppendLine($"DECLARE @constraintName NVARCHAR(200);");
                    sql.AppendLine($"SELECT @constraintName = dc.name FROM sys.default_constraints dc");
                    sql.AppendLine($"JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id");
                    sql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(dc.parent_object_id) = '{tableName}';");
                    sql.AppendLine($"IF @constraintName IS NOT NULL");
                    sql.AppendLine($"    EXEC('ALTER TABLE {tableName} DROP CONSTRAINT ' + @constraintName);");
                    
                    sql.AppendLine($"ALTER TABLE {tableName} DROP COLUMN {fieldName};");
                    
                    // Rollback: add the column back
                    rollbackSql.Append($"ALTER TABLE {tableName} ADD {fieldName} {GetSqlTypeForFieldType(fieldType, DatabaseType.SQLServer)}");
                    
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        rollbackSql.Append($" DEFAULT '{defaultValue}'");
                    }
                    
                    if (isRequired)
                    {
                        rollbackSql.Append(" NOT NULL");
                    }
                    else
                    {
                        rollbackSql.Append(" NULL");
                    }
                    
                    rollbackSql.AppendLine(";");
                }
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            // Process rename field changes
            if (renameFieldChanges.Any())
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = $"Rename {renameFieldChanges.Count} field(s)",
                    IsBreakingChange = true
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                foreach (var change in renameFieldChanges)
                {
                    var oldFieldName = change.FieldName;
                    var newFieldName = change.NewValue.TryGetValue("Name", out var nameObj) ? nameObj?.ToString() : null;
                    
                    if (string.IsNullOrEmpty(newFieldName))
                    {
                        continue;
                    }
                    
                    sql.AppendLine($"EXEC sp_rename '{tableName}.{oldFieldName}', '{newFieldName}', 'COLUMN';");
                    rollbackSql.AppendLine($"EXEC sp_rename '{tableName}.{newFieldName}', '{oldFieldName}', 'COLUMN';");
                }
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            // Process change type changes
            if (changeTypeChanges.Any())
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = $"Change type of {changeTypeChanges.Count} field(s)",
                    IsBreakingChange = changeTypeChanges.Any(c => c.IsBreakingChange)
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                foreach (var change in changeTypeChanges)
                {
                    var fieldName = change.FieldName;
                    var oldType = change.OldValue.TryGetValue("Type", out var oldTypeObj) && oldTypeObj is FieldType oldFieldType
                        ? oldFieldType
                        : FieldType.String;
                    
                    var newType = change.NewValue.TryGetValue("Type", out var newTypeObj) && newTypeObj is FieldType newFieldType
                        ? newFieldType
                        : FieldType.String;
                    
                    var oldSqlType = GetSqlTypeForFieldType(oldType, DatabaseType.SQLServer);
                    var newSqlType = GetSqlTypeForFieldType(newType, DatabaseType.SQLServer);
                    
                    // In SQL Server, we need to check for constraints before altering the column
                    sql.AppendLine($"DECLARE @constraintName NVARCHAR(200);");
                    sql.AppendLine($"SELECT @constraintName = dc.name FROM sys.default_constraints dc");
                    sql.AppendLine($"JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id");
                    sql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(dc.parent_object_id) = '{tableName}';");
                    sql.AppendLine($"IF @constraintName IS NOT NULL");
                    sql.AppendLine($"    EXEC('ALTER TABLE {tableName} DROP CONSTRAINT ' + @constraintName);");
                    
                    // Get nullability
                    sql.AppendLine($"DECLARE @isNullable BIT;");
                    sql.AppendLine($"SELECT @isNullable = c.is_nullable FROM sys.columns c");
                    sql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(c.object_id) = '{tableName}';");
                    
                    // Alter the column
                    sql.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} {newSqlType} " + 
                                  $"+ CASE WHEN @isNullable = 1 THEN ' NULL' ELSE ' NOT NULL' END;");
                    
                    // Rollback: change back to original type
                    rollbackSql.AppendLine($"DECLARE @constraintName NVARCHAR(200);");
                    rollbackSql.AppendLine($"SELECT @constraintName = dc.name FROM sys.default_constraints dc");
                    rollbackSql.AppendLine($"JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id");
                    rollbackSql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(dc.parent_object_id) = '{tableName}';");
                    rollbackSql.AppendLine($"IF @constraintName IS NOT NULL");
                    rollbackSql.AppendLine($"    EXEC('ALTER TABLE {tableName} DROP CONSTRAINT ' + @constraintName);");
                    
                    rollbackSql.AppendLine($"DECLARE @isNullable BIT;");
                    rollbackSql.AppendLine($"SELECT @isNullable = c.is_nullable FROM sys.columns c");
                    rollbackSql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(c.object_id) = '{tableName}';");
                    
                    rollbackSql.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} {oldSqlType} " + 
                                         $"+ CASE WHEN @isNullable = 1 THEN ' NULL' ELSE ' NOT NULL' END;");
                }
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            // Process change required changes
            if (changeRequiredChanges.Any())
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = $"Change required constraint for {changeRequiredChanges.Count} field(s)",
                    IsBreakingChange = changeRequiredChanges.Any(c => c.IsBreakingChange)
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                foreach (var change in changeRequiredChanges)
                {
                    var fieldName = change.FieldName;
                    var oldRequired = change.OldValue.TryGetValue("IsRequired", out var oldRequiredObj) && oldRequiredObj is bool oldRequiredBool && oldRequiredBool;
                    var newRequired = change.NewValue.TryGetValue("IsRequired", out var newRequiredObj) && newRequiredObj is bool newRequiredBool && newRequiredBool;
                    
                    // Get the current type
                    sql.AppendLine($"DECLARE @dataType NVARCHAR(128);");
                    sql.AppendLine($"SELECT @dataType = TYPE_NAME(c.user_type_id) + ");
                    sql.AppendLine($"    CASE WHEN TYPE_NAME(c.user_type_id) IN ('varchar', 'nvarchar', 'char', 'nchar') ");
                    sql.AppendLine($"        THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS VARCHAR(10)) END + ')' ");
                    sql.AppendLine($"    WHEN TYPE_NAME(c.user_type_id) IN ('decimal', 'numeric') ");
                    sql.AppendLine($"        THEN '(' + CAST(c.precision AS VARCHAR(10)) + ',' + CAST(c.scale AS VARCHAR(10)) + ')' ");
                    sql.AppendLine($"    ELSE '' END");
                    sql.AppendLine($"FROM sys.columns c");
                    sql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(c.object_id) = '{tableName}';");
                    
                    if (newRequired && !oldRequired)
                    {
                        // Field is now required
                        sql.AppendLine($"-- Ensure there are no NULL values before setting NOT NULL constraint");
                        sql.AppendLine($"UPDATE {tableName} SET {fieldName} = '' WHERE {fieldName} IS NULL;");
                        sql.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} @dataType NOT NULL;");
                        
                        rollbackSql.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} @dataType NULL;");
                    }
                    else if (!newRequired && oldRequired)
                    {
                        // Field is no longer required
                        sql.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} @dataType NULL;");
                        
                        rollbackSql.AppendLine($"-- Ensure there are no NULL values before setting NOT NULL constraint");
                        rollbackSql.AppendLine($"UPDATE {tableName} SET {fieldName} = '' WHERE {fieldName} IS NULL;");
                        rollbackSql.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {fieldName} @dataType NOT NULL;");
                    }
                }
                
                step.SqlScript = sql.ToString();
                step.RollbackScript = rollbackSql.ToString();
                
                steps.Add(step);
            }
            
            // Process change default changes
            if (changeDefaultChanges.Any())
            {
                var step = new SchemaMigrationStep
                {
                    Order = steps.Count + 1,
                    Description = $"Change default value for {changeDefaultChanges.Count} field(s)",
                    IsBreakingChange = false
                };
                
                var sql = new StringBuilder();
                var rollbackSql = new StringBuilder();
                
                foreach (var change in changeDefaultChanges)
                {
                    var fieldName = change.FieldName;
                    var oldDefault = change.OldValue.TryGetValue("DefaultValue", out var oldDefaultObj) ? oldDefaultObj?.ToString() : null;
                    var newDefault = change.NewValue.TryGetValue("DefaultValue", out var newDefaultObj) ? newDefaultObj?.ToString() : null;
                    
                    // Drop existing default constraint if any
                    sql.AppendLine($"DECLARE @constraintName NVARCHAR(200);");
                    sql.AppendLine($"SELECT @constraintName = dc.name FROM sys.default_constraints dc");
                    sql.AppendLine($"JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id");
                    sql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(dc.parent_object_id) = '{tableName}';");
                    sql.AppendLine($"IF @constraintName IS NOT NULL");
                    sql.AppendLine($"    EXEC('ALTER TABLE {tableName} DROP CONSTRAINT ' + @constraintName);");
                    
                    // Add new default constraint if specified
                    if (!string.IsNullOrEmpty(newDefault))
                    {
                        sql.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT DF_{tableName}_{fieldName} DEFAULT '{newDefault}' FOR {fieldName};");
                    }
                    
                    // Rollback: drop new constraint and add back old one if needed
                    rollbackSql.AppendLine($"DECLARE @constraintName NVARCHAR(200);");
                    rollbackSql.AppendLine($"SELECT @constraintName = dc.name FROM sys.default_constraints dc");
                    rollbackSql.AppendLine($"JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id");
                    rollbackSql.AppendLine($"WHERE c.name = '{fieldName}' AND OBJECT_NAME(dc.parent_object_id) = '{tableName}';");
                    rollbackSql.AppendLine($"IF @constraintName IS NOT NULL");
                    rollbackSql.AppendLine($"    EXEC('ALTER TABLE {tableName} DROP CONSTRAINT ' + @constraintName);");
                    
                    if (!string.IsNullOrEmpty(oldDefault))
                    {
                        rollbackSql.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT DF_{tableName}_{fieldName} DEFAULT '{oldDefault}' FOR {fieldName};");
                    }
                }
                
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
            
            // Find renamed fields that need data copying
            var renameChanges = changes.Where(c => c.ChangeType == SchemaChangeType.RenameField).ToList();
            
            foreach (var change in renameChanges)
            {
                var oldFieldName = change.FieldName;
                var newFieldName = change.NewValue.TryGetValue("Name", out var nameObj) ? nameObj?.ToString() : null;
                
                if (string.IsNullOrEmpty(newFieldName))
                {
                    continue;
                }
                
                // For SQL Server, we use sp_rename which doesn't require data copying
                // But we'll add a transformation step for documentation purposes
                transformations.Add(new DataTransformationStep
                {
                    Order = transformations.Count + 1,
                    Description = $"Field '{oldFieldName}' renamed to '{newFieldName}'",
                    FieldName = oldFieldName,
                    TransformationScript = $"-- Field renamed using EXEC sp_rename '{tableName}.{oldFieldName}', '{newFieldName}', 'COLUMN'",
                    RollbackScript = $"-- Field renamed using EXEC sp_rename '{tableName}.{newFieldName}', '{oldFieldName}', 'COLUMN'"
                });
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
                    script.AppendLine($"UPDATE {tableName} SET {fieldName} = CAST({fieldName} AS NVARCHAR(MAX)) WHERE {fieldName} IS NOT NULL");
                    break;
                
                case FieldType.Integer:
                    if (fromType == FieldType.String || fromType == FieldType.Decimal || fromType == FieldType.Boolean)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN ISNUMERIC({fieldName}) = 1 THEN CAST({fieldName} AS INT)");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL");
                    }
                    break;
                
                case FieldType.Decimal:
                    if (fromType == FieldType.String || fromType == FieldType.Integer)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN ISNUMERIC({fieldName}) = 1 THEN CAST({fieldName} AS DECIMAL(18, 6))");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL");
                    }
                    break;
                
                case FieldType.Boolean:
                    if (fromType == FieldType.String || fromType == FieldType.Integer)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN {fieldName} IN ('true', 't', 'yes', 'y', '1', 1) THEN 1");
                        script.AppendLine($"    WHEN {fieldName} IN ('false', 'f', 'no', 'n', '0', 0) THEN 0");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL");
                    }
                    break;
                
                case FieldType.DateTime:
                    if (fromType == FieldType.String)
                    {
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = CASE");
                        script.AppendLine($"    WHEN ISDATE({fieldName}) = 1 THEN CAST({fieldName} AS DATETIME2)");
                        script.AppendLine($"    ELSE NULL");
                        script.AppendLine($"END WHERE {fieldName} IS NOT NULL");
                    }
                    break;
                
                case FieldType.Json:
                    if (fromType == FieldType.String || fromType == FieldType.Complex)
                    {
                        script.AppendLine($"-- SQL Server doesn't have native JSON validation");
                        script.AppendLine($"-- This is a best-effort transformation");
                        script.AppendLine($"UPDATE {tableName} SET {fieldName} = {fieldName} WHERE {fieldName} IS NOT NULL");
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
                            
                            script.AppendLine($"UPDATE {tableName} SET {newField.Name} = REPLACE({newField.Name}, '{oldValue}', '{newValue}') WHERE {newField.Name} IS NOT NULL");
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
                        script.AppendLine($"END WHERE {newField.Name} IS NOT NULL");
                        break;
                }
            }
            
            return script.ToString();
        }
    }
}
