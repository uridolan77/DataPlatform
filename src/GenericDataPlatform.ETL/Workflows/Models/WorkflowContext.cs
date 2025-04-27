using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents the context for a workflow execution
    /// </summary>
    public class WorkflowContext
    {
        /// <summary>
        /// Gets or sets the ID of the workflow
        /// </summary>
        public string WorkflowId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the execution
        /// </summary>
        public string ExecutionId { get; set; }
        
        /// <summary>
        /// Gets or sets the source of the data
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// Gets or sets the destination of the data
        /// </summary>
        public string Destination { get; set; }
        
        /// <summary>
        /// Gets or sets the parameters for the workflow
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the variables for the workflow
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the metadata for the workflow
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
