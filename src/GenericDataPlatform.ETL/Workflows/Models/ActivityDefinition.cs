using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Models
{
    /// <summary>
    /// Represents an activity definition in a workflow
    /// </summary>
    public class ActivityDefinition
    {
        /// <summary>
        /// Gets or sets the ID of the activity
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the activity
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the activity
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the activity
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the activity
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the properties of the activity
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the possible outcomes of the activity
        /// </summary>
        public List<string> Outcomes { get; set; } = new List<string>();
    }
}
