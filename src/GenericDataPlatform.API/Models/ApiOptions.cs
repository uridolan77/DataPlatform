using System.Collections.Generic;

namespace GenericDataPlatform.API.Models
{
    /// <summary>
    /// API options
    /// </summary>
    public class ApiOptions
    {
        /// <summary>
        /// Connection strings for different database providers
        /// </summary>
        public ConnectionStrings ConnectionStrings { get; set; }
        
        /// <summary>
        /// Service endpoints
        /// </summary>
        public ServiceEndpoints ServiceEndpoints { get; set; }
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
    
    /// <summary>
    /// Service endpoints
    /// </summary>
    public class ServiceEndpoints
    {
        /// <summary>
        /// Ingestion service endpoint
        /// </summary>
        public string IngestionService { get; set; }
        
        /// <summary>
        /// Storage service endpoint
        /// </summary>
        public string StorageService { get; set; }
        
        /// <summary>
        /// Database service endpoint
        /// </summary>
        public string DatabaseService { get; set; }
        
        /// <summary>
        /// ETL service endpoint
        /// </summary>
        public string ETLService { get; set; }
        
        /// <summary>
        /// Compliance service endpoint
        /// </summary>
        public string ComplianceService { get; set; }
    }
}
