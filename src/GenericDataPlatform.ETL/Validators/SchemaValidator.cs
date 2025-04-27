using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Validators
{
    public class SchemaValidator : IValidator
    {
        private readonly ILogger<SchemaValidator> _logger;

        public string Type => "Schema";

        public SchemaValidator(ILogger<SchemaValidator> logger)
        {
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Ensure the input is a list of DataRecord objects
                if (!(input is IEnumerable<DataRecord> inputRecords))
                {
                    throw new ArgumentException("Input must be a list of DataRecord objects");
                }

                var records = inputRecords.ToList();

                // Get validation options
                var failOnError = configuration.TryGetValue("failOnError", out var failOnErrorObj) &&
                    failOnErrorObj is bool failOnErrorBool && failOnErrorBool;

                var maxErrors = configuration.TryGetValue("maxErrors", out var maxErrorsObj) &&
                    int.TryParse(maxErrorsObj.ToString(), out var maxErrorsInt) ?
                    maxErrorsInt : 100;

                // Get schema
                DataSchema schema;

                if (configuration.TryGetValue("schema", out var schemaObj) && schemaObj is DataSchema configSchema)
                {
                    schema = configSchema;
                }
                else if (configuration.TryGetValue("schemaId", out var schemaIdObj) &&
                         schemaIdObj is string schemaId &&
                         source.Schema?.Id == schemaId)
                {
                    schema = source.Schema;
                }
                else if (source.Schema != null)
                {
                    schema = source.Schema;
                }
                else
                {
                    throw new ArgumentException("Schema is required for validation");
                }

                // Validate records against schema
                var validRecords = new List<DataRecord>();
                var invalidRecords = new List<DataRecord>();
                var errors = new List<ValidationError>();

                foreach (var record in records)
                {
                    var recordErrors = ValidateRecord(record, schema);

                    if (recordErrors.Any())
                    {
                        invalidRecords.Add(record);
                        errors.AddRange(recordErrors);

                        if (errors.Count >= maxErrors)
                        {
                            _logger.LogWarning("Maximum number of validation errors reached ({MaxErrors})", maxErrors);
                            break;
                        }
                    }
                    else
                    {
                        validRecords.Add(record);
                    }
                }

                // Create validation result
                var result = new ValidationResult
                {
                    IsValid = !errors.Any(),
                    ValidRecords = validRecords,
                    InvalidRecords = invalidRecords,
                    Errors = errors,
                    ValidationTime = DateTime.UtcNow
                };

                // If fail on error is enabled and there are errors, throw an exception
                if (failOnError && !result.IsValid)
                {
                    throw new ValidationException($"Validation failed with {errors.Count} errors", result);
                }

                return await Task.FromResult(result);
            }
            catch (ValidationException)
            {
                // Re-throw validation exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating data against schema");
                throw;
            }
        }

        private List<ValidationError> ValidateRecord(DataRecord record, DataSchema schema)
        {
            var errors = new List<ValidationError>();

            // Check each field in the schema
            foreach (var field in schema.Fields)
            {
                // Check if the field exists in the record
                if (!record.Data.TryGetValue(field.Name, out var value))
                {
                    // Field is missing
                    if (field.IsRequired)
                    {
                        errors.Add(new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field.Name,
                            ErrorType = ValidationErrorType.MissingRequiredField,
                            Message = $"Required field '{field.Name}' is missing"
                        });
                    }

                    continue;
                }

                // Field exists, validate its value
                if (value == null)
                {
                    // Null value
                    if (field.IsRequired)
                    {
                        errors.Add(new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field.Name,
                            ErrorType = ValidationErrorType.NullValue,
                            Message = $"Required field '{field.Name}' has a null value"
                        });
                    }

                    continue;
                }

                // Validate type
                if (!IsTypeValid(value, field.Type))
                {
                    errors.Add(new ValidationError
                    {
                        RecordId = record.Id,
                        FieldName = field.Name,
                        ErrorType = ValidationErrorType.TypeMismatch,
                        Message = $"Field '{field.Name}' has an invalid type. Expected {field.Type}, got {value.GetType().Name}"
                    });

                    continue;
                }

                // Validate against validation rules
                if (field.Validation != null)
                {
                    // Validate string length
                    if (field.Type == FieldType.String && value is string stringValue)
                    {
                        if (field.Validation.MinLength.HasValue && stringValue.Length < field.Validation.MinLength.Value)
                        {
                            errors.Add(new ValidationError
                            {
                                RecordId = record.Id,
                                FieldName = field.Name,
                                ErrorType = ValidationErrorType.ValidationRuleViolation,
                                Message = $"Field '{field.Name}' is too short. Minimum length is {field.Validation.MinLength.Value}, got {stringValue.Length}"
                            });
                        }

                        if (field.Validation.MaxLength.HasValue && stringValue.Length > field.Validation.MaxLength.Value)
                        {
                            errors.Add(new ValidationError
                            {
                                RecordId = record.Id,
                                FieldName = field.Name,
                                ErrorType = ValidationErrorType.ValidationRuleViolation,
                                Message = $"Field '{field.Name}' is too long. Maximum length is {field.Validation.MaxLength.Value}, got {stringValue.Length}"
                            });
                        }

                        if (!string.IsNullOrEmpty(field.Validation.Pattern))
                        {
                            var regex = new Regex(field.Validation.Pattern);
                            if (!regex.IsMatch(stringValue))
                            {
                                errors.Add(new ValidationError
                                {
                                    RecordId = record.Id,
                                    FieldName = field.Name,
                                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                                    Message = $"Field '{field.Name}' does not match the required pattern: {field.Validation.Pattern}"
                                });
                            }
                        }
                    }

                    // Validate numeric range
                    if ((field.Type == FieldType.Integer || field.Type == FieldType.Decimal) &&
                        double.TryParse(value.ToString(), out var numericValue))
                    {
                        if (field.Validation.MinValue.HasValue && numericValue < field.Validation.MinValue.Value)
                        {
                            errors.Add(new ValidationError
                            {
                                RecordId = record.Id,
                                FieldName = field.Name,
                                ErrorType = ValidationErrorType.ValidationRuleViolation,
                                Message = $"Field '{field.Name}' is too small. Minimum value is {field.Validation.MinValue.Value}, got {numericValue}"
                            });
                        }

                        if (field.Validation.MaxValue.HasValue && numericValue > field.Validation.MaxValue.Value)
                        {
                            errors.Add(new ValidationError
                            {
                                RecordId = record.Id,
                                FieldName = field.Name,
                                ErrorType = ValidationErrorType.ValidationRuleViolation,
                                Message = $"Field '{field.Name}' is too large. Maximum value is {field.Validation.MaxValue.Value}, got {numericValue}"
                            });
                        }
                    }

                    // Validate enum values
                    if (field.Validation.AllowedValues != null && field.Validation.AllowedValues.Any())
                    {
                        var stringValue = value.ToString();
                        if (!field.Validation.AllowedValues.Contains(stringValue))
                        {
                            errors.Add(new ValidationError
                            {
                                RecordId = record.Id,
                                FieldName = field.Name,
                                ErrorType = ValidationErrorType.ValidationRuleViolation,
                                Message = $"Field '{field.Name}' has an invalid value. Allowed values are: {string.Join(", ", field.Validation.AllowedValues)}"
                            });
                        }
                    }
                }
            }

            return errors;
        }

        private bool IsTypeValid(object value, FieldType expectedType)
        {
            switch (expectedType)
            {
                case FieldType.String:
                    return value is string;

                case FieldType.Integer:
                    return value is int || value is long || value is short ||
                           (value is string s1 && int.TryParse(s1, out _));

                case FieldType.Decimal:
                    return value is float || value is double || value is decimal ||
                           (value is string s2 && double.TryParse(s2, out _));

                case FieldType.Boolean:
                    return value is bool ||
                           (value is string s3 && bool.TryParse(s3, out _));

                case FieldType.DateTime:
                    return value is DateTime ||
                           (value is string s4 && DateTime.TryParse(s4, out _));

                case FieldType.Json:
                    if (value is string s5)
                    {
                        try
                        {
                            System.Text.Json.JsonDocument.Parse(s5);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    return false;

                case FieldType.Binary:
                    return value is byte[] || value is System.IO.Stream;

                case FieldType.Array:
                    return value is Array || value is System.Collections.IEnumerable;

                case FieldType.Complex:
                    return true; // Accept any type for complex fields

                default:
                    return false;
            }
        }
    }

    public interface IValidator
    {
        string Type { get; }
        Task<ValidationResult> ValidateAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<DataRecord> ValidRecords { get; set; } = new List<DataRecord>();
        public List<DataRecord> InvalidRecords { get; set; } = new List<DataRecord>();
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public DateTime ValidationTime { get; set; }
    }

    public class ValidationError
    {
        public string RecordId { get; set; }
        public string FieldName { get; set; }
        public ValidationErrorType ErrorType { get; set; }
        public string Message { get; set; }
    }

    public enum ValidationErrorType
    {
        MissingRequiredField,
        NullValue,
        TypeMismatch,
        ValidationRuleViolation,
        CustomValidationFailure
    }

    public class ValidationException : Exception
    {
        public ValidationResult ValidationResult { get; }

        public ValidationException(string message, ValidationResult validationResult) : base(message)
        {
            ValidationResult = validationResult;
        }
    }
}
