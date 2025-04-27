namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents metrics for a workflow error
    /// </summary>
    public class ErrorMetrics
    {
        /// <summary>
        /// Gets or sets the error type
        /// </summary>
        public string ErrorType { get; set; }
        
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public string ErrorCode { get; set; }
        
        /// <summary>
        /// Gets or sets the number of occurrences
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Gets or sets the percentage of total errors
        /// </summary>
        public double Percentage { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the step where the error occurs most frequently
        /// </summary>
        public string MostFrequentStepId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the step where the error occurs most frequently
        /// </summary>
        public string MostFrequentStepName { get; set; }
    }
}
