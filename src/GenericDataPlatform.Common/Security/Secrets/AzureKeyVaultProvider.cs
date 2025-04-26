using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Common.Security.Secrets
{
    /// <summary>
    /// Secret provider using Azure Key Vault
    /// </summary>
    public class AzureKeyVaultProvider : ISecretProvider
    {
        private readonly AzureKeyVaultOptions _options;
        private readonly ILogger<AzureKeyVaultProvider> _logger;
        private readonly SecretClient _secretClient;
        private readonly Dictionary<string, string> _secretCache = new Dictionary<string, string>();

        public AzureKeyVaultProvider(IOptions<AzureKeyVaultOptions> options, ILogger<AzureKeyVaultProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _secretClient = CreateSecretClient();
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
                
                // Azure Key Vault doesn't support hierarchical paths or fields like Vault
                // So we just use the key as is
                var secretName = SanitizeKeyForAzure(key);
                
                // Get the secret from Azure Key Vault
                var response = await _secretClient.GetSecretAsync(secretName);
                
                if (response?.Value != null)
                {
                    var secretValue = response.Value.Value;
                    
                    // Cache the secret
                    _secretCache[key] = secretValue;
                    
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
                // Azure Key Vault doesn't support hierarchical paths or fields like Vault
                // So we just use the key as is
                var secretName = SanitizeKeyForAzure(key);
                
                // Set the secret in Azure Key Vault
                await _secretClient.SetSecretAsync(secretName, value);
                
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
                // Azure Key Vault doesn't support hierarchical paths or fields like Vault
                // So we just use the key as is
                var secretName = SanitizeKeyForAzure(key);
                
                // Delete the secret from Azure Key Vault
                var operation = await _secretClient.StartDeleteSecretAsync(secretName);
                
                // Wait for the deletion to complete
                await operation.WaitForCompletionAsync();
                
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
                
                // Azure Key Vault doesn't support hierarchical paths like Vault
                // So we list all secrets and filter by prefix
                var secrets = _secretClient.GetPropertiesOfSecretsAsync();
                
                await foreach (var secret in secrets)
                {
                    // If path is empty, include all secrets
                    // Otherwise, only include secrets that start with the path
                    if (string.IsNullOrEmpty(path) || secret.Name.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(secret.Name);
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
        /// Creates an Azure Key Vault client
        /// </summary>
        private SecretClient CreateSecretClient()
        {
            try
            {
                // Validate options
                if (string.IsNullOrEmpty(_options.VaultUri))
                {
                    throw new InvalidOperationException("Azure Key Vault URI is required");
                }
                
                // Create credential
                var credential = GetCredential();
                
                // Create secret client
                return new SecretClient(new Uri(_options.VaultUri), credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Azure Key Vault client");
                throw;
            }
        }

        /// <summary>
        /// Gets the appropriate credential based on configuration
        /// </summary>
        private object GetCredential()
        {
            // Use managed identity if specified
            if (_options.UseManagedIdentity)
            {
                return new DefaultAzureCredential();
            }
            
            // Use client secret if specified
            if (!string.IsNullOrEmpty(_options.ClientId) && !string.IsNullOrEmpty(_options.ClientSecret) && !string.IsNullOrEmpty(_options.TenantId))
            {
                return new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
            }
            
            // Use default credential as fallback
            return new DefaultAzureCredential();
        }

        /// <summary>
        /// Sanitizes a key for use with Azure Key Vault
        /// </summary>
        private string SanitizeKeyForAzure(string key)
        {
            // Azure Key Vault secret names can only contain alphanumeric characters and dashes
            // Replace invalid characters with dashes
            return key.Replace('/', '-').Replace(':', '-').Replace('.', '-');
        }
    }

    /// <summary>
    /// Options for Azure Key Vault secret provider
    /// </summary>
    public class AzureKeyVaultOptions
    {
        /// <summary>
        /// Azure Key Vault URI
        /// </summary>
        public string VaultUri { get; set; }
        
        /// <summary>
        /// Whether to use managed identity for authentication
        /// </summary>
        public bool UseManagedIdentity { get; set; } = true;
        
        /// <summary>
        /// Azure AD client ID for authentication
        /// </summary>
        public string ClientId { get; set; }
        
        /// <summary>
        /// Azure AD client secret for authentication
        /// </summary>
        public string ClientSecret { get; set; }
        
        /// <summary>
        /// Azure AD tenant ID for authentication
        /// </summary>
        public string TenantId { get; set; }
    }
}
