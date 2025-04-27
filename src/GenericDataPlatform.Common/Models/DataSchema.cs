using System;
using System.Collections.Generic;
using System.Linq;

namespace GenericDataPlatform.Common.Models
{
    public class DataSchema
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SchemaType Type { get; set; }
        public List<SchemaField> Fields { get; set; } = new List<SchemaField>();
        public SchemaVersion Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Schema evolution properties
        public string SourceId { get; set; }
        public List<string> PreviousVersionIds { get; set; } = new List<string>();
        public EvolutionStrategy EvolutionStrategy { get; set; } = EvolutionStrategy.Additive;
        public bool IsDeprecated { get; set; }
        public DateTime? DeprecatedAt { get; set; }

        // Helper methods for schema evolution
        public SchemaField GetField(string fieldName)
        {
            return Fields.FirstOrDefault(f => f.Name == fieldName);
        }

        public bool HasField(string fieldName)
        {
            return Fields.Any(f => f.Name == fieldName);
        }

        // Create a deep clone of the schema
        public DataSchema Clone()
        {
            var clone = new DataSchema
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Type = Type,
                Version = Version?.Clone(),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                SourceId = SourceId,
                EvolutionStrategy = EvolutionStrategy,
                IsDeprecated = IsDeprecated,
                DeprecatedAt = DeprecatedAt
            };

            // Clone the fields
            clone.Fields = Fields.Select(f => f.Clone()).ToList();

            // Clone the previous version IDs
            if (PreviousVersionIds != null)
            {
                clone.PreviousVersionIds = new List<string>(PreviousVersionIds);
            }

            return clone;
        }
    }

    public enum SchemaType
    {
        Strict,      // Enforce schema validation
        Flexible,    // Allow additional fields
        Dynamic      // Infer schema from data
    }

    public enum EvolutionStrategy
    {
        Additive,           // Only allow adding new fields
        NonBreaking,        // Allow adding fields and making non-breaking changes
        FullEvolution,      // Allow all changes with migration
        NoEvolution         // No changes allowed
    }

    public class SchemaField
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public FieldType Type { get; set; }
        public bool IsRequired { get; set; }
        public bool IsArray { get; set; }
        public string DefaultValue { get; set; }
        public ValidationRules Validation { get; set; }
        public List<SchemaField> NestedFields { get; set; } = new List<SchemaField>();

        // Schema evolution properties
        public bool IsDeprecated { get; set; }
        public DateTime? DeprecatedAt { get; set; }
        public string ReplacedByFieldId { get; set; }
        public string PreviousFieldId { get; set; }
        public FieldEvolutionBehavior EvolutionBehavior { get; set; } = FieldEvolutionBehavior.Standard;
        public Dictionary<string, string> MigrationRules { get; set; } = new Dictionary<string, string>();

        // Create a deep clone of the field
        public SchemaField Clone()
        {
            var clone = new SchemaField
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Type = Type,
                IsRequired = IsRequired,
                IsArray = IsArray,
                DefaultValue = DefaultValue,
                IsDeprecated = IsDeprecated,
                DeprecatedAt = DeprecatedAt,
                ReplacedByFieldId = ReplacedByFieldId,
                PreviousFieldId = PreviousFieldId,
                EvolutionBehavior = EvolutionBehavior
            };

            // Clone validation rules
            if (Validation != null)
            {
                clone.Validation = Validation.Clone();
            }

            // Clone nested fields
            if (NestedFields != null)
            {
                clone.NestedFields = NestedFields.Select(f => f.Clone()).ToList();
            }

            // Clone migration rules
            if (MigrationRules != null)
            {
                clone.MigrationRules = new Dictionary<string, string>(MigrationRules);
            }

            return clone;
        }
    }

    public enum FieldEvolutionBehavior
    {
        Standard,           // Follow schema evolution strategy
        Immutable,          // Field cannot be changed
        Deprecated,         // Field is deprecated and will be removed
        Custom              // Custom migration rules apply
    }

    public enum FieldType
    {
        String,
        Integer,
        Decimal,
        Boolean,
        DateTime,
        Json,
        Complex,
        Binary,
        // Extended types for schema evolution
        Enum,
        Reference,
        Geometry,
        Array,
        Map
    }

    public class ValidationRules
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string Pattern { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string[] AllowedValues { get; set; }
        public string CustomValidation { get; set; } // Expression or reference to validation function
        public int? Precision { get; set; } // For decimal/numeric types
        public int? Scale { get; set; } // For decimal/numeric types

        // Create a deep clone of the validation rules
        public ValidationRules Clone()
        {
            var clone = new ValidationRules
            {
                MinLength = MinLength,
                MaxLength = MaxLength,
                Pattern = Pattern,
                MinValue = MinValue,
                MaxValue = MaxValue,
                CustomValidation = CustomValidation,
                Precision = Precision,
                Scale = Scale
            };

            // Clone allowed values
            if (AllowedValues != null)
            {
                clone.AllowedValues = new string[AllowedValues.Length];
                Array.Copy(AllowedValues, clone.AllowedValues, AllowedValues.Length);
            }

            return clone;
        }
    }

    public class SchemaVersion
    {
        public string VersionNumber { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string PreviousVersion { get; set; }
        public string ChangeDescription { get; set; }
        public List<SchemaChange> Changes { get; set; } = new List<SchemaChange>();
        public string Author { get; set; }
        public bool IsCompatible { get; set; } = true;

        // Create a deep clone of the schema version
        public SchemaVersion Clone()
        {
            var clone = new SchemaVersion
            {
                VersionNumber = VersionNumber,
                EffectiveDate = EffectiveDate,
                PreviousVersion = PreviousVersion,
                ChangeDescription = ChangeDescription,
                Author = Author,
                IsCompatible = IsCompatible
            };

            // Clone changes
            if (Changes != null)
            {
                clone.Changes = Changes.Select(c => c.Clone()).ToList();
            }

            return clone;
        }
    }

    public class SchemaChange
    {
        public SchemaChangeType ChangeType { get; set; }
        public string FieldName { get; set; }
        public string Description { get; set; }
        public bool IsBreakingChange { get; set; }
        public Dictionary<string, object> OldValue { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValue { get; set; } = new Dictionary<string, object>();
        public string MigrationScript { get; set; }

        // Create a deep clone of the schema change
        public SchemaChange Clone()
        {
            var clone = new SchemaChange
            {
                ChangeType = ChangeType,
                FieldName = FieldName,
                Description = Description,
                IsBreakingChange = IsBreakingChange,
                MigrationScript = MigrationScript
            };

            // Clone old value
            if (OldValue != null)
            {
                clone.OldValue = new Dictionary<string, object>(OldValue);
            }

            // Clone new value
            if (NewValue != null)
            {
                clone.NewValue = new Dictionary<string, object>(NewValue);
            }

            return clone;
        }
    }

    public enum SchemaChangeType
    {
        AddField,
        RemoveField,
        RenameField,
        ChangeType,
        ChangeRequired,
        ChangeDefault,
        ChangeValidation,
        DeprecateField,
        AddNestedField,
        RemoveNestedField,
        Other
    }
}
