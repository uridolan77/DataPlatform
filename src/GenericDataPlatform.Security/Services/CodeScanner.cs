using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Service for scanning code for security vulnerabilities
    /// </summary>
    public class CodeScanner : ICodeScanner
    {
        private readonly SecurityOptions _options;
        private readonly ILogger<CodeScanner> _logger;
        private readonly List<SecurityRule> _securityRules;

        public CodeScanner(IOptions<SecurityOptions> options, ILogger<CodeScanner> logger)
        {
            _options = options.Value;
            _logger = logger;
            _securityRules = LoadSecurityRules();
        }

        /// <summary>
        /// Scans a file for security vulnerabilities
        /// </summary>
        public async Task<CodeScanResult> ScanFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Scanning file {FilePath}", filePath);
                
                var result = new CodeScanResult
                {
                    FilePath = filePath,
                    ScanTime = DateTime.UtcNow,
                    Issues = new List<SecurityIssue>()
                };
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File {FilePath} does not exist", filePath);
                    return result;
                }
                
                // Read file content
                var fileContent = await File.ReadAllTextAsync(filePath);
                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                
                // Apply security rules based on file extension
                var applicableRules = _securityRules.Where(r => r.ApplicableExtensions.Contains(fileExtension) || r.ApplicableExtensions.Contains("*"));
                
                foreach (var rule in applicableRules)
                {
                    var matches = rule.Pattern.Matches(fileContent);
                    
                    foreach (Match match in matches)
                    {
                        // Calculate line number
                        var lineNumber = fileContent.Substring(0, match.Index).Count(c => c == '\n') + 1;
                        
                        result.Issues.Add(new SecurityIssue
                        {
                            RuleId = rule.Id,
                            FilePath = filePath,
                            LineNumber = lineNumber,
                            Severity = rule.Severity,
                            Title = rule.Title,
                            Description = rule.Description,
                            CodeSnippet = match.Value,
                            Recommendation = rule.Recommendation
                        });
                    }
                }
                
                _logger.LogInformation("Found {IssueCount} security issues in file {FilePath}", result.Issues.Count, filePath);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Scans a directory for security vulnerabilities
        /// </summary>
        public async Task<List<CodeScanResult>> ScanDirectoryAsync(string directoryPath, string[] fileExtensions = null, bool recursive = true)
        {
            try
            {
                _logger.LogInformation("Scanning directory {DirectoryPath}", directoryPath);
                
                var results = new List<CodeScanResult>();
                
                // Check if directory exists
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning("Directory {DirectoryPath} does not exist", directoryPath);
                    return results;
                }
                
                // Set default file extensions if not provided
                fileExtensions ??= new[] { ".cs", ".cshtml", ".razor", ".js", ".ts", ".sql", ".xml", ".config", ".json" };
                
                // Get all files with specified extensions
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = fileExtensions
                    .SelectMany(ext => Directory.GetFiles(directoryPath, $"*{ext}", searchOption))
                    .ToList();
                
                // Exclude files in excluded directories
                var excludedDirectories = _options.ExcludedDirectories ?? new List<string>();
                files = files.Where(f => !excludedDirectories.Any(d => f.Contains(d))).ToList();
                
                // Scan each file
                foreach (var file in files)
                {
                    var result = await ScanFileAsync(file);
                    results.Add(result);
                }
                
                _logger.LogInformation("Scanned {FileCount} files in directory {DirectoryPath}", files.Count, directoryPath);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning directory {DirectoryPath}", directoryPath);
                throw;
            }
        }

        /// <summary>
        /// Loads security rules from configuration
        /// </summary>
        private List<SecurityRule> LoadSecurityRules()
        {
            try
            {
                var rules = new List<SecurityRule>
                {
                    // SQL Injection
                    new SecurityRule
                    {
                        Id = "SEC001",
                        Title = "Potential SQL Injection",
                        Description = "Detected string concatenation in SQL query, which may lead to SQL injection vulnerabilities.",
                        Severity = VulnerabilitySeverity.High,
                        Pattern = new Regex(@"ExecuteQuery\s*\(\s*[""'].*?\+.*?[""']"),
                        ApplicableExtensions = new[] { ".cs", ".cshtml", ".razor" },
                        Recommendation = "Use parameterized queries or an ORM like Entity Framework."
                    },
                    
                    // Hard-coded credentials
                    new SecurityRule
                    {
                        Id = "SEC002",
                        Title = "Hard-coded Credentials",
                        Description = "Detected hard-coded credentials in the code.",
                        Severity = VulnerabilitySeverity.Critical,
                        Pattern = new Regex(@"(password|pwd|passwd|secret|key|token|apikey)\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase),
                        ApplicableExtensions = new[] { ".cs", ".cshtml", ".razor", ".js", ".ts", ".xml", ".config", ".json" },
                        Recommendation = "Use secure secret management like Azure Key Vault or HashiCorp Vault."
                    },
                    
                    // Cross-site Scripting (XSS)
                    new SecurityRule
                    {
                        Id = "SEC003",
                        Title = "Potential Cross-site Scripting (XSS)",
                        Description = "Detected unencoded output in HTML context, which may lead to XSS vulnerabilities.",
                        Severity = VulnerabilitySeverity.High,
                        Pattern = new Regex(@"@Html\.Raw\(.*?\)"),
                        ApplicableExtensions = new[] { ".cshtml", ".razor" },
                        Recommendation = "Use encoded output with @Model.Property instead of Html.Raw()."
                    },
                    
                    // Insecure deserialization
                    new SecurityRule
                    {
                        Id = "SEC004",
                        Title = "Insecure Deserialization",
                        Description = "Detected use of BinaryFormatter or JavaScriptSerializer, which may lead to insecure deserialization vulnerabilities.",
                        Severity = VulnerabilitySeverity.High,
                        Pattern = new Regex(@"(BinaryFormatter|JavaScriptSerializer)"),
                        ApplicableExtensions = new[] { ".cs" },
                        Recommendation = "Use secure serialization libraries like System.Text.Json or Newtonsoft.Json with type handling disabled."
                    },
                    
                    // CSRF protection missing
                    new SecurityRule
                    {
                        Id = "SEC005",
                        Title = "CSRF Protection Missing",
                        Description = "Detected form submission without anti-forgery token, which may lead to CSRF vulnerabilities.",
                        Severity = VulnerabilitySeverity.Medium,
                        Pattern = new Regex(@"<form.*?>(?!.*@Html\.AntiForgeryToken\(\))"),
                        ApplicableExtensions = new[] { ".cshtml", ".razor" },
                        Recommendation = "Add @Html.AntiForgeryToken() to forms and [ValidateAntiForgeryToken] to controller actions."
                    },
                    
                    // Insecure direct object reference
                    new SecurityRule
                    {
                        Id = "SEC006",
                        Title = "Insecure Direct Object Reference",
                        Description = "Detected direct use of user input for database queries without authorization checks.",
                        Severity = VulnerabilitySeverity.Medium,
                        Pattern = new Regex(@"GetById\s*\(\s*.*?Request\."),
                        ApplicableExtensions = new[] { ".cs", ".cshtml", ".razor" },
                        Recommendation = "Implement proper authorization checks before accessing objects by ID."
                    },
                    
                    // Insecure configuration
                    new SecurityRule
                    {
                        Id = "SEC007",
                        Title = "Insecure Configuration",
                        Description = "Detected insecure configuration settings.",
                        Severity = VulnerabilitySeverity.Medium,
                        Pattern = new Regex(@"(RequireHttps|ValidateAntiForgeryToken|EnableCors)\s*=\s*false", RegexOptions.IgnoreCase),
                        ApplicableExtensions = new[] { ".cs", ".config", ".json" },
                        Recommendation = "Enable security features like HTTPS, CSRF protection, and proper CORS configuration."
                    },
                    
                    // Weak cryptography
                    new SecurityRule
                    {
                        Id = "SEC008",
                        Title = "Weak Cryptography",
                        Description = "Detected use of weak cryptographic algorithms.",
                        Severity = VulnerabilitySeverity.High,
                        Pattern = new Regex(@"(MD5|SHA1|DES|TripleDES|RC2)"),
                        ApplicableExtensions = new[] { ".cs" },
                        Recommendation = "Use strong cryptographic algorithms like AES for encryption and SHA-256 or better for hashing."
                    },
                    
                    // Insecure randomness
                    new SecurityRule
                    {
                        Id = "SEC009",
                        Title = "Insecure Randomness",
                        Description = "Detected use of insecure random number generation.",
                        Severity = VulnerabilitySeverity.Medium,
                        Pattern = new Regex(@"new Random\(\)"),
                        ApplicableExtensions = new[] { ".cs" },
                        Recommendation = "Use cryptographically secure random number generation with System.Security.Cryptography.RandomNumberGenerator."
                    },
                    
                    // Sensitive data exposure
                    new SecurityRule
                    {
                        Id = "SEC010",
                        Title = "Sensitive Data Exposure",
                        Description = "Detected logging of sensitive data.",
                        Severity = VulnerabilitySeverity.High,
                        Pattern = new Regex(@"Log(Information|Warning|Error|Debug|Trace)\s*\(.*?(password|secret|token|key|credit|ssn|social).*?\)", RegexOptions.IgnoreCase),
                        ApplicableExtensions = new[] { ".cs" },
                        Recommendation = "Avoid logging sensitive data or use data masking techniques."
                    }
                };
                
                return rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security rules");
                return new List<SecurityRule>();
            }
        }
    }

    /// <summary>
    /// Interface for code scanner
    /// </summary>
    public interface ICodeScanner
    {
        Task<CodeScanResult> ScanFileAsync(string filePath);
        Task<List<CodeScanResult>> ScanDirectoryAsync(string directoryPath, string[] fileExtensions = null, bool recursive = true);
    }

    /// <summary>
    /// Security rule for code scanning
    /// </summary>
    public class SecurityRule
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public VulnerabilitySeverity Severity { get; set; }
        public Regex Pattern { get; set; }
        public string[] ApplicableExtensions { get; set; }
        public string Recommendation { get; set; }
    }
}
