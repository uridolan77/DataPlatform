using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Validators
{
    public class DataQualityValidator : IValidator
    {
        private readonly ILogger<DataQualityValidator> _logger;
        
        public string Type => "DataQuality";
        
        public DataQualityValidator(ILogger<DataQualityValidator> logger)
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
                
                // Get quality rules
                if (!configuration.TryGetValue("rules", out var rulesObj) || !(rulesObj is List<object> rulesList))
                {
                    throw new ArgumentException("Quality rules are required for data quality validation");
                }
                
                var rules = new List<QualityRule>();
                
                foreach (var ruleObj in rulesList)
                {
                    if (ruleObj is Dictionary<string, object> ruleDict)
                    {
                        var rule = new QualityRule();
                        
                        if (ruleDict.TryGetValue("type", out var typeObj))
                        {
                            rule.Type = typeObj.ToString();
                        }
                        else
                        {
                            throw new ArgumentException("Rule type is required");
                        }
                        
                        if (ruleDict.TryGetValue("field", out var fieldObj))
                        {
                            rule.Field = fieldObj.ToString();
                        }
                        
                        if (ruleDict.TryGetValue("parameters", out var parametersObj) && 
                            parametersObj is Dictionary<string, object> parameters)
                        {
                            rule.Parameters = parameters;
                        }
                        else
                        {
                            rule.Parameters = new Dictionary<string, object>();
                        }
                        
                        rules.Add(rule);
                    }
                }
                
                // Validate records against quality rules
                var validRecords = new List<DataRecord>();
                var invalidRecords = new List<DataRecord>();
                var errors = new List<ValidationError>();
                
                // Apply record-level rules
                var recordLevelRules = rules.Where(r => string.IsNullOrEmpty(r.Field)).ToList();
                if (recordLevelRules.Any())
                {
                    var recordLevelErrors = ValidateRecordLevel(records, recordLevelRules);
                    errors.AddRange(recordLevelErrors);
                    
                    // Mark records as invalid
                    var invalidRecordIds = recordLevelErrors.Select(e => e.RecordId).Distinct().ToHashSet();
                    foreach (var record in records)
                    {
                        if (invalidRecordIds.Contains(record.Id))
                        {
                            invalidRecords.Add(record);
                        }
                        else
                        {
                            validRecords.Add(record);
                        }
                    }
                }
                else
                {
                    // No record-level rules, all records are initially valid
                    validRecords.AddRange(records);
                }
                
                // Apply field-level rules
                var fieldLevelRules = rules.Where(r => !string.IsNullOrEmpty(r.Field)).ToList();
                if (fieldLevelRules.Any())
                {
                    // Group rules by field
                    var rulesByField = fieldLevelRules.GroupBy(r => r.Field).ToDictionary(g => g.Key, g => g.ToList());
                    
                    // Validate each record
                    foreach (var record in validRecords.ToList()) // Use ToList to avoid modifying the collection during iteration
                    {
                        var recordErrors = new List<ValidationError>();
                        
                        foreach (var fieldRules in rulesByField)
                        {
                            var field = fieldRules.Key;
                            var fieldRulesList = fieldRules.Value;
                            
                            // Check if the field exists
                            if (!record.Data.TryGetValue(field, out var value))
                            {
                                // Skip validation for missing fields
                                continue;
                            }
                            
                            // Apply each rule
                            foreach (var rule in fieldRulesList)
                            {
                                var error = ValidateField(record, field, value, rule);
                                if (error != null)
                                {
                                    recordErrors.Add(error);
                                }
                            }
                        }
                        
                        if (recordErrors.Any())
                        {
                            // Move record from valid to invalid
                            validRecords.Remove(record);
                            invalidRecords.Add(record);
                            errors.AddRange(recordErrors);
                            
                            if (errors.Count >= maxErrors)
                            {
                                _logger.LogWarning("Maximum number of validation errors reached ({MaxErrors})", maxErrors);
                                break;
                            }
                        }
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
                    throw new ValidationException($"Data quality validation failed with {errors.Count} errors", result);
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
                _logger.LogError(ex, "Error validating data quality");
                throw;
            }
        }
        
        private List<ValidationError> ValidateRecordLevel(List<DataRecord> records, List<QualityRule> rules)
        {
            var errors = new List<ValidationError>();
            
            foreach (var rule in rules)
            {
                switch (rule.Type.ToLowerInvariant())
                {
                    case "uniqueness":
                        errors.AddRange(ValidateUniqueness(records, rule));
                        break;
                    
                    case "referentialintegrity":
                        errors.AddRange(ValidateReferentialIntegrity(records, rule));
                        break;
                    
                    case "consistency":
                        errors.AddRange(ValidateConsistency(records, rule));
                        break;
                    
                    default:
                        _logger.LogWarning("Unsupported record-level rule type: {RuleType}", rule.Type);
                        break;
                }
            }
            
            return errors;
        }
        
        private ValidationError ValidateField(DataRecord record, string field, object value, QualityRule rule)
        {
            switch (rule.Type.ToLowerInvariant())
            {
                case "nullcheck":
                    return ValidateNullCheck(record, field, value, rule);
                
                case "pattern":
                    return ValidatePattern(record, field, value, rule);
                
                case "range":
                    return ValidateRange(record, field, value, rule);
                
                case "length":
                    return ValidateLength(record, field, value, rule);
                
                case "format":
                    return ValidateFormat(record, field, value, rule);
                
                case "custom":
                    return ValidateCustom(record, field, value, rule);
                
                default:
                    _logger.LogWarning("Unsupported field-level rule type: {RuleType}", rule.Type);
                    return null;
            }
        }
        
        private ValidationError ValidateNullCheck(DataRecord record, string field, object value, QualityRule rule)
        {
            var allowNull = rule.Parameters.TryGetValue("allowNull", out var allowNullObj) && 
                allowNullObj is bool allowNullBool && allowNullBool;
            
            if (!allowNull && value == null)
            {
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.NullValue,
                    Message = $"Field '{field}' cannot be null"
                };
            }
            
            return null;
        }
        
        private ValidationError ValidatePattern(DataRecord record, string field, object value, QualityRule rule)
        {
            if (value == null)
            {
                return null; // Skip pattern validation for null values
            }
            
            if (!rule.Parameters.TryGetValue("pattern", out var patternObj))
            {
                throw new ArgumentException($"Pattern is required for pattern validation of field '{field}'");
            }
            
            var pattern = patternObj.ToString();
            var stringValue = value.ToString();
            
            try
            {
                var regex = new Regex(pattern);
                if (!regex.IsMatch(stringValue))
                {
                    return new ValidationError
                    {
                        RecordId = record.Id,
                        FieldName = field,
                        ErrorType = ValidationErrorType.ValidationRuleViolation,
                        Message = $"Field '{field}' does not match the required pattern: {pattern}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating pattern for field '{Field}'", field);
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                    Message = $"Error validating pattern for field '{field}': {ex.Message}"
                };
            }
            
            return null;
        }
        
        private ValidationError ValidateRange(DataRecord record, string field, object value, QualityRule rule)
        {
            if (value == null)
            {
                return null; // Skip range validation for null values
            }
            
            if (!double.TryParse(value.ToString(), out var numericValue))
            {
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                    Message = $"Field '{field}' is not a valid number"
                };
            }
            
            if (rule.Parameters.TryGetValue("min", out var minObj) && 
                double.TryParse(minObj.ToString(), out var min) && 
                numericValue < min)
            {
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                    Message = $"Field '{field}' is too small. Minimum value is {min}, got {numericValue}"
                };
            }
            
            if (rule.Parameters.TryGetValue("max", out var maxObj) && 
                double.TryParse(maxObj.ToString(), out var max) && 
                numericValue > max)
            {
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                    Message = $"Field '{field}' is too large. Maximum value is {max}, got {numericValue}"
                };
            }
            
            return null;
        }
        
        private ValidationError ValidateLength(DataRecord record, string field, object value, QualityRule rule)
        {
            if (value == null)
            {
                return null; // Skip length validation for null values
            }
            
            var stringValue = value.ToString();
            
            if (rule.Parameters.TryGetValue("minLength", out var minLengthObj) && 
                int.TryParse(minLengthObj.ToString(), out var minLength) && 
                stringValue.Length < minLength)
            {
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                    Message = $"Field '{field}' is too short. Minimum length is {minLength}, got {stringValue.Length}"
                };
            }
            
            if (rule.Parameters.TryGetValue("maxLength", out var maxLengthObj) && 
                int.TryParse(maxLengthObj.ToString(), out var maxLength) && 
                stringValue.Length > maxLength)
            {
                return new ValidationError
                {
                    RecordId = record.Id,
                    FieldName = field,
                    ErrorType = ValidationErrorType.ValidationRuleViolation,
                    Message = $"Field '{field}' is too long. Maximum length is {maxLength}, got {stringValue.Length}"
                };
            }
            
            return null;
        }
        
        private ValidationError ValidateFormat(DataRecord record, string field, object value, QualityRule rule)
        {
            if (value == null)
            {
                return null; // Skip format validation for null values
            }
            
            if (!rule.Parameters.TryGetValue("format", out var formatObj))
            {
                throw new ArgumentException($"Format is required for format validation of field '{field}'");
            }
            
            var format = formatObj.ToString();
            var stringValue = value.ToString();
            
            switch (format.ToLowerInvariant())
            {
                case "email":
                    var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                    if (!Regex.IsMatch(stringValue, emailPattern))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid email address"
                        };
                    }
                    break;
                
                case "url":
                    var urlPattern = @"^(http|https)://[a-zA-Z0-9-\.]+\.[a-zA-Z]{2,}(/\S*)?$";
                    if (!Regex.IsMatch(stringValue, urlPattern))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid URL"
                        };
                    }
                    break;
                
                case "date":
                    if (!DateTime.TryParse(stringValue, out _))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid date"
                        };
                    }
                    break;
                
                case "time":
                    var timePattern = @"^([01]?[0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?$";
                    if (!Regex.IsMatch(stringValue, timePattern))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid time"
                        };
                    }
                    break;
                
                case "phone":
                    var phonePattern = @"^\+?[0-9]{10,15}$";
                    if (!Regex.IsMatch(stringValue, phonePattern))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid phone number"
                        };
                    }
                    break;
                
                case "zipcode":
                    var zipPattern = @"^\d{5}(-\d{4})?$";
                    if (!Regex.IsMatch(stringValue, zipPattern))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid ZIP code"
                        };
                    }
                    break;
                
                case "ipv4":
                    var ipv4Pattern = @"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$";
                    if (!Regex.IsMatch(stringValue, ipv4Pattern))
                    {
                        return new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = field,
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Field '{field}' is not a valid IPv4 address"
                        };
                    }
                    break;
                
                default:
                    _logger.LogWarning("Unsupported format: {Format}", format);
                    break;
            }
            
            return null;
        }
        
        private ValidationError ValidateCustom(DataRecord record, string field, object value, QualityRule rule)
        {
            if (!rule.Parameters.TryGetValue("expression", out var expressionObj))
            {
                throw new ArgumentException($"Expression is required for custom validation of field '{field}'");
            }
            
            var expression = expressionObj.ToString();
            
            // In a real implementation, this would evaluate the expression
            // For this example, we'll just log a warning
            _logger.LogWarning("Custom validation expressions are not implemented: {Expression}", expression);
            
            return null;
        }
        
        private List<ValidationError> ValidateUniqueness(List<DataRecord> records, QualityRule rule)
        {
            var errors = new List<ValidationError>();
            
            if (!rule.Parameters.TryGetValue("fields", out var fieldsObj) || !(fieldsObj is List<object> fieldsList))
            {
                throw new ArgumentException("Fields are required for uniqueness validation");
            }
            
            var fields = fieldsList.Select(f => f.ToString()).ToList();
            
            // Group records by the specified fields
            var groups = records.GroupBy(record =>
            {
                var key = new Dictionary<string, object>();
                
                foreach (var field in fields)
                {
                    if (record.Data.TryGetValue(field, out var value))
                    {
                        key[field] = value;
                    }
                    else
                    {
                        key[field] = null;
                    }
                }
                
                return new GroupKey(key);
            });
            
            // Find duplicate groups
            foreach (var group in groups)
            {
                if (group.Count() > 1)
                {
                    // Create an error for each duplicate record
                    foreach (var record in group.Skip(1)) // Skip the first record
                    {
                        errors.Add(new ValidationError
                        {
                            RecordId = record.Id,
                            FieldName = string.Join(", ", fields),
                            ErrorType = ValidationErrorType.ValidationRuleViolation,
                            Message = $"Duplicate value found for fields: {string.Join(", ", fields)}"
                        });
                    }
                }
            }
            
            return errors;
        }
        
        private List<ValidationError> ValidateReferentialIntegrity(List<DataRecord> records, QualityRule rule)
        {
            // In a real implementation, this would check referential integrity against a reference dataset
            // For this example, we'll just log a warning
            _logger.LogWarning("Referential integrity validation is not implemented");
            
            return new List<ValidationError>();
        }
        
        private List<ValidationError> ValidateConsistency(List<DataRecord> records, QualityRule rule)
        {
            var errors = new List<ValidationError>();
            
            if (!rule.Parameters.TryGetValue("conditions", out var conditionsObj) || 
                !(conditionsObj is List<object> conditionsList))
            {
                throw new ArgumentException("Conditions are required for consistency validation");
            }
            
            // In a real implementation, this would validate consistency conditions
            // For this example, we'll just log a warning
            _logger.LogWarning("Consistency validation is not fully implemented");
            
            return errors;
        }
        
        private class GroupKey
        {
            public Dictionary<string, object> Fields { get; }
            
            public GroupKey(Dictionary<string, object> fields)
            {
                Fields = fields;
            }
            
            public override bool Equals(object obj)
            {
                if (obj is GroupKey other)
                {
                    if (Fields.Count != other.Fields.Count)
                    {
                        return false;
                    }
                    
                    foreach (var field in Fields)
                    {
                        if (!other.Fields.TryGetValue(field.Key, out var otherValue) || 
                            !Equals(field.Value, otherValue))
                        {
                            return false;
                        }
                    }
                    
                    return true;
                }
                
                return false;
            }
            
            public override int GetHashCode()
            {
                var hash = 17;
                
                foreach (var field in Fields)
                {
                    hash = hash * 23 + (field.Key?.GetHashCode() ?? 0);
                    hash = hash * 23 + (field.Value?.GetHashCode() ?? 0);
                }
                
                return hash;
            }
        }
    }
    
    public class QualityRule
    {
        public string Type { get; set; }
        public string Field { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
