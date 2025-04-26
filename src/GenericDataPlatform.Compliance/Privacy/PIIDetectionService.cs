using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Compliance.Privacy
{
    /// <summary>
    /// Service for detecting and masking PII data
    /// </summary>
    public class PIIDetectionService : IPIIDetectionService
    {
        private readonly PIIOptions _options;
        private readonly ILogger<PIIDetectionService> _logger;
        private readonly Dictionary<string, Regex> _patterns;

        public PIIDetectionService(IOptions<PIIOptions> options, ILogger<PIIDetectionService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _patterns = InitializePatterns();
        }

        /// <summary>
        /// Detects PII in a string
        /// </summary>
        public IEnumerable<PIIDetection> DetectPII(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            foreach (var pattern in _patterns)
            {
                var matches = pattern.Value.Matches(text);
                foreach (Match match in matches)
                {
                    yield return new PIIDetection
                    {
                        Type = pattern.Key,
                        Value = match.Value,
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length - 1,
                        Confidence = 0.9 // Fixed confidence for regex patterns
                    };
                }
            }
        }

        /// <summary>
        /// Detects PII in a dictionary of values
        /// </summary>
        public IEnumerable<PIIDetection> DetectPII(Dictionary<string, object> data)
        {
            if (data == null)
            {
                yield break;
            }

            foreach (var item in data)
            {
                if (item.Value == null)
                {
                    continue;
                }

                // Check if the field name is a known PII field
                if (_options.KnownPIIFields.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                {
                    yield return new PIIDetection
                    {
                        Type = item.Key,
                        Value = item.Value.ToString(),
                        FieldName = item.Key,
                        Confidence = 1.0 // High confidence for known PII fields
                    };
                    continue;
                }

                // Check the value with regex patterns
                var valueStr = item.Value.ToString();
                foreach (var detection in DetectPII(valueStr))
                {
                    detection.FieldName = item.Key;
                    yield return detection;
                }
            }
        }

        /// <summary>
        /// Masks PII in a string
        /// </summary>
        public string MaskPII(string text, PIIMaskingOptions maskingOptions = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            maskingOptions ??= _options.DefaultMaskingOptions;
            var detections = DetectPII(text).OrderByDescending(d => d.StartIndex).ToList();
            
            foreach (var detection in detections)
            {
                if (detection.StartIndex >= 0 && detection.EndIndex < text.Length)
                {
                    var maskedValue = ApplyMasking(detection.Value, detection.Type, maskingOptions);
                    text = text.Substring(0, detection.StartIndex) + maskedValue + text.Substring(detection.EndIndex + 1);
                }
            }

            return text;
        }

        /// <summary>
        /// Masks PII in a dictionary of values
        /// </summary>
        public Dictionary<string, object> MaskPII(Dictionary<string, object> data, PIIMaskingOptions maskingOptions = null)
        {
            if (data == null)
            {
                return data;
            }

            maskingOptions ??= _options.DefaultMaskingOptions;
            var result = new Dictionary<string, object>(data);
            var detections = DetectPII(data).ToList();
            
            foreach (var detection in detections)
            {
                if (!string.IsNullOrEmpty(detection.FieldName) && result.ContainsKey(detection.FieldName))
                {
                    var value = result[detection.FieldName]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        var maskedValue = ApplyMasking(value, detection.Type, maskingOptions);
                        result[detection.FieldName] = maskedValue;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Applies masking to a value based on its type
        /// </summary>
        private string ApplyMasking(string value, string piiType, PIIMaskingOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            switch (options.MaskingType)
            {
                case PIIMaskingType.Redact:
                    return "[REDACTED]";
                
                case PIIMaskingType.Hash:
                    return HashValue(value);
                
                case PIIMaskingType.Tokenize:
                    return TokenizeValue(value, piiType);
                
                case PIIMaskingType.PartialMask:
                    return PartialMask(value, piiType, options);
                
                default:
                    return "[REDACTED]";
            }
        }

        /// <summary>
        /// Hashes a value
        /// </summary>
        private string HashValue(string value)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Tokenizes a value
        /// </summary>
        private string TokenizeValue(string value, string piiType)
        {
            // In a real implementation, this would use a tokenization service
            // For this example, we'll just use a simple hash with a type prefix
            return $"{piiType}_{HashValue(value).Substring(0, 8)}";
        }

        /// <summary>
        /// Partially masks a value
        /// </summary>
        private string PartialMask(string value, string piiType, PIIMaskingOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            switch (piiType.ToLowerInvariant())
            {
                case "email":
                    var parts = value.Split('@');
                    if (parts.Length == 2)
                    {
                        var username = parts[0];
                        var domain = parts[1];
                        var maskedUsername = username.Length <= 2 
                            ? username 
                            : username.Substring(0, 2) + new string('*', username.Length - 2);
                        return $"{maskedUsername}@{domain}";
                    }
                    break;
                
                case "creditcard":
                    if (value.Length >= 4)
                    {
                        return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
                    }
                    break;
                
                case "phone":
                    if (value.Length >= 4)
                    {
                        return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
                    }
                    break;
                
                case "ssn":
                    if (value.Length >= 4)
                    {
                        return "XXX-XX-" + value.Substring(value.Length - 4);
                    }
                    break;
                
                case "name":
                case "address":
                case "zipcode":
                case "ip":
                default:
                    if (value.Length > 0)
                    {
                        var visibleChars = Math.Max(1, value.Length / 4);
                        return value.Substring(0, visibleChars) + new string('*', value.Length - visibleChars);
                    }
                    break;
            }

            return new string('*', value.Length);
        }

        /// <summary>
        /// Initializes regex patterns for PII detection
        /// </summary>
        private Dictionary<string, Regex> InitializePatterns()
        {
            var patterns = new Dictionary<string, Regex>();
            
            // Email
            patterns.Add("email", new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled));
            
            // Credit card
            patterns.Add("creditcard", new Regex(@"(?:\d[ -]*?){13,16}", RegexOptions.Compiled));
            
            // Phone number
            patterns.Add("phone", new Regex(@"(?:\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}", RegexOptions.Compiled));
            
            // SSN
            patterns.Add("ssn", new Regex(@"\d{3}-\d{2}-\d{4}", RegexOptions.Compiled));
            
            // IP address
            patterns.Add("ip", new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled));
            
            // ZIP code
            patterns.Add("zipcode", new Regex(@"\b\d{5}(?:-\d{4})?\b", RegexOptions.Compiled));
            
            // Add custom patterns from configuration
            foreach (var pattern in _options.CustomPatterns)
            {
                if (!patterns.ContainsKey(pattern.Key))
                {
                    try
                    {
                        patterns.Add(pattern.Key, new Regex(pattern.Value, RegexOptions.Compiled));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error compiling regex pattern for {PatternKey}: {Pattern}", 
                            pattern.Key, pattern.Value);
                    }
                }
            }
            
            return patterns;
        }
    }

    /// <summary>
    /// Interface for PII detection service
    /// </summary>
    public interface IPIIDetectionService
    {
        IEnumerable<PIIDetection> DetectPII(string text);
        IEnumerable<PIIDetection> DetectPII(Dictionary<string, object> data);
        string MaskPII(string text, PIIMaskingOptions maskingOptions = null);
        Dictionary<string, object> MaskPII(Dictionary<string, object> data, PIIMaskingOptions maskingOptions = null);
    }

    /// <summary>
    /// Options for PII detection
    /// </summary>
    public class PIIOptions
    {
        /// <summary>
        /// Known field names that contain PII
        /// </summary>
        public List<string> KnownPIIFields { get; set; } = new List<string>
        {
            "ssn", "socialSecurityNumber", "taxId", "passport",
            "creditCard", "creditCardNumber", "ccNumber",
            "password", "secret", "apiKey",
            "dob", "dateOfBirth", "birthDate",
            "address", "streetAddress", "mailingAddress",
            "phoneNumber", "phone", "mobileNumber",
            "email", "emailAddress"
        };
        
        /// <summary>
        /// Custom regex patterns for PII detection
        /// </summary>
        public Dictionary<string, string> CustomPatterns { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Default masking options
        /// </summary>
        public PIIMaskingOptions DefaultMaskingOptions { get; set; } = new PIIMaskingOptions
        {
            MaskingType = PIIMaskingType.PartialMask
        };
    }

    /// <summary>
    /// Options for PII masking
    /// </summary>
    public class PIIMaskingOptions
    {
        /// <summary>
        /// Type of masking to apply
        /// </summary>
        public PIIMaskingType MaskingType { get; set; }
        
        /// <summary>
        /// Character to use for masking
        /// </summary>
        public char MaskingChar { get; set; } = '*';
    }

    /// <summary>
    /// Types of PII masking
    /// </summary>
    public enum PIIMaskingType
    {
        /// <summary>
        /// Replace the entire value with a fixed string
        /// </summary>
        Redact,
        
        /// <summary>
        /// Replace the value with a hash
        /// </summary>
        Hash,
        
        /// <summary>
        /// Replace the value with a token
        /// </summary>
        Tokenize,
        
        /// <summary>
        /// Mask part of the value
        /// </summary>
        PartialMask
    }
}
