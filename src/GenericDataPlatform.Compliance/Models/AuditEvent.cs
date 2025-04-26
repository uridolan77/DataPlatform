using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Compliance.Models
{
    /// <summary>
    /// Represents an audit event in the system
    /// </summary>
    public class AuditEvent
    {
        /// <summary>
        /// Unique identifier for the audit event
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Type of the audit event (e.g., DataAccess, UserLogin, SchemaChange)
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// ID of the user who performed the action
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// ID of the resource that was accessed or modified
        /// </summary>
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Type of the resource (e.g., DataSource, User, Schema)
        /// </summary>
        public string ResourceType { get; set; }
        
        /// <summary>
        /// Action that was performed (e.g., Read, Create, Update, Delete)
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Status of the action (e.g., Success, Failure, Denied)
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Additional details about the event
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// IP address of the user
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// User agent of the client
        /// </summary>
        public string UserAgent { get; set; }
        
        /// <summary>
        /// Name of the service that generated the event
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Correlation ID for tracking related events
        /// </summary>
        public string CorrelationId { get; set; }
    }
    
    /// <summary>
    /// Filter for searching audit events
    /// </summary>
    public class AuditEventFilter
    {
        /// <summary>
        /// Filter by user ID
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Filter by resource ID
        /// </summary>
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Filter by resource type
        /// </summary>
        public string ResourceType { get; set; }
        
        /// <summary>
        /// Filter by event type
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// Filter by action
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Filter by status
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Filter by start time
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// Filter by end time
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Filter by IP address
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// Filter by service name
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Filter by correlation ID
        /// </summary>
        public string CorrelationId { get; set; }
    }
    
    /// <summary>
    /// Common audit event types
    /// </summary>
    public static class AuditEventTypes
    {
        public const string UserLogin = "UserLogin";
        public const string UserLogout = "UserLogout";
        public const string UserCreated = "UserCreated";
        public const string UserUpdated = "UserUpdated";
        public const string UserDeleted = "UserDeleted";
        public const string DataAccess = "DataAccess";
        public const string DataCreated = "DataCreated";
        public const string DataUpdated = "DataUpdated";
        public const string DataDeleted = "DataDeleted";
        public const string SchemaCreated = "SchemaCreated";
        public const string SchemaUpdated = "SchemaUpdated";
        public const string SchemaDeleted = "SchemaDeleted";
        public const string ConfigurationChanged = "ConfigurationChanged";
        public const string PermissionGranted = "PermissionGranted";
        public const string PermissionRevoked = "PermissionRevoked";
        public const string AccessDenied = "AccessDenied";
    }
    
    /// <summary>
    /// Common audit event statuses
    /// </summary>
    public static class AuditEventStatuses
    {
        public const string Success = "Success";
        public const string Failure = "Failure";
        public const string Denied = "Denied";
        public const string Pending = "Pending";
    }
}
