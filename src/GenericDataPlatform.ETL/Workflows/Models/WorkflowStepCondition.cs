using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a condition for executing a workflow step
    /// </summary>
    public class WorkflowStepCondition
    {
        /// <summary>
        /// Gets or sets the type of condition
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the field to evaluate
        /// </summary>
        public string Field { get; set; }
        
        /// <summary>
        /// Gets or sets the operator for the condition
        /// </summary>
        public string Operator { get; set; }
        
        /// <summary>
        /// Gets or sets the value to compare against
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Gets or sets the source of the field (e.g., "context", "step", "variable")
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the step to get the field from (if Source is "step")
        /// </summary>
        public string StepId { get; set; }
        
        /// <summary>
        /// Gets or sets additional parameters for the condition
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
