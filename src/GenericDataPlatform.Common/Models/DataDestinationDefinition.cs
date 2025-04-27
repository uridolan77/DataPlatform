using System.Collections.Generic;

namespace GenericDataPlatform.Common.Models
{
    /// <summary>
    /// Represents a data destination definition
    /// </summary>
    public class DataDestinationDefinition
    {
        /// <summary>
        /// Gets or sets the ID of the data destination
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the data destination
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the data destination
        /// </summary>
        public DataDestinationType Type { get; set; }
        
        /// <summary>
        /// Gets or sets the connection properties for the data destination
        /// </summary>
        public Dictionary<string, string> ConnectionProperties { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Represents the type of data destination
    /// </summary>
    public enum DataDestinationType
    {
        /// <summary>
        /// Database destination
        /// </summary>
        Database,
        
        /// <summary>
        /// File system destination
        /// </summary>
        FileSystem,
        
        /// <summary>
        /// REST API destination
        /// </summary>
        RestApi,
        
        /// <summary>
        /// Message queue destination
        /// </summary>
        MessageQueue,
        
        /// <summary>
        /// Data lake destination
        /// </summary>
        DataLake,
        
        /// <summary>
        /// Blob storage destination
        /// </summary>
        BlobStorage,
        
        /// <summary>
        /// Custom destination
        /// </summary>
        Custom
    }
}
