using System.Collections.Generic;

namespace GenericDataPlatform.Common.Configuration
{
    /// <summary>
    /// Interface for the configuration service
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default);
        
        /// <summary>
        /// Gets a configuration section
        /// </summary>
        T GetSection<T>(string sectionName) where T : class, new();
        
        /// <summary>
        /// Gets a connection string by name
        /// </summary>
        string GetConnectionString(string name);
        
        /// <summary>
        /// Gets all connection strings
        /// </summary>
        Dictionary<string, string> GetAllConnectionStrings();
        
        /// <summary>
        /// Gets a secret value by key
        /// </summary>
        string GetSecret(string key);
    }
}
