using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Service for scanning projects for security vulnerabilities
    /// </summary>
    public class SecurityScanner : ISecurityScanner
    {
        private readonly SecurityOptions _options;
        private readonly IDependencyScanner _dependencyScanner;
        private readonly ICodeScanner _codeScanner;
        private readonly IVulnerabilityRepository _vulnerabilityRepository;
        private readonly ILogger<SecurityScanner> _logger;

        public SecurityScanner(
            IOptions<SecurityOptions> options,
            IDependencyScanner dependencyScanner,
            ICodeScanner codeScanner,
            IVulnerabilityRepository vulnerabilityRepository,
            ILogger<SecurityScanner> logger)
        {
            _options = options.Value;
            _dependencyScanner = dependencyScanner;
            _codeScanner = codeScanner;
            _vulnerabilityRepository = vulnerabilityRepository;
            _logger = logger;
        }

        /// <summary>
        /// Scans a project for security vulnerabilities
        /// </summary>
        public async Task<SecurityScanResult> ScanProjectAsync(string projectPath)
        {
            try
            {
                _logger.LogInformation("Scanning project {ProjectPath}", projectPath);
                
                var result = new SecurityScanResult
                {
                    ProjectPath = projectPath,
                    ScanTime = DateTime.UtcNow,
                    DependencyScanResult = null,
                    CodeScanResults = new List<CodeScanResult>()
                };
                
                // Scan dependencies
                result.DependencyScanResult = await _dependencyScanner.ScanProjectAsync(projectPath);
                
                // Scan code
                var projectDirectory = Path.GetDirectoryName(projectPath);
                result.CodeScanResults = await _codeScanner.ScanDirectoryAsync(projectDirectory);
                
                // Save scan result
                await _vulnerabilityRepository.SaveScanResultAsync(result);
                
                _logger.LogInformation("Completed security scan for project {ProjectPath}", projectPath);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning project {ProjectPath}", projectPath);
                throw;
            }
        }

        /// <summary>
        /// Scans a solution for security vulnerabilities
        /// </summary>
        public async Task<List<SecurityScanResult>> ScanSolutionAsync(string solutionPath)
        {
            try
            {
                _logger.LogInformation("Scanning solution {SolutionPath}", solutionPath);
                
                var results = new List<SecurityScanResult>();
                
                // Find all project files in the solution directory
                var solutionDirectory = Path.GetDirectoryName(solutionPath);
                var projectFiles = Directory.GetFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories);
                
                foreach (var projectFile in projectFiles)
                {
                    var result = await ScanProjectAsync(projectFile);
                    results.Add(result);
                }
                
                _logger.LogInformation("Completed security scan for solution {SolutionPath}", solutionPath);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning solution {SolutionPath}", solutionPath);
                throw;
            }
        }

        /// <summary>
        /// Generates a security report for a project
        /// </summary>
        public async Task<SecurityReport> GenerateReportAsync(string projectPath)
        {
            try
            {
                _logger.LogInformation("Generating security report for project {ProjectPath}", projectPath);
                
                // Get latest scan result
                var scanResult = await _vulnerabilityRepository.GetLatestScanResultAsync(projectPath);
                
                if (scanResult == null)
                {
                    _logger.LogWarning("No scan results found for project {ProjectPath}", projectPath);
                    return null;
                }
                
                var report = new SecurityReport
                {
                    ProjectPath = projectPath,
                    GenerationTime = DateTime.UtcNow,
                    ScanTime = scanResult.ScanTime,
                    Summary = new SecuritySummary(),
                    VulnerabilityDetails = new List<VulnerabilityDetail>(),
                    Recommendations = new List<SecurityRecommendation>()
                };
                
                // Calculate summary
                var vulnerabilities = scanResult.DependencyScanResult?.Vulnerabilities ?? new List<VulnerabilityInfo>();
                var issues = scanResult.CodeScanResults?.SelectMany(r => r.Issues).ToList() ?? new List<SecurityIssue>();
                
                report.Summary.TotalVulnerabilities = vulnerabilities.Count;
                report.Summary.TotalIssues = issues.Count;
                report.Summary.CriticalCount = vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Critical) +
                                              issues.Count(i => i.Severity == VulnerabilitySeverity.Critical);
                report.Summary.HighCount = vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.High) +
                                          issues.Count(i => i.Severity == VulnerabilitySeverity.High);
                report.Summary.MediumCount = vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Medium) +
                                            issues.Count(i => i.Severity == VulnerabilitySeverity.Medium);
                report.Summary.LowCount = vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Low) +
                                         issues.Count(i => i.Severity == VulnerabilitySeverity.Low);
                
                // Add vulnerability details
                foreach (var vulnerability in vulnerabilities)
                {
                    report.VulnerabilityDetails.Add(new VulnerabilityDetail
                    {
                        Id = vulnerability.Id,
                        Title = vulnerability.Title,
                        Description = vulnerability.Description,
                        Severity = vulnerability.Severity,
                        Type = "Dependency",
                        Location = $"{vulnerability.DependencyName} {vulnerability.DependencyVersion}",
                        Recommendation = $"Update {vulnerability.DependencyName} to a non-vulnerable version"
                    });
                }
                
                // Add issue details
                foreach (var issue in issues)
                {
                    report.VulnerabilityDetails.Add(new VulnerabilityDetail
                    {
                        Id = issue.RuleId,
                        Title = issue.Title,
                        Description = issue.Description,
                        Severity = issue.Severity,
                        Type = "Code",
                        Location = $"{issue.FilePath}:{issue.LineNumber}",
                        Recommendation = issue.Recommendation
                    });
                }
                
                // Generate recommendations
                report.Recommendations = GenerateRecommendations(report.VulnerabilityDetails);
                
                _logger.LogInformation("Generated security report for project {ProjectPath}", projectPath);
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating security report for project {ProjectPath}", projectPath);
                throw;
            }
        }

        /// <summary>
        /// Generates recommendations based on vulnerability details
        /// </summary>
        private List<SecurityRecommendation> GenerateRecommendations(List<VulnerabilityDetail> vulnerabilityDetails)
        {
            var recommendations = new List<SecurityRecommendation>();
            
            // Group vulnerabilities by type and severity
            var groupedVulnerabilities = vulnerabilityDetails
                .GroupBy(v => new { v.Type, v.Severity })
                .OrderByDescending(g => g.Key.Severity)
                .ThenBy(g => g.Key.Type);
            
            foreach (var group in groupedVulnerabilities)
            {
                var type = group.Key.Type;
                var severity = group.Key.Severity;
                var count = group.Count();
                
                if (type == "Dependency")
                {
                    // Group dependencies by name
                    var dependencyGroups = group.GroupBy(v => v.Location.Split(' ')[0]);
                    
                    foreach (var dependencyGroup in dependencyGroups)
                    {
                        var dependencyName = dependencyGroup.Key;
                        var dependencyCount = dependencyGroup.Count();
                        
                        recommendations.Add(new SecurityRecommendation
                        {
                            Priority = (int)severity,
                            Title = $"Update {dependencyName} to fix {dependencyCount} {severity.ToString().ToLowerInvariant()} severity vulnerabilities",
                            Description = $"The dependency {dependencyName} has {dependencyCount} {severity.ToString().ToLowerInvariant()} severity vulnerabilities. Update to a non-vulnerable version.",
                            Action = "Update dependency",
                            ImpactedComponents = new List<string> { dependencyName }
                        });
                    }
                }
                else if (type == "Code")
                {
                    // Group code issues by rule ID
                    var ruleGroups = group.GroupBy(v => v.Id);
                    
                    foreach (var ruleGroup in ruleGroups)
                    {
                        var ruleId = ruleGroup.Key;
                        var ruleCount = ruleGroup.Count();
                        var firstIssue = ruleGroup.First();
                        
                        recommendations.Add(new SecurityRecommendation
                        {
                            Priority = (int)severity,
                            Title = $"Fix {ruleCount} {severity.ToString().ToLowerInvariant()} severity {firstIssue.Title} issues",
                            Description = firstIssue.Description,
                            Action = firstIssue.Recommendation,
                            ImpactedComponents = ruleGroup.Select(v => v.Location).ToList()
                        });
                    }
                }
            }
            
            return recommendations;
        }
    }

    /// <summary>
    /// Interface for security scanner
    /// </summary>
    public interface ISecurityScanner
    {
        Task<SecurityScanResult> ScanProjectAsync(string projectPath);
        Task<List<SecurityScanResult>> ScanSolutionAsync(string solutionPath);
        Task<SecurityReport> GenerateReportAsync(string projectPath);
    }
}
