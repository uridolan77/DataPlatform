namespace GenericDataPlatform.Compliance.Models
{
    /// <summary>
    /// Options for the compliance service
    /// </summary>
    public class ComplianceOptions
    {
        /// <summary>
        /// Database connection string
        /// </summary>
        public string DatabaseConnectionString { get; set; }
        
        /// <summary>
        /// Whether to enable audit logging
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;
        
        /// <summary>
        /// Whether to enable PII detection
        /// </summary>
        public bool EnablePIIDetection { get; set; } = true;
        
        /// <summary>
        /// Whether to enable access control
        /// </summary>
        public bool EnableAccessControl { get; set; } = true;
        
        /// <summary>
        /// Whether to enable data encryption
        /// </summary>
        public bool EnableDataEncryption { get; set; } = true;
        
        /// <summary>
        /// Path to the encryption key file
        /// </summary>
        public string EncryptionKeyPath { get; set; }
        
        /// <summary>
        /// Vault server URL for secret management
        /// </summary>
        public string VaultServerUrl { get; set; }
        
        /// <summary>
        /// Vault authentication token
        /// </summary>
        public string VaultToken { get; set; }
        
        /// <summary>
        /// Vault role ID for AppRole authentication
        /// </summary>
        public string VaultRoleId { get; set; }
        
        /// <summary>
        /// Vault secret ID for AppRole authentication
        /// </summary>
        public string VaultSecretId { get; set; }
    }
}
