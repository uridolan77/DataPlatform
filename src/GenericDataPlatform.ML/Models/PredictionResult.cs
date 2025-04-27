using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Represents the result of a model prediction
    /// </summary>
    public class PredictionResult
    {
        /// <summary>
        /// Unique identifier for the prediction result
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// ID of the prediction request
        /// </summary>
        public string RequestId { get; set; }
        
        /// <summary>
        /// ID of the model used for prediction
        /// </summary>
        public string ModelId { get; set; }
        
        /// <summary>
        /// Prediction output data
        /// </summary>
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Optional batch of output data for multiple predictions
        /// </summary>
        public List<Dictionary<string, object>> BatchResults { get; set; } = new List<Dictionary<string, object>>();
        
        /// <summary>
        /// Whether this is a batch prediction result
        /// </summary>
        public bool IsBatch => BatchResults != null && BatchResults.Count > 0;
        
        /// <summary>
        /// Date and time when the prediction was made
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Prediction confidence score (if applicable)
        /// </summary>
        public double? Confidence { get; set; }
        
        /// <summary>
        /// Additional prediction metrics
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
    }
}
