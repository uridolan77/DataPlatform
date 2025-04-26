using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Background service for scheduled security scanning
    /// </summary>
    public class SecurityScanBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SecurityOptions _options;
        private readonly ILogger<SecurityScanBackgroundService> _logger;

        public SecurityScanBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<SecurityOptions> options,
            ILogger<SecurityScanBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Security scan background service is starting");

            // Wait for the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run security scan
                    await RunSecurityScanAsync();

                    // Wait for the next scan interval
                    var scanInterval = _options.ScanIntervalHours > 0
                        ? TimeSpan.FromHours(_options.ScanIntervalHours)
                        : TimeSpan.FromHours(24); // Default to 24 hours

                    _logger.LogInformation("Next security scan scheduled in {ScanInterval}", scanInterval);
                    await Task.Delay(scanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running security scan");
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
            }

            _logger.LogInformation("Security scan background service is stopping");
        }

        /// <summary>
        /// Runs a security scan
        /// </summary>
        private async Task RunSecurityScanAsync()
        {
            _logger.LogInformation("Starting scheduled security scan");

            using var scope = _serviceProvider.CreateScope();
            var securityScanner = scope.ServiceProvider.GetRequiredService<ISecurityScanner>();

            // Get solution path
            var solutionPath = _options.SolutionPath;
            if (string.IsNullOrEmpty(solutionPath))
            {
                // Try to find solution file in the current directory
                var currentDirectory = AppContext.BaseDirectory;
                var solutionFiles = Directory.GetFiles(currentDirectory, "*.sln", SearchOption.AllDirectories);

                if (solutionFiles.Length > 0)
                {
                    solutionPath = solutionFiles[0];
                }
                else
                {
                    _logger.LogWarning("No solution file found for security scanning");
                    return;
                }
            }

            // Run security scan
            var results = await securityScanner.ScanSolutionAsync(solutionPath);

            _logger.LogInformation("Completed scheduled security scan for {ProjectCount} projects", results.Count);

            // Generate reports
            foreach (var result in results)
            {
                var report = await securityScanner.GenerateReportAsync(result.ProjectPath);
                _logger.LogInformation("Generated security report for project {ProjectPath}", result.ProjectPath);
            }
        }
    }
}
