using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution
{
    public class SchemaComparer
    {
        private readonly ILogger<SchemaComparer> _logger;
        
        public SchemaComparer(ILogger<SchemaComparer> logger)
        {
            _logger = logger;
        }
        
        public async Task<List<SchemaChange>> CompareSchemas(DataSchema oldSchema, DataSchema newSchema)
        {
            var changes = new List<SchemaChange>();
            
            try
            {
                // Check for null schemas
                if (oldSchema == null && newSchema == null)
                {
                    return changes;
                }
                
                if (oldSchema == null)
                {
                    // New schema is being created, all fields are added
                    foreach (var field in newSchema.Fields)
                    {
                        changes.Add(CreateAddFieldChange(field));
                    }
                    
                    return changes;
                }
                
                if (newSchema == null)
                {
                    // Schema is being deleted, all fields are removed
                    foreach (var field in oldSchema.Fields)
                    {
                        changes.Add(CreateRemoveFieldChange(field));
                    }
                    
                    return changes;
                }
                
                // Compare schema properties
                if (oldSchema.Type != newSchema.Type)
                {
                    changes.Add(new SchemaChange
                    {
                        ChangeType = SchemaChangeType.Other,
                        Description = $"Schema type changed from {oldSchema.Type} to {newSchema.Type}",
                        IsBreakingChange = true,
                        OldValue = new Dictionary<string, object> { { "Type", oldSchema.Type } },
                        NewValue = new Dictionary<string, object> { { "Type", newSchema.Type } }
                    });
                }
                
                // Compare fields
                // Find added fields
                foreach (var newField in newSchema.Fields)
                {
                    var oldField = oldSchema.Fields.FirstOrDefault(f => f.Name == newField.Name);
                    
                    if (oldField == null)
                    {
                        // Field was added
                        changes.Add(CreateAddFieldChange(newField));
                    }
                    else
                    {
                        // Field exists in both schemas, check for changes
                        changes.AddRange(CompareFields(oldField, newField));
                    }
                }
                
                // Find removed fields
                foreach (var oldField in oldSchema.Fields)
                {
                    var newField = newSchema.Fields.FirstOrDefault(f => f.Name == oldField.Name);
                    
                    if (newField == null)
                    {
                        // Field was removed
                        changes.Add(CreateRemoveFieldChange(oldField));
                    }
                }
                
                // Check for renamed fields (based on field IDs)
                foreach (var newField in newSchema.Fields.Where(f => !string.IsNullOrEmpty(f.PreviousFieldId)))
                {
                    var oldField = oldSchema.Fields.FirstOrDefault(f => f.Id == newField.PreviousFieldId);
                    
                    if (oldField != null && oldField.Name != newField.Name)
                    {
                        changes.Add(new SchemaChange
                        {
                            ChangeType = SchemaChangeType.RenameField,
                            FieldName = oldField.Name,
                            Description = $"Field renamed from '{oldField.Name}' to '{newField.Name}'",
                            IsBreakingChange = true,
                            OldValue = new Dictionary<string, object> { { "Name", oldField.Name } },
                            NewValue = new Dictionary<string, object> { { "Name", newField.Name } }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing schemas");
                throw;
            }
            
            return changes;
        }
        
        private List<SchemaChange> CompareFields(SchemaField oldField, SchemaField newField)
        {
            var changes = new List<SchemaChange>();
            
            // Check for type changes
            if (oldField.Type != newField.Type)
            {
                var isBreakingChange = IsTypeChangeBreaking(oldField.Type, newField.Type);
                
                changes.Add(new SchemaChange
                {
                    ChangeType = SchemaChangeType.ChangeType,
                    FieldName = oldField.Name,
                    Description = $"Field type changed from {oldField.Type} to {newField.Type}",
                    IsBreakingChange = isBreakingChange,
                    OldValue = new Dictionary<string, object> { { "Type", oldField.Type } },
                    NewValue = new Dictionary<string, object> { { "Type", newField.Type } }
                });
            }
            
            // Check for required changes
            if (oldField.IsRequired != newField.IsRequired)
            {
                var isBreakingChange = newField.IsRequired; // Making a field required is a breaking change
                
                changes.Add(new SchemaChange
                {
                    ChangeType = SchemaChangeType.ChangeRequired,
                    FieldName = oldField.Name,
                    Description = $"Field required changed from {oldField.IsRequired} to {newField.IsRequired}",
                    IsBreakingChange = isBreakingChange,
                    OldValue = new Dictionary<string, object> { { "IsRequired", oldField.IsRequired } },
                    NewValue = new Dictionary<string, object> { { "IsRequired", newField.IsRequired } }
                });
            }
            
            // Check for array changes
            if (oldField.IsArray != newField.IsArray)
            {
                changes.Add(new SchemaChange
                {
                    ChangeType = SchemaChangeType.ChangeType,
                    FieldName = oldField.Name,
                    Description = $"Field array changed from {oldField.IsArray} to {newField.IsArray}",
                    IsBreakingChange = true,
                    OldValue = new Dictionary<string, object> { { "IsArray", oldField.IsArray } },
                    NewValue = new Dictionary<string, object> { { "IsArray", newField.IsArray } }
                });
            }
            
            // Check for default value changes
            if (oldField.DefaultValue != newField.DefaultValue)
            {
                changes.Add(new SchemaChange
                {
                    ChangeType = SchemaChangeType.ChangeDefault,
                    FieldName = oldField.Name,
                    Description = $"Field default value changed from '{oldField.DefaultValue}' to '{newField.DefaultValue}'",
                    IsBreakingChange = false,
                    OldValue = new Dictionary<string, object> { { "DefaultValue", oldField.DefaultValue } },
                    NewValue = new Dictionary<string, object> { { "DefaultValue", newField.DefaultValue } }
                });
            }
            
            // Check for validation changes
            if (HasValidationChanged(oldField.Validation, newField.Validation))
            {
                var isBreakingChange = IsValidationChangeBreaking(oldField.Validation, newField.Validation);
                
                changes.Add(new SchemaChange
                {
                    ChangeType = SchemaChangeType.ChangeValidation,
                    FieldName = oldField.Name,
                    Description = "Field validation rules changed",
                    IsBreakingChange = isBreakingChange,
                    OldValue = CreateValidationDictionary(oldField.Validation),
                    NewValue = CreateValidationDictionary(newField.Validation)
                });
            }
            
            // Check for deprecation
            if (!oldField.IsDeprecated && newField.IsDeprecated)
            {
                changes.Add(new SchemaChange
                {
                    ChangeType = SchemaChangeType.DeprecateField,
                    FieldName = oldField.Name,
                    Description = $"Field '{oldField.Name}' has been deprecated",
                    IsBreakingChange = false,
                    OldValue = new Dictionary<string, object> { { "IsDeprecated", false } },
                    NewValue = new Dictionary<string, object> { { "IsDeprecated", true } }
                });
            }
            
            // Compare nested fields
            if (oldField.NestedFields != null && newField.NestedFields != null)
            {
                // Find added nested fields
                foreach (var newNestedField in newField.NestedFields)
                {
                    var oldNestedField = oldField.NestedFields.FirstOrDefault(f => f.Name == newNestedField.Name);
                    
                    if (oldNestedField == null)
                    {
                        // Nested field was added
                        changes.Add(new SchemaChange
                        {
                            ChangeType = SchemaChangeType.AddNestedField,
                            FieldName = $"{oldField.Name}.{newNestedField.Name}",
                            Description = $"Added nested field '{newNestedField.Name}' to '{oldField.Name}'",
                            IsBreakingChange = false,
                            NewValue = new Dictionary<string, object>
                            {
                                { "Name", newNestedField.Name },
                                { "Type", newNestedField.Type },
                                { "IsRequired", newNestedField.IsRequired }
                            }
                        });
                    }
                    else
                    {
                        // Nested field exists in both schemas, check for changes
                        var nestedChanges = CompareFields(oldNestedField, newNestedField);
                        
                        // Update field names to include parent field
                        foreach (var change in nestedChanges)
                        {
                            change.FieldName = $"{oldField.Name}.{change.FieldName}";
                        }
                        
                        changes.AddRange(nestedChanges);
                    }
                }
                
                // Find removed nested fields
                foreach (var oldNestedField in oldField.NestedFields)
                {
                    var newNestedField = newField.NestedFields.FirstOrDefault(f => f.Name == oldNestedField.Name);
                    
                    if (newNestedField == null)
                    {
                        // Nested field was removed
                        changes.Add(new SchemaChange
                        {
                            ChangeType = SchemaChangeType.RemoveNestedField,
                            FieldName = $"{oldField.Name}.{oldNestedField.Name}",
                            Description = $"Removed nested field '{oldNestedField.Name}' from '{oldField.Name}'",
                            IsBreakingChange = true,
                            OldValue = new Dictionary<string, object>
                            {
                                { "Name", oldNestedField.Name },
                                { "Type", oldNestedField.Type },
                                { "IsRequired", oldNestedField.IsRequired }
                            }
                        });
                    }
                }
            }
            
            return changes;
        }
        
        private SchemaChange CreateAddFieldChange(SchemaField field)
        {
            return new SchemaChange
            {
                ChangeType = SchemaChangeType.AddField,
                FieldName = field.Name,
                Description = $"Added field '{field.Name}' of type {field.Type}",
                IsBreakingChange = field.IsRequired && string.IsNullOrEmpty(field.DefaultValue),
                NewValue = new Dictionary<string, object>
                {
                    { "Name", field.Name },
                    { "Type", field.Type },
                    { "IsRequired", field.IsRequired },
                    { "IsArray", field.IsArray },
                    { "DefaultValue", field.DefaultValue }
                }
            };
        }
        
        private SchemaChange CreateRemoveFieldChange(SchemaField field)
        {
            return new SchemaChange
            {
                ChangeType = SchemaChangeType.RemoveField,
                FieldName = field.Name,
                Description = $"Removed field '{field.Name}' of type {field.Type}",
                IsBreakingChange = true,
                OldValue = new Dictionary<string, object>
                {
                    { "Name", field.Name },
                    { "Type", field.Type },
                    { "IsRequired", field.IsRequired },
                    { "IsArray", field.IsArray },
                    { "DefaultValue", field.DefaultValue }
                }
            };
        }
        
        private bool IsTypeChangeBreaking(FieldType oldType, FieldType newType)
        {
            // Define compatible type changes
            var compatibleChanges = new Dictionary<FieldType, HashSet<FieldType>>
            {
                { FieldType.String, new HashSet<FieldType> { FieldType.Enum } },
                { FieldType.Integer, new HashSet<FieldType> { FieldType.Decimal } },
                { FieldType.Decimal, new HashSet<FieldType> { FieldType.String } },
                { FieldType.Boolean, new HashSet<FieldType> { FieldType.String, FieldType.Integer } },
                { FieldType.DateTime, new HashSet<FieldType> { FieldType.String } },
                { FieldType.Json, new HashSet<FieldType> { FieldType.String, FieldType.Complex } },
                { FieldType.Complex, new HashSet<FieldType> { FieldType.Json } },
                { FieldType.Binary, new HashSet<FieldType> { FieldType.String } },
                { FieldType.Enum, new HashSet<FieldType> { FieldType.String } },
                { FieldType.Reference, new HashSet<FieldType> { FieldType.String } },
                { FieldType.Geometry, new HashSet<FieldType> { FieldType.String, FieldType.Json } },
                { FieldType.Array, new HashSet<FieldType> { FieldType.Json } },
                { FieldType.Map, new HashSet<FieldType> { FieldType.Json, FieldType.Complex } }
            };
            
            // Check if the type change is compatible
            if (compatibleChanges.TryGetValue(oldType, out var compatibleTypes))
            {
                return !compatibleTypes.Contains(newType);
            }
            
            // Default to breaking change
            return true;
        }
        
        private bool HasValidationChanged(ValidationRules oldValidation, ValidationRules newValidation)
        {
            // Handle null cases
            if (oldValidation == null && newValidation == null)
            {
                return false;
            }
            
            if (oldValidation == null || newValidation == null)
            {
                return true;
            }
            
            // Compare validation properties
            if (oldValidation.MinLength != newValidation.MinLength ||
                oldValidation.MaxLength != newValidation.MaxLength ||
                oldValidation.Pattern != newValidation.Pattern ||
                oldValidation.MinValue != newValidation.MinValue ||
                oldValidation.MaxValue != newValidation.MaxValue ||
                oldValidation.CustomValidation != newValidation.CustomValidation)
            {
                return true;
            }
            
            // Compare allowed values
            if ((oldValidation.AllowedValues == null) != (newValidation.AllowedValues == null))
            {
                return true;
            }
            
            if (oldValidation.AllowedValues != null && newValidation.AllowedValues != null)
            {
                if (oldValidation.AllowedValues.Length != newValidation.AllowedValues.Length)
                {
                    return true;
                }
                
                for (int i = 0; i < oldValidation.AllowedValues.Length; i++)
                {
                    if (oldValidation.AllowedValues[i] != newValidation.AllowedValues[i])
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private bool IsValidationChangeBreaking(ValidationRules oldValidation, ValidationRules newValidation)
        {
            // Handle null cases
            if (oldValidation == null && newValidation == null)
            {
                return false;
            }
            
            if (oldValidation == null)
            {
                // Adding validation is potentially breaking
                return true;
            }
            
            if (newValidation == null)
            {
                // Removing validation is not breaking
                return false;
            }
            
            // Check for breaking validation changes
            
            // More restrictive min length
            if (newValidation.MinLength > oldValidation.MinLength)
            {
                return true;
            }
            
            // More restrictive max length
            if (oldValidation.MaxLength.HasValue && newValidation.MaxLength.HasValue && 
                newValidation.MaxLength < oldValidation.MaxLength)
            {
                return true;
            }
            
            // Changed pattern
            if (!string.IsNullOrEmpty(oldValidation.Pattern) && 
                !string.IsNullOrEmpty(newValidation.Pattern) && 
                oldValidation.Pattern != newValidation.Pattern)
            {
                return true;
            }
            
            // More restrictive min value
            if (oldValidation.MinValue.HasValue && newValidation.MinValue.HasValue && 
                newValidation.MinValue > oldValidation.MinValue)
            {
                return true;
            }
            
            // More restrictive max value
            if (oldValidation.MaxValue.HasValue && newValidation.MaxValue.HasValue && 
                newValidation.MaxValue < oldValidation.MaxValue)
            {
                return true;
            }
            
            // Changed allowed values
            if (oldValidation.AllowedValues != null && newValidation.AllowedValues != null)
            {
                var oldValues = new HashSet<string>(oldValidation.AllowedValues);
                var newValues = new HashSet<string>(newValidation.AllowedValues);
                
                // If any old values are no longer allowed, it's a breaking change
                if (oldValues.Except(newValues).Any())
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private Dictionary<string, object> CreateValidationDictionary(ValidationRules validation)
        {
            if (validation == null)
            {
                return new Dictionary<string, object>();
            }
            
            var result = new Dictionary<string, object>();
            
            if (validation.MinLength.HasValue)
            {
                result["MinLength"] = validation.MinLength.Value;
            }
            
            if (validation.MaxLength.HasValue)
            {
                result["MaxLength"] = validation.MaxLength.Value;
            }
            
            if (!string.IsNullOrEmpty(validation.Pattern))
            {
                result["Pattern"] = validation.Pattern;
            }
            
            if (validation.MinValue.HasValue)
            {
                result["MinValue"] = validation.MinValue.Value;
            }
            
            if (validation.MaxValue.HasValue)
            {
                result["MaxValue"] = validation.MaxValue.Value;
            }
            
            if (validation.AllowedValues != null)
            {
                result["AllowedValues"] = string.Join(", ", validation.AllowedValues);
            }
            
            if (!string.IsNullOrEmpty(validation.CustomValidation))
            {
                result["CustomValidation"] = validation.CustomValidation;
            }
            
            return result;
        }
    }
}
