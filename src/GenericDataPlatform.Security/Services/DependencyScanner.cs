using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Service for scanning dependencies for vulnerabilities
    /// </summary>
    public class DependencyScanner : IDependencyScanner
    {
        private readonly SecurityOptions _options;
        private readonly IVulnerabilityRepository _vulnerabilityRepository;
        private readonly ILogger<DependencyScanner> _logger;
        private readonly HttpClient _httpClient;
        private readonly SourceCacheContext _cacheContext;
        private readonly SourceRepository _nugetRepository;

        public DependencyScanner(
            IOptions<SecurityOptions> options,
            IVulnerabilityRepository vulnerabilityRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<DependencyScanner> logger)
        {
            _options = options.Value;
            _vulnerabilityRepository = vulnerabilityRepository;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("SecurityScanner");
            
            // Initialize NuGet repository
            _cacheContext = new SourceCacheContext();
            _nugetRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        }

        /// <summary>
        /// Scans dependencies in a project
        /// </summary>
        public async Task<DependencyScanResult> ScanProjectAsync(string projectPath)
        {
            try
            {
                _logger.LogInformation("Scanning dependencies in project {ProjectPath}", projectPath);
                
                var result = new DependencyScanResult
                {
                    ProjectPath = projectPath,
                    ScanTime = DateTime.UtcNow,
                    Dependencies = new List<DependencyInfo>(),
                    Vulnerabilities = new List<VulnerabilityInfo>()
                };
                
                // Parse project file to extract dependencies
                var dependencies = await ParseProjectFileAsync(projectPath);
                result.Dependencies.AddRange(dependencies);
                
                // Check each dependency for vulnerabilities
                foreach (var dependency in dependencies)
                {
                    var vulnerabilities = await CheckDependencyVulnerabilitiesAsync(dependency);
                    result.Vulnerabilities.AddRange(vulnerabilities);
                }
                
                _logger.LogInformation("Found {DependencyCount} dependencies and {VulnerabilityCount} vulnerabilities in project {ProjectPath}",
                    result.Dependencies.Count, result.Vulnerabilities.Count, projectPath);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning dependencies in project {ProjectPath}", projectPath);
                throw;
            }
        }

        /// <summary>
        /// Scans all dependencies in a solution
        /// </summary>
        public async Task<List<DependencyScanResult>> ScanSolutionAsync(string solutionPath)
        {
            try
            {
                _logger.LogInformation("Scanning dependencies in solution {SolutionPath}", solutionPath);
                
                var results = new List<DependencyScanResult>();
                
                // Find all project files in the solution directory
                var solutionDirectory = Path.GetDirectoryName(solutionPath);
                var projectFiles = Directory.GetFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories);
                
                foreach (var projectFile in projectFiles)
                {
                    var result = await ScanProjectAsync(projectFile);
                    results.Add(result);
                }
                
                _logger.LogInformation("Scanned {ProjectCount} projects in solution {SolutionPath}", results.Count, solutionPath);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning dependencies in solution {SolutionPath}", solutionPath);
                throw;
            }
        }

        /// <summary>
        /// Parses a project file to extract dependencies
        /// </summary>
        private async Task<List<DependencyInfo>> ParseProjectFileAsync(string projectPath)
        {
            try
            {
                var dependencies = new List<DependencyInfo>();
                
                // Read project file
                var projectContent = await File.ReadAllTextAsync(projectPath);
                
                // Extract PackageReference elements
                var packageReferences = System.Text.RegularExpressions.Regex.Matches(projectContent, 
                    @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""");
                
                foreach (System.Text.RegularExpressions.Match match in packageReferences)
                {
                    var packageName = match.Groups[1].Value;
                    var packageVersion = match.Groups[2].Value;
                    
                    dependencies.Add(new DependencyInfo
                    {
                        Name = packageName,
                        Version = packageVersion,
                        Type = "NuGet"
                    });
                }
                
                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing project file {ProjectPath}", projectPath);
                throw;
            }
        }

        /// <summary>
        /// Checks a dependency for vulnerabilities
        /// </summary>
        private async Task<List<VulnerabilityInfo>> CheckDependencyVulnerabilitiesAsync(DependencyInfo dependency)
        {
            try
            {
                var vulnerabilities = new List<VulnerabilityInfo>();
                
                // Check local vulnerability database
                var localVulnerabilities = await _vulnerabilityRepository.GetVulnerabilitiesForDependencyAsync(dependency.Name, dependency.Version);
                vulnerabilities.AddRange(localVulnerabilities);
                
                // Check NuGet for vulnerabilities
                if (dependency.Type == "NuGet")
                {
                    var nugetVulnerabilities = await CheckNuGetVulnerabilitiesAsync(dependency.Name, dependency.Version);
                    vulnerabilities.AddRange(nugetVulnerabilities);
                }
                
                // Check OSV database for vulnerabilities
                var osvVulnerabilities = await CheckOSVVulnerabilitiesAsync(dependency.Name, dependency.Version);
                vulnerabilities.AddRange(osvVulnerabilities);
                
                return vulnerabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking vulnerabilities for dependency {DependencyName} {DependencyVersion}",
                    dependency.Name, dependency.Version);
                return new List<VulnerabilityInfo>();
            }
        }

        /// <summary>
        /// Checks NuGet for vulnerabilities
        /// </summary>
        private async Task<List<VulnerabilityInfo>> CheckNuGetVulnerabilitiesAsync(string packageName, string packageVersion)
        {
            try
            {
                var vulnerabilities = new List<VulnerabilityInfo>();
                
                // Get package metadata
                var resource = await _nugetRepository.GetResourceAsync<PackageMetadataResource>();
                var metadata = await resource.GetMetadataAsync(packageName, includePrerelease: false, includeUnlisted: false, _cacheContext, NuGet.Common.NullLogger.Instance, CancellationToken.None);
                
                var package = metadata.FirstOrDefault(p => p.Identity.Version.ToString() == packageVersion);
                if (package == null)
                {
                    return vulnerabilities;
                }
                
                // Check for vulnerabilities in package metadata
                if (package.Vulnerabilities != null)
                {
                    foreach (var vulnerability in package.Vulnerabilities)
                    {
                        vulnerabilities.Add(new VulnerabilityInfo
                        {
                            Id = vulnerability.AdvisoryUrl,
                            DependencyName = packageName,
                            DependencyVersion = packageVersion,
                            Title = vulnerability.AdvisoryUrl,
                            Description = vulnerability.AdvisoryUrl,
                            Severity = MapSeverity(vulnerability.Severity),
                            CVSS = null,
                            PublishedDate = DateTime.UtcNow,
                            References = new List<string> { vulnerability.AdvisoryUrl },
                            Source = "NuGet"
                        });
                    }
                }
                
                return vulnerabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking NuGet vulnerabilities for package {PackageName} {PackageVersion}",
                    packageName, packageVersion);
                return new List<VulnerabilityInfo>();
            }
        }

        /// <summary>
        /// Checks OSV database for vulnerabilities
        /// </summary>
        private async Task<List<VulnerabilityInfo>> CheckOSVVulnerabilitiesAsync(string packageName, string packageVersion)
        {
            try
            {
                var vulnerabilities = new List<VulnerabilityInfo>();
                
                // Query OSV API
                var requestBody = new
                {
                    package = new
                    {
                        name = packageName,
                        ecosystem = "NuGet"
                    },
                    version = packageVersion
                };
                
                var response = await _httpClient.PostAsJsonAsync("https://api.osv.dev/v1/query", requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    return vulnerabilities;
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var osvResponse = JsonSerializer.Deserialize<OSVResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (osvResponse?.Vulns == null)
                {
                    return vulnerabilities;
                }
                
                foreach (var vuln in osvResponse.Vulns)
                {
                    vulnerabilities.Add(new VulnerabilityInfo
                    {
                        Id = vuln.Id,
                        DependencyName = packageName,
                        DependencyVersion = packageVersion,
                        Title = vuln.Summary,
                        Description = vuln.Details,
                        Severity = MapSeverity(vuln.Severity),
                        CVSS = vuln.Cvss?.Score,
                        PublishedDate = vuln.Published,
                        References = vuln.References?.Select(r => r.Url).ToList() ?? new List<string>(),
                        Source = "OSV"
                    });
                }
                
                return vulnerabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking OSV vulnerabilities for package {PackageName} {PackageVersion}",
                    packageName, packageVersion);
                return new List<VulnerabilityInfo>();
            }
        }

        /// <summary>
        /// Maps severity string to VulnerabilitySeverity enum
        /// </summary>
        private VulnerabilitySeverity MapSeverity(string severity)
        {
            if (string.IsNullOrEmpty(severity))
            {
                return VulnerabilitySeverity.Unknown;
            }
            
            return severity.ToLowerInvariant() switch
            {
                "critical" => VulnerabilitySeverity.Critical,
                "high" => VulnerabilitySeverity.High,
                "moderate" => VulnerabilitySeverity.Medium,
                "medium" => VulnerabilitySeverity.Medium,
                "low" => VulnerabilitySeverity.Low,
                _ => VulnerabilitySeverity.Unknown
            };
        }

        /// <summary>
        /// OSV API response model
        /// </summary>
        private class OSVResponse
        {
            public List<OSVVulnerability> Vulns { get; set; }
        }

        /// <summary>
        /// OSV vulnerability model
        /// </summary>
        private class OSVVulnerability
        {
            public string Id { get; set; }
            public string Summary { get; set; }
            public string Details { get; set; }
            public string Severity { get; set; }
            public OSVCvss Cvss { get; set; }
            public DateTime Published { get; set; }
            public List<OSVReference> References { get; set; }
        }

        /// <summary>
        /// OSV CVSS model
        /// </summary>
        private class OSVCvss
        {
            public double Score { get; set; }
            public string Vector { get; set; }
        }

        /// <summary>
        /// OSV reference model
        /// </summary>
        private class OSVReference
        {
            public string Type { get; set; }
            public string Url { get; set; }
        }
    }

    /// <summary>
    /// Interface for dependency scanner
    /// </summary>
    public interface IDependencyScanner
    {
        Task<DependencyScanResult> ScanProjectAsync(string projectPath);
        Task<List<DependencyScanResult>> ScanSolutionAsync(string solutionPath);
    }
}
