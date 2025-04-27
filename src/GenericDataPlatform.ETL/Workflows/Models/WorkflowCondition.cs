namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a condition in a workflow
    /// </summary>
    public class WorkflowCondition
    {
        /// <summary>
        /// Gets or sets the ID of the condition
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the expression for the condition
        /// </summary>
        public string Expression { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the condition
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the condition
        /// </summary>
        public WorkflowConditionType Type { get; set; } = WorkflowConditionType.Expression;
    }
    
    /// <summary>
    /// Represents the type of a workflow condition
    /// </summary>
    public enum WorkflowConditionType
    {
        /// <summary>
        /// Expression condition
        /// </summary>
        Expression,
        
        /// <summary>
        /// Script condition
        /// </summary>
        Script,
        
        /// <summary>
        /// Data-based condition
        /// </summary>
        DataBased,
        
        /// <summary>
        /// External condition
        /// </summary>
        External
    }
}
