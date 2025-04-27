using System;

namespace GenericDataPlatform.ETL.Models
{
    /// <summary>
    /// Represents a parameter for a rule
    /// </summary>
    public class RuleParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the parameter
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the parameter
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the parameter is required
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
