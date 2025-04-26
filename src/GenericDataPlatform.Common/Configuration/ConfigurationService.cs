using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Common.Configuration
{
    /// <summary>
    /// Centralized configuration service for the platform
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationService> _logger;
        
        public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                return _configuration.GetValue<T>(key, defaultValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration value for key {Key}", key);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Gets a configuration section
        /// </summary>
        public T GetSection<T>(string sectionName) where T : class, new()
        {
            try
            {
                var section = _configuration.GetSection(sectionName);
                var result = new T();
                section.Bind(result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration section {SectionName}", sectionName);
                return new T();
            }
        }
        
        /// <summary>
        /// Gets a connection string by name
        /// </summary>
        public string GetConnectionString(string name)
        {
            try
            {
                return _configuration.GetConnectionString(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection string {Name}", name);
                return null;
            }
        }
        
        /// <summary>
        /// Gets all connection strings
        /// </summary>
        public Dictionary<string, string> GetAllConnectionStrings()
        {
            try
            {
                var connectionStrings = new Dictionary<string, string>();
                var section = _configuration.GetSection("ConnectionStrings");
                
                foreach (var child in section.GetChildren())
                {
                    connectionStrings[child.Key] = child.Value;
                }
                
                return connectionStrings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all connection strings");
                return new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// Gets a secret value by key
        /// </summary>
        public string GetSecret(string key)
        {
            try
            {
                // First try to get from user secrets
                var value = _configuration[key];
                
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
                
                // Then try to get from environment variables
                value = Environment.GetEnvironmentVariable(key);
                
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
                
                // Finally, try to get from a secrets file
                var secretsPath = Path.Combine(AppContext.BaseDirectory, "secrets.json");
                
                if (File.Exists(secretsPath))
                {
                    var secretsConfig = new ConfigurationBuilder()
                        .AddJsonFile(secretsPath, optional: true)
                        .Build();
                    
                    return secretsConfig[key];
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret for key {Key}", key);
                return null;
            }
        }
    }
}
