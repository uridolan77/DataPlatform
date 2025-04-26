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
        
        /// <summary>
        /// SMTP server for sending notifications
        /// </summary>
        public string SmtpServer { get; set; }
        
        /// <summary>
        /// SMTP port for sending notifications
        /// </summary>
        public int SmtpPort { get; set; } = 587;
        
        /// <summary>
        /// SMTP username for sending notifications
        /// </summary>
        public string SmtpUsername { get; set; }
        
        /// <summary>
        /// SMTP password for sending notifications
        /// </summary>
        public string SmtpPassword { get; set; }
        
        /// <summary>
        /// SMTP from address for sending notifications
        /// </summary>
        public string SmtpFromAddress { get; set; }
        
        /// <summary>
        /// SMS API URL for sending notifications
        /// </summary>
        public string SmsApiUrl { get; set; }
        
        /// <summary>
        /// SMS API key for sending notifications
        /// </summary>
        public string SmsApiKey { get; set; }
        
        /// <summary>
        /// SMS from number for sending notifications
        /// </summary>
        public string SmsFromNumber { get; set; }
        
        /// <summary>
        /// Connection strings for different database providers
        /// </summary>
        public ConnectionStrings ConnectionStrings { get; set; }
    }
    
    /// <summary>
    /// Connection strings for different database providers
    /// </summary>
    public class ConnectionStrings
    {
        /// <summary>
        /// SQL Server connection string
        /// </summary>
        public string SqlServer { get; set; }
        
        /// <summary>
        /// PostgreSQL connection string
        /// </summary>
        public string Postgres { get; set; }
        
        /// <summary>
        /// Redis connection string
        /// </summary>
        public string Redis { get; set; }
    }
}
