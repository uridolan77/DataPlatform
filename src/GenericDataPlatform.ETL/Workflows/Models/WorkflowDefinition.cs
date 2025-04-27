using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a workflow definition
    /// </summary>
    public class WorkflowDefinition
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
        /// Gets or sets whether this is the latest version of the workflow
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// Gets or sets the date and time the workflow was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time the workflow was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time the workflow was last modified
        /// </summary>
        public DateTime LastModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the tags for the workflow
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the steps in the workflow
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

        /// <summary>
        /// Gets or sets the activities in the workflow
        /// </summary>
        public List<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();

        /// <summary>
        /// Gets or sets the connections between activities in the workflow
        /// </summary>
        public List<ConnectionDefinition> Connections { get; set; } = new List<ConnectionDefinition>();

        /// <summary>
        /// Gets or sets the variables for the workflow
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the metadata for the workflow
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the error handling configuration for the workflow
        /// </summary>
        public WorkflowErrorHandling ErrorHandling { get; set; }
}
