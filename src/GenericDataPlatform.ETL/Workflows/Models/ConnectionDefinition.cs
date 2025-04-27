namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents a connection between activities in a workflow
    /// </summary>
    public class ConnectionDefinition
    {
        /// <summary>
        /// Gets or sets the ID of the source activity
        /// </summary>
        public string SourceActivityId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the target activity
        /// </summary>
        public string TargetActivityId { get; set; }
        
        /// <summary>
        /// Gets or sets the outcome that triggers this connection
        /// </summary>
        public string Outcome { get; set; }
    }
}
