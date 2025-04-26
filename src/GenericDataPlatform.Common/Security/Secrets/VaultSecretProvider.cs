using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace GenericDataPlatform.Common.Security.Secrets
{
    /// <summary>
    /// Secret provider using HashiCorp Vault
    /// </summary>
    public class VaultSecretProvider : ISecretProvider
    {
        private readonly VaultOptions _options;
        private readonly ILogger<VaultSecretProvider> _logger;
        private readonly IVaultClient _vaultClient;
        private readonly Dictionary<string, string> _secretCache = new Dictionary<string, string>();

        public VaultSecretProvider(IOptions<VaultOptions> options, ILogger<VaultSecretProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _vaultClient = CreateVaultClient();
        }

        /// <summary>
        /// Gets a secret by key
        /// </summary>
        public async Task<string> GetSecretAsync(string key)
        {
            try
            {
                // Check cache first
                if (_secretCache.TryGetValue(key, out var cachedSecret))
                {
                    return cachedSecret;
                }
                
                // Parse the key to get the path and field
                var (path, field) = ParseKey(key);
                
                // Get the secret from Vault
                var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path,
                    mountPoint: _options.SecretsEnginePath);
                
                if (secret?.Data?.Data != null && secret.Data.Data.TryGetValue(field, out var value) && value != null)
                {
                    var secretValue = value.ToString();
                    
                    // Cache the secret if it's not null or empty
                    if (!string.IsNullOrEmpty(secretValue))
                    {
                        _secretCache[key] = secretValue;
                    }
                    
                    return secretValue;
                }
                
                _logger.LogWarning("Secret not found: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Sets a secret
        /// </summary>
        public async Task SetSecretAsync(string key, string value)
        {
            try
            {
                // Parse the key to get the path and field
                var (path, field) = ParseKey(key);
                
                // Get the current secret data (if any)
                Dictionary<string, object> data;
                try
                {
                    var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                        path,
                        mountPoint: _options.SecretsEnginePath);
                    
                    // Fix: Use explicit cast to convert IDictionary to Dictionary
                    data = secret?.Data?.Data != null ? 
                           new Dictionary<string, object>(secret.Data.Data) : 
                           new Dictionary<string, object>();
                }
                catch
                {
                    data = new Dictionary<string, object>();
                }
                
                // Update the field
                data[field] = value;
                
                // Write the secret back to Vault
                await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                    path,
                    data,
                    mountPoint: _options.SecretsEnginePath);
                
                // Update cache
                _secretCache[key] = value;
                
                _logger.LogInformation("Secret set: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting secret: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Deletes a secret
        /// </summary>
        public async Task DeleteSecretAsync(string key)
        {
            try
            {
                // Parse the key to get the path and field
                var (path, field) = ParseKey(key);
                
                // Get the current secret data
                Dictionary<string, object> data;
                try
                {
                    var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                        path, 
                        mountPoint: _options.SecretsEnginePath);
                    
                    // Fix: Use explicit cast to convert IDictionary to Dictionary
                    data = secret?.Data?.Data != null ? 
                           new Dictionary<string, object>(secret.Data.Data) : 
                           new Dictionary<string, object>();
                }
                catch
                {
                    // If the secret doesn't exist, there's nothing to delete
                    return;
                }
                
                // Remove the field
                if (data.ContainsKey(field))
                {
                    data.Remove(field);
                    
                    // If there are still other fields, update the secret
                    if (data.Count > 0)
                    {
                        await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                            path,
                            data,
                            mountPoint: _options.SecretsEnginePath);
                    }
                    else
                    {
                        // If there are no more fields, delete the entire secret
                        await _vaultClient.V1.Secrets.KeyValue.V2.DeleteSecretAsync(
                            path,
                            mountPoint: _options.SecretsEnginePath);
                    }
                }
                
                // Remove from cache
                _secretCache.Remove(key);
                
                _logger.LogInformation("Secret deleted: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Lists all secrets in a path
        /// </summary>
        public async Task<IEnumerable<string>> ListSecretsAsync(string path)
        {
            try
            {
                var result = new List<string>();
                
                // List secrets in the path
                var secrets = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(
                    path,
                    mountPoint: _options.SecretsEnginePath);
                
                if (secrets?.Data?.Keys != null)
                {
                    foreach (var key in secrets.Data.Keys)
                    {
                        // If the key ends with /, it's a directory
                        if (key.EndsWith("/"))
                        {
                            var subPath = $"{path}/{key.TrimEnd('/')}";
                            var subKeys = await ListSecretsAsync(subPath);
                            foreach (var subKey in subKeys)
                            {
                                result.Add(subKey);
                            }
                        }
                        else
                        {
                            // Get the secret to list its fields
                            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                                $"{path}/{key}",
                                mountPoint: _options.SecretsEnginePath);
                            
                            if (secret?.Data?.Data != null)
                            {
                                foreach (var field in secret.Data.Data.Keys)
                                {
                                    result.Add($"{path}/{key}:{field}");
                                }
                            }
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing secrets in path: {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Creates a Vault client
        /// </summary>
        private IVaultClient CreateVaultClient()
        {
            try
            {
                // Create authentication method
                IAuthMethodInfo authMethod;
                
                if (!string.IsNullOrEmpty(_options.Token))
                {
                    // Use token authentication
                    authMethod = new TokenAuthMethodInfo(_options.Token);
                }
                else if (!string.IsNullOrEmpty(_options.RoleId) && !string.IsNullOrEmpty(_options.SecretId))
                {
                    // Use AppRole authentication
                    authMethod = new AppRoleAuthMethodInfo(_options.RoleId, _options.SecretId);
                }
                else
                {
                    throw new InvalidOperationException("No valid authentication method configured for Vault");
                }
                
                // Create Vault client settings
                var vaultClientSettings = new VaultClientSettings(
                    _options.ServerUrl,
                    authMethod)
                {
                    Namespace = _options.Namespace
                };
                
                // Create Vault client
                return new VaultClient(vaultClientSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Vault client");
                throw;
            }
        }

        /// <summary>
        /// Parses a key into path and field
        /// </summary>
        private (string Path, string Field) ParseKey(string key)
        {
            // Key format: path/to/secret:field
            var parts = key.Split(':');
            
            if (parts.Length == 1)
            {
                // If no field is specified, use "value" as the default field
                return (parts[0], "value");
            }
            else if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }
            else
            {
                throw new ArgumentException($"Invalid key format: {key}. Expected format: path/to/secret:field");
            }
        }
    }

    /// <summary>
    /// Interface for secret provider
    /// </summary>
    public interface ISecretProvider
    {
        Task<string> GetSecretAsync(string key);
        Task SetSecretAsync(string key, string value);
        Task DeleteSecretAsync(string key);
        Task<IEnumerable<string>> ListSecretsAsync(string path);
    }

    /// <summary>
    /// Options for Vault secret provider
    /// </summary>
    public class VaultOptions
    {
        /// <summary>
        /// Vault server URL
        /// </summary>
        public string ServerUrl { get; set; } = "http://localhost:8200";
        
        /// <summary>
        /// Vault token for authentication
        /// </summary>
        public string Token { get; set; }
        
        /// <summary>
        /// Vault role ID for AppRole authentication
        /// </summary>
        public string RoleId { get; set; }
        
        /// <summary>
        /// Vault secret ID for AppRole authentication
        /// </summary>
        public string SecretId { get; set; }
        
        /// <summary>
        /// Vault namespace (for Vault Enterprise)
        /// </summary>
        public string Namespace { get; set; }
        
        /// <summary>
        /// Secrets engine path
        /// </summary>
        public string SecretsEnginePath { get; set; } = "secret";
    }
}
