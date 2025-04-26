using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Security.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Common.Configuration
{
    /// <summary>
    /// Centralized configuration service that combines configuration from multiple sources
    /// </summary>
    public class CentralizedConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger<CentralizedConfigurationService> _logger;
        private readonly Dictionary<string, object> _configCache = new Dictionary<string, object>();
        private readonly Dictionary<string, string> _secretCache = new Dictionary<string, string>();
        private readonly ConfigurationOptions _options;

        public CentralizedConfigurationService(
            IConfiguration configuration,
            ISecretProvider secretProvider,
            IOptions<ConfigurationOptions> options,
            ILogger<CentralizedConfigurationService> logger)
        {
            _configuration = configuration;
            _secretProvider = secretProvider;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                // Check cache first
                if (_configCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
                {
                    return typedValue;
                }

                // Try to get from configuration
                var value = _configuration.GetValue<T>(key);

                // If value is not found and we should check Vault
                if (EqualityComparer<T>.Default.Equals(value, default) && _options.CheckVaultForConfig)
                {
                    // Try to get from Vault
                    var secretKey = $"config/{key}";
                    var secretValue = GetSecretAsync(secretKey).GetAwaiter().GetResult();

                    if (!string.IsNullOrEmpty(secretValue))
                    {
                        // Convert string to T
                        value = (T)Convert.ChangeType(secretValue, typeof(T));
                    }
                }

                // If still not found, use default
                if (EqualityComparer<T>.Default.Equals(value, default))
                {
                    value = defaultValue;
                }

                // Cache the value
                _configCache[key] = value;

                return value;
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
                // Check cache first
                if (_configCache.TryGetValue(sectionName, out var cachedValue) && cachedValue is T typedValue)
                {
                    return typedValue;
                }

                // Get from configuration
                var section = _configuration.GetSection(sectionName);
                var value = section.Get<T>() ?? new T();

                // Cache the value
                _configCache[sectionName] = value;

                return value;
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
                // Check if we should use Vault for connection strings
                if (_options.UseVaultForConnectionStrings)
                {
                    // Try to get from Vault
                    var secretKey = $"connectionstrings/{name}";
                    var connectionString = GetSecretAsync(secretKey).GetAwaiter().GetResult();

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        return connectionString;
                    }
                }

                // Fall back to configuration
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

                // Get from configuration
                var connectionStringSection = _configuration.GetSection("ConnectionStrings");
                foreach (var child in connectionStringSection.GetChildren())
                {
                    connectionStrings[child.Key] = child.Value;
                }

                // If we should use Vault for connection strings
                if (_options.UseVaultForConnectionStrings)
                {
                    // Try to get from Vault
                    var vaultConnectionStrings = GetSecretAsync("connectionstrings").GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(vaultConnectionStrings))
                    {
                        try
                        {
                            // Parse JSON
                            var vaultDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(vaultConnectionStrings);
                            foreach (var kvp in vaultDict)
                            {
                                connectionStrings[kvp.Key] = kvp.Value;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error parsing connection strings from Vault");
                        }
                    }
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
            return GetSecretAsync(key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets a secret value by key asynchronously
        /// </summary>
        private async Task<string> GetSecretAsync(string key)
        {
            try
            {
                // Check cache first
                if (_secretCache.TryGetValue(key, out var cachedSecret))
                {
                    return cachedSecret;
                }

                // Get from secret provider
                var secret = await _secretProvider.GetSecretAsync(key);

                // Cache the secret
                if (!string.IsNullOrEmpty(secret))
                {
                    _secretCache[key] = secret;
                }

                return secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Clears the configuration cache
        /// </summary>
        public void ClearCache()
        {
            _configCache.Clear();
            _secretCache.Clear();
        }
    }

    /// <summary>
    /// Options for the centralized configuration service
    /// </summary>
    public class ConfigurationOptions
    {
        /// <summary>
        /// Whether to use Vault for connection strings
        /// </summary>
        public bool UseVaultForConnectionStrings { get; set; } = true;

        /// <summary>
        /// Whether to check Vault for configuration values
        /// </summary>
        public bool CheckVaultForConfig { get; set; } = true;

        /// <summary>
        /// Cache timeout in seconds
        /// </summary>
        public int CacheTimeoutSeconds { get; set; } = 300;
    }
}
