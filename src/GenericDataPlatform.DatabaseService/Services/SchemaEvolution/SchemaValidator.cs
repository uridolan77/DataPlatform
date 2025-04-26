using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution
{
    public class SchemaValidator
    {
        private readonly SchemaComparer _schemaComparer;
        private readonly ILogger<SchemaValidator> _logger;
        
        public SchemaValidator(SchemaComparer schemaComparer, ILogger<SchemaValidator> logger)
        {
            _schemaComparer = schemaComparer;
            _logger = logger;
        }
        
        public async Task<SchemaValidationResult> ValidateSchemaCompatibility(DataSchema oldSchema, DataSchema newSchema)
        {
            var result = new SchemaValidationResult
            {
                IsValid = true,
                HasBreakingChanges = false,
                RequiresMigration = false
            };
            
            try
            {
                // If both schemas are null, they're compatible
                if (oldSchema == null && newSchema == null)
                {
                    return result;
                }
                
                // If old schema is null, this is a new schema, so it's valid
                if (oldSchema == null)
                {
                    return result;
                }
                
                // If new schema is null, this is a schema deletion, which is a breaking change
                if (newSchema == null)
                {
                    result.IsValid = false;
                    result.HasBreakingChanges = true;
                    result.Issues.Add(new SchemaValidationIssue
                    {
                        Type = SchemaValidationIssueType.Other,
                        Message = "Schema is being deleted",
                        IsBreakingChange = true
                    });
                    
                    return result;
                }
                
                // Check evolution strategy compatibility
                if (!IsEvolutionStrategyCompatible(oldSchema.EvolutionStrategy, newSchema.EvolutionStrategy))
                {
                    result.Issues.Add(new SchemaValidationIssue
                    {
                        Type = SchemaValidationIssueType.IncompatibleEvolutionStrategy,
                        Message = $"Evolution strategy changed from {oldSchema.EvolutionStrategy} to {newSchema.EvolutionStrategy}",
                        IsBreakingChange = true
                    });
                    
                    result.IsValid = false;
                    result.HasBreakingChanges = true;
                }
                
                // Get schema changes
                var changes = await _schemaComparer.CompareSchemas(oldSchema, newSchema);
                
                // Check for breaking changes
                var breakingChanges = changes.Where(c => c.IsBreakingChange).ToList();
                
                if (breakingChanges.Any())
                {
                    result.HasBreakingChanges = true;
                    
                    // Check if breaking changes are allowed based on evolution strategy
                    if (newSchema.EvolutionStrategy == EvolutionStrategy.Additive || 
                        newSchema.EvolutionStrategy == EvolutionStrategy.NoEvolution)
                    {
                        result.IsValid = false;
                        
                        foreach (var change in breakingChanges)
                        {
                            result.Issues.Add(CreateValidationIssueFromChange(change));
                        }
                    }
                    else
                    {
                        // Breaking changes are allowed, but require migration
                        result.RequiresMigration = true;
                    }
                }
                
                // Check for non-breaking changes that require migration
                var nonBreakingChanges = changes.Where(c => !c.IsBreakingChange).ToList();
                
                if (nonBreakingChanges.Any())
                {
                    // Check if any changes require migration
                    var changesRequiringMigration = nonBreakingChanges.Where(c => 
                        c.ChangeType == SchemaChangeType.AddField && 
                        c.NewValue.TryGetValue("IsRequired", out var isRequired) && 
                        isRequired is bool isRequiredBool && isRequiredBool).ToList();
                    
                    if (changesRequiringMigration.Any())
                    {
                        result.RequiresMigration = true;
                    }
                }
                
                // Validate field-specific evolution behaviors
                foreach (var field in oldSchema.Fields)
                {
                    var newField = newSchema.Fields.FirstOrDefault(f => f.Name == field.Name);
                    
                    // Skip fields that have been removed
                    if (newField == null)
                    {
                        continue;
                    }
                    
                    // Check immutable fields
                    if (field.EvolutionBehavior == FieldEvolutionBehavior.Immutable)
                    {
                        var fieldChanges = changes.Where(c => c.FieldName == field.Name).ToList();
                        
                        if (fieldChanges.Any())
                        {
                            result.IsValid = false;
                            result.Issues.Add(new SchemaValidationIssue
                            {
                                Type = SchemaValidationIssueType.Other,
                                Message = $"Field '{field.Name}' is marked as immutable but has changes",
                                FieldName = field.Name,
                                IsBreakingChange = true
                            });
                        }
                    }
                    
                    // Check deprecated fields
                    if (field.EvolutionBehavior == FieldEvolutionBehavior.Deprecated && 
                        newField.EvolutionBehavior != FieldEvolutionBehavior.Deprecated)
                    {
                        result.Issues.Add(new SchemaValidationIssue
                        {
                            Type = SchemaValidationIssueType.Other,
                            Message = $"Field '{field.Name}' was deprecated but is no longer marked as deprecated",
                            FieldName = field.Name,
                            IsBreakingChange = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating schema compatibility");
                
                result.IsValid = false;
                result.Issues.Add(new SchemaValidationIssue
                {
                    Type = SchemaValidationIssueType.Other,
                    Message = $"Error validating schema: {ex.Message}",
                    IsBreakingChange = true
                });
            }
            
            return result;
        }
        
        private bool IsEvolutionStrategyCompatible(EvolutionStrategy oldStrategy, EvolutionStrategy newStrategy)
        {
            // NoEvolution can't be changed to anything else
            if (oldStrategy == EvolutionStrategy.NoEvolution && newStrategy != EvolutionStrategy.NoEvolution)
            {
                return false;
            }
            
            // Additive can be changed to NonBreaking or FullEvolution
            if (oldStrategy == EvolutionStrategy.Additive && 
                (newStrategy == EvolutionStrategy.NonBreaking || newStrategy == EvolutionStrategy.FullEvolution))
            {
                return true;
            }
            
            // NonBreaking can be changed to FullEvolution
            if (oldStrategy == EvolutionStrategy.NonBreaking && newStrategy == EvolutionStrategy.FullEvolution)
            {
                return true;
            }
            
            // FullEvolution can't be changed to a more restrictive strategy
            if (oldStrategy == EvolutionStrategy.FullEvolution && newStrategy != EvolutionStrategy.FullEvolution)
            {
                return false;
            }
            
            // Same strategy is always compatible
            return oldStrategy == newStrategy;
        }
        
        private SchemaValidationIssue CreateValidationIssueFromChange(SchemaChange change)
        {
            var issueType = SchemaValidationIssueType.Other;
            
            switch (change.ChangeType)
            {
                case SchemaChangeType.RemoveField:
                    issueType = SchemaValidationIssueType.FieldRemoved;
                    break;
                
                case SchemaChangeType.ChangeType:
                    issueType = SchemaValidationIssueType.FieldTypeChanged;
                    break;
                
                case SchemaChangeType.ChangeRequired:
                    issueType = SchemaValidationIssueType.RequiredAdded;
                    break;
                
                case SchemaChangeType.ChangeValidation:
                    issueType = SchemaValidationIssueType.ValidationChanged;
                    break;
            }
            
            return new SchemaValidationIssue
            {
                Type = issueType,
                Message = change.Description,
                FieldName = change.FieldName,
                IsBreakingChange = change.IsBreakingChange
            };
        }
    }
}
