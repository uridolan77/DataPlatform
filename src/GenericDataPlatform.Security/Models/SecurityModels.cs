using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models
{
    /// <summary>
    /// Security options
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// Path to the solution file
        /// </summary>
        public string SolutionPath { get; set; }
        
        /// <summary>
        /// Directory to store security data
        /// </summary>
        public string DataDirectory { get; set; }
        
        /// <summary>
        /// Scan interval in hours
        /// </summary>
        public int ScanIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Directories to exclude from scanning
        /// </summary>
        public List<string> ExcludedDirectories { get; set; } = new List<string> { "bin", "obj", "node_modules", "wwwroot/lib" };
        
        /// <summary>
        /// Whether to enable automatic vulnerability database updates
        /// </summary>
        public bool EnableAutomaticUpdates { get; set; } = true;
        
        /// <summary>
        /// URL for the vulnerability database
        /// </summary>
        public string VulnerabilityDatabaseUrl { get; set; } = "https://osv.dev/api/v1";
    }
    
    /// <summary>
    /// Dependency scan result
    /// </summary>
    public class DependencyScanResult
    {
        /// <summary>
        /// Path to the project
        /// </summary>
        public string ProjectPath { get; set; }
        
        /// <summary>
        /// Time of the scan
        /// </summary>
        public DateTime ScanTime { get; set; }
        
        /// <summary>
        /// Dependencies found in the project
        /// </summary>
        public List<DependencyInfo> Dependencies { get; set; } = new List<DependencyInfo>();
        
        /// <summary>
        /// Vulnerabilities found in the dependencies
        /// </summary>
        public List<VulnerabilityInfo> Vulnerabilities { get; set; } = new List<VulnerabilityInfo>();
    }
    
    /// <summary>
    /// Dependency information
    /// </summary>
    public class DependencyInfo
    {
        /// <summary>
        /// Name of the dependency
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Version of the dependency
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Type of the dependency (e.g., NuGet, npm)
        /// </summary>
        public string Type { get; set; }
    }
    
    /// <summary>
    /// Vulnerability information
    /// </summary>
    public class VulnerabilityInfo
    {
        /// <summary>
        /// ID of the vulnerability
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the affected dependency
        /// </summary>
        public string DependencyName { get; set; }
        
        /// <summary>
        /// Version of the affected dependency
        /// </summary>
        public string DependencyVersion { get; set; }
        
        /// <summary>
        /// Affected versions
        /// </summary>
        public List<string> AffectedVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// Title of the vulnerability
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of the vulnerability
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Severity of the vulnerability
        /// </summary>
        public VulnerabilitySeverity Severity { get; set; }
        
        /// <summary>
        /// CVSS score
        /// </summary>
        public double? CVSS { get; set; }
        
        /// <summary>
        /// Date the vulnerability was published
        /// </summary>
        public DateTime PublishedDate { get; set; }
        
        /// <summary>
        /// References to more information
        /// </summary>
        public List<string> References { get; set; } = new List<string>();
        
        /// <summary>
        /// Source of the vulnerability information
        /// </summary>
        public string Source { get; set; }
    }
    
    /// <summary>
    /// Code scan result
    /// </summary>
    public class CodeScanResult
    {
        /// <summary>
        /// Path to the file
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Time of the scan
        /// </summary>
        public DateTime ScanTime { get; set; }
        
        /// <summary>
        /// Security issues found in the file
        /// </summary>
        public List<SecurityIssue> Issues { get; set; } = new List<SecurityIssue>();
    }
    
    /// <summary>
    /// Security issue
    /// </summary>
    public class SecurityIssue
    {
        /// <summary>
        /// ID of the rule that detected the issue
        /// </summary>
        public string RuleId { get; set; }
        
        /// <summary>
        /// Path to the file containing the issue
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Line number where the issue was found
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Severity of the issue
        /// </summary>
        public VulnerabilitySeverity Severity { get; set; }
        
        /// <summary>
        /// Title of the issue
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Code snippet containing the issue
        /// </summary>
        public string CodeSnippet { get; set; }
        
        /// <summary>
        /// Recommendation for fixing the issue
        /// </summary>
        public string Recommendation { get; set; }
    }
    
    /// <summary>
    /// Security scan result
    /// </summary>
    public class SecurityScanResult
    {
        /// <summary>
        /// Path to the project
        /// </summary>
        public string ProjectPath { get; set; }
        
        /// <summary>
        /// Time of the scan
        /// </summary>
        public DateTime ScanTime { get; set; }
        
        /// <summary>
        /// Dependency scan result
        /// </summary>
        public DependencyScanResult DependencyScanResult { get; set; }
        
        /// <summary>
        /// Code scan results
        /// </summary>
        public List<CodeScanResult> CodeScanResults { get; set; } = new List<CodeScanResult>();
    }
    
    /// <summary>
    /// Security report
    /// </summary>
    public class SecurityReport
    {
        /// <summary>
        /// Path to the project
        /// </summary>
        public string ProjectPath { get; set; }
        
        /// <summary>
        /// Time the report was generated
        /// </summary>
        public DateTime GenerationTime { get; set; }
        
        /// <summary>
        /// Time of the scan
        /// </summary>
        public DateTime ScanTime { get; set; }
        
        /// <summary>
        /// Summary of the security issues
        /// </summary>
        public SecuritySummary Summary { get; set; }
        
        /// <summary>
        /// Details of the vulnerabilities
        /// </summary>
        public List<VulnerabilityDetail> VulnerabilityDetails { get; set; } = new List<VulnerabilityDetail>();
        
        /// <summary>
        /// Recommendations for fixing the issues
        /// </summary>
        public List<SecurityRecommendation> Recommendations { get; set; } = new List<SecurityRecommendation>();
    }
    
    /// <summary>
    /// Security summary
    /// </summary>
    public class SecuritySummary
    {
        /// <summary>
        /// Total number of vulnerabilities
        /// </summary>
        public int TotalVulnerabilities { get; set; }
        
        /// <summary>
        /// Total number of issues
        /// </summary>
        public int TotalIssues { get; set; }
        
        /// <summary>
        /// Number of critical issues
        /// </summary>
        public int CriticalCount { get; set; }
        
        /// <summary>
        /// Number of high severity issues
        /// </summary>
        public int HighCount { get; set; }
        
        /// <summary>
        /// Number of medium severity issues
        /// </summary>
        public int MediumCount { get; set; }
        
        /// <summary>
        /// Number of low severity issues
        /// </summary>
        public int LowCount { get; set; }
        
        /// <summary>
        /// Overall security score (0-100)
        /// </summary>
        public int Score => CalculateScore();
        
        /// <summary>
        /// Calculates the security score
        /// </summary>
        private int CalculateScore()
        {
            var total = TotalVulnerabilities + TotalIssues;
            if (total == 0)
            {
                return 100;
            }
            
            var weightedIssues = CriticalCount * 10 + HighCount * 5 + MediumCount * 2 + LowCount;
            var maxScore = total * 10; // Maximum possible weighted score
            
            return Math.Max(0, 100 - (int)Math.Round((double)weightedIssues / maxScore * 100));
        }
    }
    
    /// <summary>
    /// Vulnerability detail
    /// </summary>
    public class VulnerabilityDetail
    {
        /// <summary>
        /// ID of the vulnerability
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Title of the vulnerability
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of the vulnerability
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Severity of the vulnerability
        /// </summary>
        public VulnerabilitySeverity Severity { get; set; }
        
        /// <summary>
        /// Type of the vulnerability (Dependency or Code)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Location of the vulnerability
        /// </summary>
        public string Location { get; set; }
        
        /// <summary>
        /// Recommendation for fixing the vulnerability
        /// </summary>
        public string Recommendation { get; set; }
    }
    
    /// <summary>
    /// Security recommendation
    /// </summary>
    public class SecurityRecommendation
    {
        /// <summary>
        /// Priority of the recommendation (1-4, where 1 is highest)
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Title of the recommendation
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of the recommendation
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Action to take
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Components impacted by the recommendation
        /// </summary>
        public List<string> ImpactedComponents { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Vulnerability severity
    /// </summary>
    public enum VulnerabilitySeverity
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}
