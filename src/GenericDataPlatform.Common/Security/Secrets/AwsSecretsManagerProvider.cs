using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Common.Security.Secrets
{
    /// <summary>
    /// Secret provider using AWS Secrets Manager
    /// </summary>
    public class AwsSecretsManagerProvider : ISecretProvider
    {
        private readonly AwsSecretsManagerOptions _options;
        private readonly ILogger<AwsSecretsManagerProvider> _logger;
        private readonly IAmazonSecretsManager _secretsManager;
        private readonly Dictionary<string, string> _secretCache = new Dictionary<string, string>();

        public AwsSecretsManagerProvider(IOptions<AwsSecretsManagerOptions> options, ILogger<AwsSecretsManagerProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _secretsManager = CreateSecretsManagerClient();
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
                
                // Parse the key to get the secret ID and field
                var (secretId, field) = ParseKey(key);
                
                // Get the secret from AWS Secrets Manager
                var request = new GetSecretValueRequest
                {
                    SecretId = secretId
                };
                
                var response = await _secretsManager.GetSecretValueAsync(request);
                
                if (response?.SecretString != null)
                {
                    string secretValue;
                    
                    // If a field is specified, try to parse the secret as JSON and get the field
                    if (!string.IsNullOrEmpty(field))
                    {
                        try
                        {
                            var secretJson = JsonDocument.Parse(response.SecretString);
                            if (secretJson.RootElement.TryGetProperty(field, out var fieldValue))
                            {
                                secretValue = fieldValue.ToString();
                            }
                            else
                            {
                                _logger.LogWarning("Field not found in secret: {Field}", field);
                                return null;
                            }
                        }
                        catch (JsonException)
                        {
                            // If the secret is not valid JSON, return the whole secret
                            secretValue = response.SecretString;
                        }
                    }
                    else
                    {
                        // If no field is specified, return the whole secret
                        secretValue = response.SecretString;
                    }
                    
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
                // Parse the key to get the secret ID and field
                var (secretId, field) = ParseKey(key);
                
                string secretValue;
                
                // If a field is specified, try to update just that field
                if (!string.IsNullOrEmpty(field))
                {
                    // Try to get the current secret
                    try
                    {
                        var getRequest = new GetSecretValueRequest
                        {
                            SecretId = secretId
                        };
                        
                        var getResponse = await _secretsManager.GetSecretValueAsync(getRequest);
                        
                        if (getResponse?.SecretString != null)
                        {
                            try
                            {
                                // Parse the current secret as JSON
                                var secretJson = JsonDocument.Parse(getResponse.SecretString);
                                var secretDict = new Dictionary<string, object>();
                                
                                // Copy all existing fields
                                foreach (var property in secretJson.RootElement.EnumerateObject())
                                {
                                    secretDict[property.Name] = property.Value.ToString();
                                }
                                
                                // Update the specified field
                                secretDict[field] = value;
                                
                                // Serialize back to JSON
                                secretValue = JsonSerializer.Serialize(secretDict);
                            }
                            catch (JsonException)
                            {
                                // If the current secret is not valid JSON, create a new JSON object
                                secretValue = JsonSerializer.Serialize(new Dictionary<string, string>
                                {
                                    [field] = value
                                });
                            }
                        }
                        else
                        {
                            // If the secret doesn't exist, create a new JSON object
                            secretValue = JsonSerializer.Serialize(new Dictionary<string, string>
                            {
                                [field] = value
                            });
                        }
                    }
                    catch (ResourceNotFoundException)
                    {
                        // If the secret doesn't exist, create a new JSON object
                        secretValue = JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            [field] = value
                        });
                    }
                }
                else
                {
                    // If no field is specified, use the value as is
                    secretValue = value;
                }
                
                // Set the secret in AWS Secrets Manager
                var request = new PutSecretValueRequest
                {
                    SecretId = secretId,
                    SecretString = secretValue
                };
                
                await _secretsManager.PutSecretValueAsync(request);
                
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
                // Parse the key to get the secret ID and field
                var (secretId, field) = ParseKey(key);
                
                // If a field is specified, try to update just that field
                if (!string.IsNullOrEmpty(field))
                {
                    // Try to get the current secret
                    try
                    {
                        var getRequest = new GetSecretValueRequest
                        {
                            SecretId = secretId
                        };
                        
                        var getResponse = await _secretsManager.GetSecretValueAsync(getRequest);
                        
                        if (getResponse?.SecretString != null)
                        {
                            try
                            {
                                // Parse the current secret as JSON
                                var secretJson = JsonDocument.Parse(getResponse.SecretString);
                                var secretDict = new Dictionary<string, object>();
                                
                                // Copy all existing fields except the one to delete
                                foreach (var property in secretJson.RootElement.EnumerateObject())
                                {
                                    if (property.Name != field)
                                    {
                                        secretDict[property.Name] = property.Value.ToString();
                                    }
                                }
                                
                                // If there are still other fields, update the secret
                                if (secretDict.Count > 0)
                                {
                                    var putRequest = new PutSecretValueRequest
                                    {
                                        SecretId = secretId,
                                        SecretString = JsonSerializer.Serialize(secretDict)
                                    };
                                    
                                    await _secretsManager.PutSecretValueAsync(putRequest);
                                }
                                else
                                {
                                    // If there are no more fields, delete the entire secret
                                    var deleteRequest = new DeleteSecretRequest
                                    {
                                        SecretId = secretId,
                                        RecoveryWindowInDays = 7 // Use recovery window for safety
                                    };
                                    
                                    await _secretsManager.DeleteSecretAsync(deleteRequest);
                                }
                            }
                            catch (JsonException)
                            {
                                // If the current secret is not valid JSON, delete the entire secret
                                var deleteRequest = new DeleteSecretRequest
                                {
                                    SecretId = secretId,
                                    RecoveryWindowInDays = 7 // Use recovery window for safety
                                };
                                
                                await _secretsManager.DeleteSecretAsync(deleteRequest);
                            }
                        }
                    }
                    catch (ResourceNotFoundException)
                    {
                        // If the secret doesn't exist, there's nothing to delete
                        return;
                    }
                }
                else
                {
                    // If no field is specified, delete the entire secret
                    var deleteRequest = new DeleteSecretRequest
                    {
                        SecretId = secretId,
                        RecoveryWindowInDays = 7 // Use recovery window for safety
                    };
                    
                    await _secretsManager.DeleteSecretAsync(deleteRequest);
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
                
                // AWS Secrets Manager doesn't support hierarchical paths like Vault
                // So we list all secrets and filter by prefix
                var request = new ListSecretsRequest
                {
                    MaxResults = 100
                };
                
                ListSecretsResponse response;
                do
                {
                    response = await _secretsManager.ListSecretsAsync(request);
                    
                    if (response.SecretList != null)
                    {
                        foreach (var secret in response.SecretList)
                        {
                            // If path is empty, include all secrets
                            // Otherwise, only include secrets that start with the path
                            if (string.IsNullOrEmpty(path) || secret.Name.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                            {
                                // Get the secret value to list its fields
                                try
                                {
                                    var getRequest = new GetSecretValueRequest
                                    {
                                        SecretId = secret.ARN
                                    };
                                    
                                    var getResponse = await _secretsManager.GetSecretValueAsync(getRequest);
                                    
                                    if (getResponse?.SecretString != null)
                                    {
                                        try
                                        {
                                            // Parse the secret as JSON
                                            var secretJson = JsonDocument.Parse(getResponse.SecretString);
                                            
                                            // If it's a JSON object, list each field as a separate secret
                                            if (secretJson.RootElement.ValueKind == JsonValueKind.Object)
                                            {
                                                foreach (var property in secretJson.RootElement.EnumerateObject())
                                                {
                                                    result.Add($"{secret.Name}:{property.Name}");
                                                }
                                            }
                                            else
                                            {
                                                // If it's not a JSON object, list the secret as is
                                                result.Add(secret.Name);
                                            }
                                        }
                                        catch (JsonException)
                                        {
                                            // If it's not valid JSON, list the secret as is
                                            result.Add(secret.Name);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error getting secret value: {Name}", secret.Name);
                                    result.Add(secret.Name);
                                }
                            }
                        }
                    }
                    
                    request.NextToken = response.NextToken;
                }
                while (!string.IsNullOrEmpty(response.NextToken));
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing secrets in path: {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Creates an AWS Secrets Manager client
        /// </summary>
        private IAmazonSecretsManager CreateSecretsManagerClient()
        {
            try
            {
                // Create client configuration
                var config = new AmazonSecretsManagerConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
                };
                
                // Create client with credentials if specified
                if (!string.IsNullOrEmpty(_options.AccessKey) && !string.IsNullOrEmpty(_options.SecretKey))
                {
                    return new AmazonSecretsManagerClient(_options.AccessKey, _options.SecretKey, config);
                }
                
                // Otherwise, use the default credential provider chain
                return new AmazonSecretsManagerClient(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AWS Secrets Manager client");
                throw;
            }
        }

        /// <summary>
        /// Parses a key into secret ID and field
        /// </summary>
        private (string SecretId, string Field) ParseKey(string key)
        {
            // Key format: secretId:field
            var parts = key.Split(':');
            
            if (parts.Length == 1)
            {
                // If no field is specified, return the whole secret
                return (parts[0], null);
            }
            else if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }
            else
            {
                throw new ArgumentException($"Invalid key format: {key}. Expected format: secretId:field");
            }
        }
    }

    /// <summary>
    /// Options for AWS Secrets Manager secret provider
    /// </summary>
    public class AwsSecretsManagerOptions
    {
        /// <summary>
        /// AWS region
        /// </summary>
        public string Region { get; set; } = "us-east-1";
        
        /// <summary>
        /// AWS access key
        /// </summary>
        public string AccessKey { get; set; }
        
        /// <summary>
        /// AWS secret key
        /// </summary>
        public string SecretKey { get; set; }
    }
}
