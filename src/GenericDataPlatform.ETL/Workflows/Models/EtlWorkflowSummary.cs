using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a summary of an ETL workflow
    /// </summary>
    public class EtlWorkflowSummary
    {
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string Id { get; set; }
        
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
        /// Gets or sets the version of the workflow
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Gets or sets whether the workflow is published
        /// </summary>
        public bool IsPublished { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time the workflow was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time the workflow was last modified
        /// </summary>
        public DateTime LastModifiedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time the workflow was last executed
        /// </summary>
        public DateTime? LastExecutedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the status of the last execution
        /// </summary>
        public WorkflowStatus? LastExecutionStatus { get; set; }
        
        /// <summary>
        /// Gets or sets the tags for the workflow
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the number of extract steps in the workflow
        /// </summary>
        public int ExtractStepCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of transform steps in the workflow
        /// </summary>
        public int TransformStepCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of load steps in the workflow
        /// </summary>
        public int LoadStepCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether the workflow is scheduled
        /// </summary>
        public bool IsScheduled { get; set; }
        
        /// <summary>
        /// Gets or sets the next scheduled execution time
        /// </summary>
        public DateTime? NextScheduledExecution { get; set; }
    }
}
