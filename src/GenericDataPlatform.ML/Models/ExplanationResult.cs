using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ML.Models
{
    /// <summary>
    /// Result of model explanation
    /// </summary>
    public class ExplanationResult
    {
        /// <summary>
        /// Model name
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Model version
        /// </summary>
        public string ModelVersion { get; set; }
        
        /// <summary>
        /// Type of explanation
        /// </summary>
        public ExplanationType ExplanationType { get; set; }
        
        /// <summary>
        /// Global feature importance
        /// </summary>
        public Dictionary<string, double> GlobalFeatureImportance { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Local feature importance for specific instances
        /// </summary>
        public List<LocalExplanation> LocalExplanations { get; set; } = new List<LocalExplanation>();
        
        /// <summary>
        /// Date and time when the explanation was generated
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Local explanation for a specific instance
    /// </summary>
    public class LocalExplanation
    {
        /// <summary>
        /// Input instance
        /// </summary>
        public Dictionary<string, object> Instance { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Prediction for the instance
        /// </summary>
        public Dictionary<string, object> Prediction { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Feature importance for the instance
        /// </summary>
        public Dictionary<string, double> FeatureImportance { get; set; } = new Dictionary<string, double>();
    }
    
    /// <summary>
    /// Type of explanation
    /// </summary>
    public enum ExplanationType
    {
        /// <summary>
        /// SHAP (SHapley Additive exPlanations)
        /// </summary>
        SHAP,
        
        /// <summary>
        /// LIME (Local Interpretable Model-agnostic Explanations)
        /// </summary>
        LIME,
        
        /// <summary>
        /// Permutation Feature Importance
        /// </summary>
        PermutationFeatureImportance,
        
        /// <summary>
        /// Built-in feature importance from the model
        /// </summary>
        BuiltIn
    }
}
