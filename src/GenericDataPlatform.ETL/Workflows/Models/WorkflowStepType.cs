namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents the type of a workflow step
    /// </summary>
    public enum WorkflowStepType
    {
        /// <summary>
        /// Extract step
        /// </summary>
        Extract,
        
        /// <summary>
        /// Transform step
        /// </summary>
        Transform,
        
        /// <summary>
        /// Load step
        /// </summary>
        Load,
        
        /// <summary>
        /// Validate step
        /// </summary>
        Validate,
        
        /// <summary>
        /// Enrich step
        /// </summary>
        Enrich,
        
        /// <summary>
        /// Branch step
        /// </summary>
        Branch,
        
        /// <summary>
        /// Join step
        /// </summary>
        Join,
        
        /// <summary>
        /// Custom step
        /// </summary>
        Custom
    }
}
