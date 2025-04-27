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
        /// Gets or sets the type of the step
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the IDs of the steps this step depends on
        /// </summary>
        public List<string> DependsOn { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the configuration for the step
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }
}
