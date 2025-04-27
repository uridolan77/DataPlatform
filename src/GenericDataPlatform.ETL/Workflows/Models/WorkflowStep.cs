using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a step in a workflow
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Gets or sets the ID of the step
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the step
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the step
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the step
        /// </summary>
        public WorkflowStepType Type { get; set; }

        /// <summary>
        /// Gets or sets the type of the step as a string
        /// </summary>
        public string TypeString
        {
            get => Type.ToString();
            set => Type = value != null ? System.Enum.Parse<WorkflowStepType>(value) : WorkflowStepType.Custom;
        }

        /// <summary>
        /// Gets or sets the IDs of the steps this step depends on
        /// </summary>
        public List<string> DependsOn { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the configuration for the step
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the error handling configuration for the step
        /// </summary>
        public WorkflowStepErrorHandling ErrorHandling { get; set; }

        /// <summary>
        /// Gets or sets the number of times to retry the step if it fails
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds between retries
        /// </summary>
        public int RetryInterval { get; set; }

        /// <summary>
        /// Gets or sets the conditions for executing the step
        /// </summary>
        public List<WorkflowStepCondition> Conditions { get; set; } = new List<WorkflowStepCondition>();
    }
}
