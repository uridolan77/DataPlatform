using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Enrichers
{
    public class DataEnricher : IEnricher
    {
        private readonly ILogger<DataEnricher> _logger;
        
        public string Type => "Data";
        
        public DataEnricher(ILogger<DataEnricher> logger)
        {
            _logger = logger;
        }
        
        public async Task<object> EnrichAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Ensure the input is a list of DataRecord objects
                if (!(input is IEnumerable<DataRecord> inputRecords))
                {
                    throw new ArgumentException("Input must be a list of DataRecord objects");
                }
                
                var records = inputRecords.ToList();
                
                // Get enrichment rules
                if (!configuration.TryGetValue("rules", out var rulesObj) || !(rulesObj is List<object> rulesList))
                {
                    throw new ArgumentException("Enrichment rules are required");
                }
                
                var rules = new List<EnrichmentRule>();
                
                foreach (var ruleObj in rulesList)
                {
                    if (ruleObj is Dictionary<string, object> ruleDict)
                    {
                        var rule = new EnrichmentRule();
                        
                        if (ruleDict.TryGetValue("type", out var typeObj))
                        {
                            rule.Type = typeObj.ToString();
                        }
                        else
                        {
                            throw new ArgumentException("Rule type is required");
                        }
                        
                        if (ruleDict.TryGetValue("targetField", out var targetFieldObj))
                        {
                            rule.TargetField = targetFieldObj.ToString();
                        }
                        else
                        {
                            throw new ArgumentException("Target field is required");
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
                
                // Apply enrichment rules
                var enrichedRecords = new List<DataRecord>();
                
                foreach (var record in records)
                {
                    var enrichedData = new Dictionary<string, object>(record.Data);
                    
                    foreach (var rule in rules)
                    {
                        try
                        {
                            ApplyEnrichmentRule(enrichedData, rule);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying enrichment rule {RuleType} to field {TargetField}", 
                                rule.Type, rule.TargetField);
                        }
                    }
                    
                    // Create a new record with enriched data
                    var enrichedRecord = new DataRecord
                    {
                        Id = record.Id,
                        SchemaId = record.SchemaId,
                        SourceId = record.SourceId,
                        Data = enrichedData,
                        Metadata = new Dictionary<string, string>(record.Metadata)
                        {
                            ["enriched"] = "true",
                            ["enrichmentTime"] = DateTime.UtcNow.ToString("o")
                        },
                        CreatedAt = record.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                        Version = record.Version
                    };
                    
                    enrichedRecords.Add(enrichedRecord);
                }
                
                return await Task.FromResult(enrichedRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching data");
                throw;
            }
        }
        
        private void ApplyEnrichmentRule(Dictionary<string, object> data, EnrichmentRule rule)
        {
            switch (rule.Type.ToLowerInvariant())
            {
                case "derived":
                    ApplyDerivedField(data, rule);
                    break;
                
                case "lookup":
                    ApplyLookup(data, rule);
                    break;
                
                case "transform":
                    ApplyTransform(data, rule);
                    break;
                
                case "combine":
                    ApplyCombine(data, rule);
                    break;
                
                case "split":
                    ApplySplit(data, rule);
                    break;
                
                case "format":
                    ApplyFormat(data, rule);
                    break;
                
                case "default":
                    ApplyDefault(data, rule);
                    break;
                
                case "extract":
                    ApplyExtract(data, rule);
                    break;
                
                case "calculate":
                    ApplyCalculate(data, rule);
                    break;
                
                default:
                    _logger.LogWarning("Unsupported enrichment rule type: {RuleType}", rule.Type);
                    break;
            }
        }
        
        private void ApplyDerivedField(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("expression", out var expressionObj))
            {
                throw new ArgumentException($"Expression is required for derived field enrichment");
            }
            
            var expression = expressionObj.ToString();
            
            // In a real implementation, this would evaluate the expression
            // For this example, we'll implement a simple expression evaluator
            
            // Check if it's a simple field reference
            if (expression.StartsWith("$") && !expression.Contains(" "))
            {
                var fieldName = expression.Substring(1);
                if (data.TryGetValue(fieldName, out var value))
                {
                    data[rule.TargetField] = value;
                }
                return;
            }
            
            // Check if it's a simple arithmetic expression
            var arithmeticPattern = @"^\$(\w+)\s*([\+\-\*\/])\s*(\d+(\.\d+)?)$";
            var match = Regex.Match(expression, arithmeticPattern);
            
            if (match.Success)
            {
                var fieldName = match.Groups[1].Value;
                var operation = match.Groups[2].Value;
                var operand = double.Parse(match.Groups[3].Value);
                
                if (data.TryGetValue(fieldName, out var fieldValue) && 
                    double.TryParse(fieldValue?.ToString(), out var numericValue))
                {
                    double result = 0;
                    
                    switch (operation)
                    {
                        case "+":
                            result = numericValue + operand;
                            break;
                        
                        case "-":
                            result = numericValue - operand;
                            break;
                        
                        case "*":
                            result = numericValue * operand;
                            break;
                        
                        case "/":
                            if (operand != 0)
                            {
                                result = numericValue / operand;
                            }
                            break;
                    }
                    
                    data[rule.TargetField] = result;
                }
                
                return;
            }
            
            // For more complex expressions, log a warning
            _logger.LogWarning("Complex expressions are not fully implemented: {Expression}", expression);
        }
        
        private void ApplyLookup(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceField", out var sourceFieldObj))
            {
                throw new ArgumentException($"Source field is required for lookup enrichment");
            }
            
            var sourceField = sourceFieldObj.ToString();
            
            if (!rule.Parameters.TryGetValue("lookupTable", out var lookupTableObj))
            {
                throw new ArgumentException($"Lookup table is required for lookup enrichment");
            }
            
            var lookupTable = lookupTableObj as Dictionary<string, object>;
            if (lookupTable == null)
            {
                throw new ArgumentException($"Lookup table must be a dictionary");
            }
            
            // Get the source value
            if (data.TryGetValue(sourceField, out var sourceValue) && sourceValue != null)
            {
                var sourceKey = sourceValue.ToString();
                
                // Look up the value
                if (lookupTable.TryGetValue(sourceKey, out var lookupValue))
                {
                    data[rule.TargetField] = lookupValue;
                }
                else if (rule.Parameters.TryGetValue("defaultValue", out var defaultValue))
                {
                    data[rule.TargetField] = defaultValue;
                }
            }
        }
        
        private void ApplyTransform(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceField", out var sourceFieldObj))
            {
                throw new ArgumentException($"Source field is required for transform enrichment");
            }
            
            var sourceField = sourceFieldObj.ToString();
            
            if (!rule.Parameters.TryGetValue("transformType", out var transformTypeObj))
            {
                throw new ArgumentException($"Transform type is required for transform enrichment");
            }
            
            var transformType = transformTypeObj.ToString();
            
            // Get the source value
            if (data.TryGetValue(sourceField, out var sourceValue) && sourceValue != null)
            {
                var stringValue = sourceValue.ToString();
                
                switch (transformType.ToLowerInvariant())
                {
                    case "uppercase":
                        data[rule.TargetField] = stringValue.ToUpper();
                        break;
                    
                    case "lowercase":
                        data[rule.TargetField] = stringValue.ToLower();
                        break;
                    
                    case "trim":
                        data[rule.TargetField] = stringValue.Trim();
                        break;
                    
                    case "replace":
                        if (rule.Parameters.TryGetValue("oldValue", out var oldValueObj) && 
                            rule.Parameters.TryGetValue("newValue", out var newValueObj))
                        {
                            data[rule.TargetField] = stringValue.Replace(
                                oldValueObj.ToString(), 
                                newValueObj?.ToString() ?? string.Empty);
                        }
                        break;
                    
                    case "substring":
                        if (rule.Parameters.TryGetValue("startIndex", out var startIndexObj) && 
                            int.TryParse(startIndexObj.ToString(), out var startIndex))
                        {
                            int length = stringValue.Length - startIndex;
                            
                            if (rule.Parameters.TryGetValue("length", out var lengthObj) && 
                                int.TryParse(lengthObj.ToString(), out var lengthValue))
                            {
                                length = lengthValue;
                            }
                            
                            if (startIndex >= 0 && startIndex < stringValue.Length && length > 0)
                            {
                                length = Math.Min(length, stringValue.Length - startIndex);
                                data[rule.TargetField] = stringValue.Substring(startIndex, length);
                            }
                        }
                        break;
                    
                    case "regex":
                        if (rule.Parameters.TryGetValue("pattern", out var patternObj))
                        {
                            var pattern = patternObj.ToString();
                            
                            if (rule.Parameters.TryGetValue("replacement", out var replacementObj))
                            {
                                // Regex replace
                                var replacement = replacementObj?.ToString() ?? string.Empty;
                                data[rule.TargetField] = Regex.Replace(stringValue, pattern, replacement);
                            }
                            else
                            {
                                // Regex match
                                var match = Regex.Match(stringValue, pattern);
                                if (match.Success)
                                {
                                    if (rule.Parameters.TryGetValue("group", out var groupObj) && 
                                        int.TryParse(groupObj.ToString(), out var groupIndex) && 
                                        groupIndex < match.Groups.Count)
                                    {
                                        data[rule.TargetField] = match.Groups[groupIndex].Value;
                                    }
                                    else
                                    {
                                        data[rule.TargetField] = match.Value;
                                    }
                                }
                            }
                        }
                        break;
                    
                    default:
                        _logger.LogWarning("Unsupported transform type: {TransformType}", transformType);
                        break;
                }
            }
        }
        
        private void ApplyCombine(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceFields", out var sourceFieldsObj) || 
                !(sourceFieldsObj is List<object> sourceFieldsList))
            {
                throw new ArgumentException($"Source fields are required for combine enrichment");
            }
            
            var sourceFields = sourceFieldsList.Select(f => f.ToString()).ToList();
            
            var separator = rule.Parameters.TryGetValue("separator", out var separatorObj) ? 
                separatorObj.ToString() : " ";
            
            var values = new List<string>();
            
            foreach (var field in sourceFields)
            {
                if (data.TryGetValue(field, out var value) && value != null)
                {
                    values.Add(value.ToString());
                }
            }
            
            data[rule.TargetField] = string.Join(separator, values);
        }
        
        private void ApplySplit(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceField", out var sourceFieldObj))
            {
                throw new ArgumentException($"Source field is required for split enrichment");
            }
            
            var sourceField = sourceFieldObj.ToString();
            
            if (!rule.Parameters.TryGetValue("delimiter", out var delimiterObj))
            {
                throw new ArgumentException($"Delimiter is required for split enrichment");
            }
            
            var delimiter = delimiterObj.ToString();
            
            if (!rule.Parameters.TryGetValue("index", out var indexObj) || 
                !int.TryParse(indexObj.ToString(), out var index))
            {
                throw new ArgumentException($"Index is required for split enrichment");
            }
            
            // Get the source value
            if (data.TryGetValue(sourceField, out var sourceValue) && sourceValue != null)
            {
                var stringValue = sourceValue.ToString();
                var parts = stringValue.Split(new[] { delimiter }, StringSplitOptions.None);
                
                if (index >= 0 && index < parts.Length)
                {
                    data[rule.TargetField] = parts[index];
                }
            }
        }
        
        private void ApplyFormat(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceField", out var sourceFieldObj))
            {
                throw new ArgumentException($"Source field is required for format enrichment");
            }
            
            var sourceField = sourceFieldObj.ToString();
            
            if (!rule.Parameters.TryGetValue("format", out var formatObj))
            {
                throw new ArgumentException($"Format is required for format enrichment");
            }
            
            var format = formatObj.ToString();
            
            // Get the source value
            if (data.TryGetValue(sourceField, out var sourceValue) && sourceValue != null)
            {
                if (sourceValue is DateTime dateTime)
                {
                    data[rule.TargetField] = dateTime.ToString(format);
                }
                else if (sourceValue is IFormattable formattable)
                {
                    data[rule.TargetField] = formattable.ToString(format, null);
                }
                else
                {
                    // Try to parse as date
                    if (DateTime.TryParse(sourceValue.ToString(), out var parsedDateTime))
                    {
                        data[rule.TargetField] = parsedDateTime.ToString(format);
                    }
                    else
                    {
                        // Try to parse as number
                        if (double.TryParse(sourceValue.ToString(), out var parsedNumber))
                        {
                            data[rule.TargetField] = parsedNumber.ToString(format);
                        }
                        else
                        {
                            // Just use string format
                            data[rule.TargetField] = string.Format($"{{0:{format}}}", sourceValue);
                        }
                    }
                }
            }
        }
        
        private void ApplyDefault(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceField", out var sourceFieldObj))
            {
                throw new ArgumentException($"Source field is required for default enrichment");
            }
            
            var sourceField = sourceFieldObj.ToString();
            
            if (!rule.Parameters.TryGetValue("defaultValue", out var defaultValueObj))
            {
                throw new ArgumentException($"Default value is required for default enrichment");
            }
            
            var defaultValue = defaultValueObj;
            
            // Check if the source field exists and has a non-null value
            if (!data.TryGetValue(sourceField, out var sourceValue) || sourceValue == null)
            {
                data[rule.TargetField] = defaultValue;
            }
            else
            {
                data[rule.TargetField] = sourceValue;
            }
        }
        
        private void ApplyExtract(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("sourceField", out var sourceFieldObj))
            {
                throw new ArgumentException($"Source field is required for extract enrichment");
            }
            
            var sourceField = sourceFieldObj.ToString();
            
            if (!rule.Parameters.TryGetValue("extractType", out var extractTypeObj))
            {
                throw new ArgumentException($"Extract type is required for extract enrichment");
            }
            
            var extractType = extractTypeObj.ToString();
            
            // Get the source value
            if (data.TryGetValue(sourceField, out var sourceValue) && sourceValue != null)
            {
                var stringValue = sourceValue.ToString();
                
                switch (extractType.ToLowerInvariant())
                {
                    case "year":
                        if (DateTime.TryParse(stringValue, out var dateTime))
                        {
                            data[rule.TargetField] = dateTime.Year;
                        }
                        break;
                    
                    case "month":
                        if (DateTime.TryParse(stringValue, out dateTime))
                        {
                            data[rule.TargetField] = dateTime.Month;
                        }
                        break;
                    
                    case "day":
                        if (DateTime.TryParse(stringValue, out dateTime))
                        {
                            data[rule.TargetField] = dateTime.Day;
                        }
                        break;
                    
                    case "hour":
                        if (DateTime.TryParse(stringValue, out dateTime))
                        {
                            data[rule.TargetField] = dateTime.Hour;
                        }
                        break;
                    
                    case "minute":
                        if (DateTime.TryParse(stringValue, out dateTime))
                        {
                            data[rule.TargetField] = dateTime.Minute;
                        }
                        break;
                    
                    case "second":
                        if (DateTime.TryParse(stringValue, out dateTime))
                        {
                            data[rule.TargetField] = dateTime.Second;
                        }
                        break;
                    
                    case "domain":
                        if (Uri.TryCreate(stringValue, UriKind.Absolute, out var uri))
                        {
                            data[rule.TargetField] = uri.Host;
                        }
                        else if (stringValue.Contains("@"))
                        {
                            // Try to extract domain from email
                            var parts = stringValue.Split('@');
                            if (parts.Length == 2)
                            {
                                data[rule.TargetField] = parts[1];
                            }
                        }
                        break;
                    
                    case "filename":
                        data[rule.TargetField] = System.IO.Path.GetFileName(stringValue);
                        break;
                    
                    case "extension":
                        data[rule.TargetField] = System.IO.Path.GetExtension(stringValue);
                        break;
                    
                    default:
                        _logger.LogWarning("Unsupported extract type: {ExtractType}", extractType);
                        break;
                }
            }
        }
        
        private void ApplyCalculate(Dictionary<string, object> data, EnrichmentRule rule)
        {
            if (!rule.Parameters.TryGetValue("expression", out var expressionObj))
            {
                throw new ArgumentException($"Expression is required for calculate enrichment");
            }
            
            var expression = expressionObj.ToString();
            
            // In a real implementation, this would evaluate the expression
            // For this example, we'll implement a simple expression evaluator for basic arithmetic
            
            // Check if it's a simple arithmetic expression with two fields
            var arithmeticPattern = @"^\$(\w+)\s*([\+\-\*\/])\s*\$(\w+)$";
            var match = Regex.Match(expression, arithmeticPattern);
            
            if (match.Success)
            {
                var field1 = match.Groups[1].Value;
                var operation = match.Groups[2].Value;
                var field2 = match.Groups[3].Value;
                
                if (data.TryGetValue(field1, out var value1) && 
                    data.TryGetValue(field2, out var value2) && 
                    double.TryParse(value1?.ToString(), out var num1) && 
                    double.TryParse(value2?.ToString(), out var num2))
                {
                    double result = 0;
                    
                    switch (operation)
                    {
                        case "+":
                            result = num1 + num2;
                            break;
                        
                        case "-":
                            result = num1 - num2;
                            break;
                        
                        case "*":
                            result = num1 * num2;
                            break;
                        
                        case "/":
                            if (num2 != 0)
                            {
                                result = num1 / num2;
                            }
                            break;
                    }
                    
                    data[rule.TargetField] = result;
                }
                
                return;
            }
            
            // For more complex expressions, log a warning
            _logger.LogWarning("Complex expressions are not fully implemented: {Expression}", expression);
        }
    }
    
    public interface IEnricher
    {
        string Type { get; }
        Task<object> EnrichAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source);
    }
    
    public class EnrichmentRule
    {
        public string Type { get; set; }
        public string TargetField { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
