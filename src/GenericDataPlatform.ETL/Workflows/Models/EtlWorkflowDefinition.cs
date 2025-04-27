using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents an ETL workflow definition
    /// </summary>
    public class EtlWorkflowDefinition
    {
        /// <summary>
        /// Gets or sets the name of the workflow
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the workflow
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the workflow
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the tags for the workflow
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the extract steps for the workflow
        /// </summary>
        public List<ExtractStepDefinition> ExtractSteps { get; set; } = new List<ExtractStepDefinition>();
        
        /// <summary>
        /// Gets or sets the transform steps for the workflow
        /// </summary>
        public List<TransformStepDefinition> TransformSteps { get; set; } = new List<TransformStepDefinition>();
        
        /// <summary>
        /// Gets or sets the load steps for the workflow
        /// </summary>
        public List<LoadStepDefinition> LoadSteps { get; set; } = new List<LoadStepDefinition>();
        
        /// <summary>
        /// Gets or sets the schedule for the workflow
        /// </summary>
        public WorkflowSchedule Schedule { get; set; }
        
        /// <summary>
        /// Gets or sets the error handling configuration for the workflow
        /// </summary>
        public ErrorHandlingConfiguration ErrorHandling { get; set; }
        
        /// <summary>
        /// Gets or sets the notification configuration for the workflow
        /// </summary>
        public NotificationConfiguration Notifications { get; set; }
    }
    
    /// <summary>
    /// Represents an extract step definition
    /// </summary>
    public class ExtractStepDefinition
    {
        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the step
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the type of extractor to use
        /// </summary>
        public string ExtractorType { get; set; }
        
        /// <summary>
        /// Gets or sets the data source for the step
        /// </summary>
        public GenericDataPlatform.Common.Models.DataSourceDefinition Source { get; set; }
        
        /// <summary>
        /// Gets or sets the configuration for the step
        /// </summary>
        public object Configuration { get; set; }
    }
    
    /// <summary>
    /// Represents a transform step definition
    /// </summary>
    public class TransformStepDefinition
    {
        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the step
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the type of transformer to use
        /// </summary>
        public string TransformerType { get; set; }
        
        /// <summary>
        /// Gets or sets the configuration for the step
        /// </summary>
        public object Configuration { get; set; }
    }
    
    /// <summary>
    /// Represents a load step definition
    /// </summary>
    public class LoadStepDefinition
    {
        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the step
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the type of loader to use
        /// </summary>
        public string LoaderType { get; set; }
        
        /// <summary>
        /// Gets or sets the data destination for the step
        /// </summary>
        public GenericDataPlatform.Common.Models.DataDestinationDefinition Destination { get; set; }
        
        /// <summary>
        /// Gets or sets the configuration for the step
        /// </summary>
        public object Configuration { get; set; }
    }
    
    /// <summary>
    /// Represents a workflow schedule
    /// </summary>
    public class WorkflowSchedule
    {
        /// <summary>
        /// Gets or sets whether the workflow is scheduled
        /// </summary>
        public bool IsScheduled { get; set; }
        
        /// <summary>
        /// Gets or sets the cron expression for the schedule
        /// </summary>
        public string CronExpression { get; set; }
        
        /// <summary>
        /// Gets or sets the time zone for the schedule
        /// </summary>
        public string TimeZone { get; set; }
        
        /// <summary>
        /// Gets or sets the start date for the schedule
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Gets or sets the end date for the schedule
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
    
    /// <summary>
    /// Represents error handling configuration
    /// </summary>
    public class ErrorHandlingConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of retries
        /// </summary>
        public int MaxRetries { get; set; }
        
        /// <summary>
        /// Gets or sets the retry delay in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; }
        
        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries
        /// </summary>
        public bool UseExponentialBackoff { get; set; }
        
        /// <summary>
        /// Gets or sets whether to continue on error
        /// </summary>
        public bool ContinueOnError { get; set; }
        
        /// <summary>
        /// Gets or sets the error handling strategy
        /// </summary>
        public ErrorHandlingStrategy Strategy { get; set; }
    }
    
    /// <summary>
    /// Represents error handling strategy
    /// </summary>
    public enum ErrorHandlingStrategy
    {
        /// <summary>
        /// Stop the workflow on error
        /// </summary>
        StopWorkflow,
        
        /// <summary>
        /// Skip the failed record and continue
        /// </summary>
        SkipRecord,
        
        /// <summary>
        /// Redirect failed records to an error destination
        /// </summary>
        RedirectToErrorDestination
    }
    
    /// <summary>
    /// Represents notification configuration
    /// </summary>
    public class NotificationConfiguration
    {
        /// <summary>
        /// Gets or sets whether to send notifications on success
        /// </summary>
        public bool NotifyOnSuccess { get; set; }
        
        /// <summary>
        /// Gets or sets whether to send notifications on failure
        /// </summary>
        public bool NotifyOnFailure { get; set; }
        
        /// <summary>
        /// Gets or sets the notification channels
        /// </summary>
        public List<NotificationChannel> Channels { get; set; } = new List<NotificationChannel>();
    }
    
    /// <summary>
    /// Represents a notification channel
    /// </summary>
    public class NotificationChannel
    {
        /// <summary>
        /// Gets or sets the type of notification channel
        /// </summary>
        public NotificationChannelType Type { get; set; }
        
        /// <summary>
        /// Gets or sets the recipients for the notification
        /// </summary>
        public List<string> Recipients { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the configuration for the notification channel
        /// </summary>
        public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Represents notification channel type
    /// </summary>
    public enum NotificationChannelType
    {
        /// <summary>
        /// Email notification
        /// </summary>
        Email,
        
        /// <summary>
        /// Slack notification
        /// </summary>
        Slack,
        
        /// <summary>
        /// Microsoft Teams notification
        /// </summary>
        Teams,
        
        /// <summary>
        /// Webhook notification
        /// </summary>
        Webhook
    }
}
