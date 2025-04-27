using System.Collections.Generic;

namespace GenericDataPlatform.ML.Training
{
    /// <summary>
    /// Context for model training
    /// </summary>
    public class TrainingContext
    {
        /// <summary>
        /// ID of the training job
        /// </summary>
        public string JobId { get; set; }
        
        /// <summary>
        /// ID of the MLflow run
        /// </summary>
        public string RunId { get; set; }
        
        /// <summary>
        /// Training parameters
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}
