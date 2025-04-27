using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Workflows.Tracking
{
    /// <summary>
    /// Represents a data entity in the lineage graph
    /// </summary>
    public class DataEntity
    {
        /// <summary>
        /// Gets or sets the ID of the entity
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the entity
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the entity
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the location of the entity
        /// </summary>
        public string Location { get; set; }
        
        /// <summary>
        /// Gets or sets additional properties for the entity
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
