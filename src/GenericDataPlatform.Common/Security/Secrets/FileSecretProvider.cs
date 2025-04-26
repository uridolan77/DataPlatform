using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Common.Security.Secrets
{
    /// <summary>
    /// Secret provider using file system
    /// </summary>
    public class FileSecretProvider : ISecretProvider
    {
        private readonly FileSecretProviderOptions _options;
        private readonly ILogger<FileSecretProvider> _logger;
        private readonly Dictionary<string, string> _secretCache = new Dictionary<string, string>();
        private readonly byte[] _encryptionKey;
        private readonly byte[] _encryptionIv;

        public FileSecretProvider(IOptions<FileSecretProviderOptions> options, ILogger<FileSecretProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            
            // Ensure the secrets directory exists
            Directory.CreateDirectory(_options.SecretsDirectory);
            
            // Initialize encryption key and IV
            if (_options.EncryptSecrets)
            {
                if (string.IsNullOrEmpty(_options.EncryptionKey))
                {
                    throw new InvalidOperationException("Encryption key is required when encryption is enabled");
                }
                
                // Derive key and IV from the encryption key
                using var deriveBytes = new Rfc2898DeriveBytes(
                    _options.EncryptionKey,
                    Encoding.UTF8.GetBytes("GenericDataPlatform"),
                    10000,
                    HashAlgorithmName.SHA256);
                
                _encryptionKey = deriveBytes.GetBytes(32); // 256 bits
                _encryptionIv = deriveBytes.GetBytes(16); // 128 bits
            }
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
                
                // Get the file path
                var filePath = GetFilePath(path);
                
                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Secret file not found: {FilePath}", filePath);
                    return null;
                }
                
                // Read the file content
                string fileContent = await File.ReadAllTextAsync(filePath);
                
                // Decrypt if necessary
                if (_options.EncryptSecrets)
                {
                    fileContent = Decrypt(fileContent);
                }
                
                // Parse the content as JSON
                try
                {
                    var secretJson = JsonDocument.Parse(fileContent);
                    
                    // If a field is specified, get that field
                    if (!string.IsNullOrEmpty(field))
                    {
                        if (secretJson.RootElement.TryGetProperty(field, out var fieldValue))
                        {
                            var secretValue = fieldValue.ToString();
                            
                            // Cache the secret
                            _secretCache[key] = secretValue;
                            
                            return secretValue;
                        }
                        else
                        {
                            _logger.LogWarning("Field not found in secret: {Field}", field);
                            return null;
                        }
                    }
                    else
                    {
                        // If no field is specified, return the whole JSON
                        var secretValue = fileContent;
                        
                        // Cache the secret
                        _secretCache[key] = secretValue;
                        
                        return secretValue;
                    }
                }
                catch (JsonException)
                {
                    // If the content is not valid JSON, return it as is
                    var secretValue = fileContent;
                    
                    // Cache the secret
                    _secretCache[key] = secretValue;
                    
                    return secretValue;
                }
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
                
                // Get the file path
                var filePath = GetFilePath(path);
                
                string fileContent;
                
                // If a field is specified, try to update just that field
                if (!string.IsNullOrEmpty(field))
                {
                    // Try to read the current file
                    if (File.Exists(filePath))
                    {
                        string currentContent = await File.ReadAllTextAsync(filePath);
                        
                        // Decrypt if necessary
                        if (_options.EncryptSecrets)
                        {
                            currentContent = Decrypt(currentContent);
                        }
                        
                        try
                        {
                            // Parse the current content as JSON
                            var secretJson = JsonDocument.Parse(currentContent);
                            var secretDict = new Dictionary<string, object>();
                            
                            // Copy all existing fields
                            foreach (var property in secretJson.RootElement.EnumerateObject())
                            {
                                secretDict[property.Name] = property.Value.ToString();
                            }
                            
                            // Update the specified field
                            secretDict[field] = value;
                            
                            // Serialize back to JSON
                            fileContent = JsonSerializer.Serialize(secretDict, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                        }
                        catch (JsonException)
                        {
                            // If the current content is not valid JSON, create a new JSON object
                            fileContent = JsonSerializer.Serialize(new Dictionary<string, string>
                            {
                                [field] = value
                            }, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                        }
                    }
                    else
                    {
                        // If the file doesn't exist, create a new JSON object
                        fileContent = JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            [field] = value
                        }, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                    }
                }
                else
                {
                    // If no field is specified, use the value as is
                    fileContent = value;
                }
                
                // Encrypt if necessary
                if (_options.EncryptSecrets)
                {
                    fileContent = Encrypt(fileContent);
                }
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                // Write the content to the file
                await File.WriteAllTextAsync(filePath, fileContent);
                
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
                
                // Get the file path
                var filePath = GetFilePath(path);
                
                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Secret file not found: {FilePath}", filePath);
                    return;
                }
                
                // If a field is specified, try to update just that field
                if (!string.IsNullOrEmpty(field))
                {
                    // Read the current file
                    string currentContent = await File.ReadAllTextAsync(filePath);
                    
                    // Decrypt if necessary
                    if (_options.EncryptSecrets)
                    {
                        currentContent = Decrypt(currentContent);
                    }
                    
                    try
                    {
                        // Parse the current content as JSON
                        var secretJson = JsonDocument.Parse(currentContent);
                        var secretDict = new Dictionary<string, object>();
                        
                        // Copy all existing fields except the one to delete
                        foreach (var property in secretJson.RootElement.EnumerateObject())
                        {
                            if (property.Name != field)
                            {
                                secretDict[property.Name] = property.Value.ToString();
                            }
                        }
                        
                        // If there are still other fields, update the file
                        if (secretDict.Count > 0)
                        {
                            // Serialize back to JSON
                            string fileContent = JsonSerializer.Serialize(secretDict, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                            
                            // Encrypt if necessary
                            if (_options.EncryptSecrets)
                            {
                                fileContent = Encrypt(fileContent);
                            }
                            
                            // Write the content to the file
                            await File.WriteAllTextAsync(filePath, fileContent);
                        }
                        else
                        {
                            // If there are no more fields, delete the file
                            File.Delete(filePath);
                        }
                    }
                    catch (JsonException)
                    {
                        // If the current content is not valid JSON, delete the file
                        File.Delete(filePath);
                    }
                }
                else
                {
                    // If no field is specified, delete the file
                    File.Delete(filePath);
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
                
                // Get the directory path
                var directoryPath = Path.Combine(_options.SecretsDirectory, path.Replace(':', Path.DirectorySeparatorChar));
                
                // Check if the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    return result;
                }
                
                // Get all files in the directory and subdirectories
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    // Get the relative path
                    var relativePath = file.Substring(_options.SecretsDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                    
                    // Replace directory separators with colons
                    var secretPath = relativePath.Replace(Path.DirectorySeparatorChar, ':');
                    
                    // Read the file content
                    string fileContent = await File.ReadAllTextAsync(file);
                    
                    // Decrypt if necessary
                    if (_options.EncryptSecrets)
                    {
                        fileContent = Decrypt(fileContent);
                    }
                    
                    try
                    {
                        // Parse the content as JSON
                        var secretJson = JsonDocument.Parse(fileContent);
                        
                        // If it's a JSON object, list each field as a separate secret
                        if (secretJson.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var property in secretJson.RootElement.EnumerateObject())
                            {
                                result.Add($"{secretPath}:{property.Name}");
                            }
                        }
                        else
                        {
                            // If it's not a JSON object, list the secret as is
                            result.Add(secretPath);
                        }
                    }
                    catch (JsonException)
                    {
                        // If it's not valid JSON, list the secret as is
                        result.Add(secretPath);
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
        /// Gets the file path for a secret
        /// </summary>
        private string GetFilePath(string path)
        {
            // Replace colons with directory separators
            var filePath = path.Replace(':', Path.DirectorySeparatorChar);
            
            // Combine with the secrets directory
            return Path.Combine(_options.SecretsDirectory, filePath);
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
                // If no field is specified, return the whole secret
                return (parts[0], null);
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

        /// <summary>
        /// Encrypts a string
        /// </summary>
        private string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = _encryptionIv;
            
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Decrypts a string
        /// </summary>
        private string Decrypt(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = _encryptionIv;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }
    }

    /// <summary>
    /// Options for file-based secret provider
    /// </summary>
    public class FileSecretProviderOptions
    {
        /// <summary>
        /// Directory where secrets are stored
        /// </summary>
        public string SecretsDirectory { get; set; } = "secrets";
        
        /// <summary>
        /// Whether to encrypt secrets
        /// </summary>
        public bool EncryptSecrets { get; set; } = true;
        
        /// <summary>
        /// Key used for encryption
        /// </summary>
        public string EncryptionKey { get; set; }
    }
}
