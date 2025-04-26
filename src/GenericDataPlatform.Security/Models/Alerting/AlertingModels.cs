using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models.Alerting
{
    /// <summary>
    /// Alert rule
    /// </summary>
    public class AlertRule
    {
        /// <summary>
        /// ID of the rule
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the rule
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the rule
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Severity of alerts triggered by this rule
        /// </summary>
        public AlertSeverity Severity { get; set; }
        
        /// <summary>
        /// Name of the metric to monitor
        /// </summary>
        public string MetricName { get; set; }
        
        /// <summary>
        /// Labels to filter the metric
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Operator to compare the metric value with the threshold
        /// </summary>
        public AlertOperator Operator { get; set; }
        
        /// <summary>
        /// Threshold value
        /// </summary>
        public double Threshold { get; set; }
        
        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Whether to auto-resolve alerts when the condition is no longer met
        /// </summary>
        public bool AutoResolve { get; set; } = true;
        
        /// <summary>
        /// Notifications to send when the alert is triggered
        /// </summary>
        public List<AlertNotification> Notifications { get; set; } = new List<AlertNotification>();
        
        /// <summary>
        /// When the rule was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the rule was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
    
    /// <summary>
    /// Alert notification
    /// </summary>
    public class AlertNotification
    {
        /// <summary>
        /// Type of notification
        /// </summary>
        public NotificationType Type { get; set; }
        
        /// <summary>
        /// Recipients of the notification
        /// </summary>
        public List<string> Recipients { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Alert
    /// </summary>
    public class Alert
    {
        /// <summary>
        /// ID of the alert
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// ID of the rule that triggered the alert
        /// </summary>
        public string RuleId { get; set; }
        
        /// <summary>
        /// Name of the alert
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the alert
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Severity of the alert
        /// </summary>
        public AlertSeverity Severity { get; set; }
        
        /// <summary>
        /// Status of the alert
        /// </summary>
        public AlertStatus Status { get; set; } = AlertStatus.Active;
        
        /// <summary>
        /// Name of the metric that triggered the alert
        /// </summary>
        public string MetricName { get; set; }
        
        /// <summary>
        /// Value of the metric that triggered the alert
        /// </summary>
        public double MetricValue { get; set; }
        
        /// <summary>
        /// Labels of the metric that triggered the alert
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// When the alert was triggered
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the alert was resolved
        /// </summary>
        public DateTime? ResolvedAt { get; set; }
        
        /// <summary>
        /// Resolution of the alert
        /// </summary>
        public string Resolution { get; set; }
    }
    
    /// <summary>
    /// Notification
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Type of notification
        /// </summary>
        public NotificationType Type { get; set; }
        
        /// <summary>
        /// Recipients of the notification
        /// </summary>
        public List<string> Recipients { get; set; } = new List<string>();
        
        /// <summary>
        /// Subject of the notification
        /// </summary>
        public string Subject { get; set; }
        
        /// <summary>
        /// Message of the notification
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Additional properties of the notification
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Alert severity
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    /// <summary>
    /// Alert status
    /// </summary>
    public enum AlertStatus
    {
        Active,
        Resolved
    }
    
    /// <summary>
    /// Alert operator
    /// </summary>
    public enum AlertOperator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual
    }
    
    /// <summary>
    /// Notification type
    /// </summary>
    public enum NotificationType
    {
        Email,
        Sms,
        Webhook,
        Slack,
        Teams
    }
}
