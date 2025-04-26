using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenericDataPlatform.Common.Security.Secrets
{
    /// <summary>
    /// Extensions for registering secret providers
    /// </summary>
    public static class SecretProviderExtensions
    {
        /// <summary>
        /// Adds a secret provider to the service collection
        /// </summary>
        public static IServiceCollection AddSecretProvider(this IServiceCollection services, IConfiguration configuration)
        {
            // Get the provider type from configuration
            var providerType = configuration["Secrets:Provider"]?.ToLowerInvariant() ?? "vault";
            
            switch (providerType)
            {
                case "vault":
                    // Configure Vault options
                    services.Configure<VaultOptions>(configuration.GetSection("Secrets:Vault"));
                    
                    // Register Vault secret provider
                    services.AddSingleton<ISecretProvider, VaultSecretProvider>();
                    break;
                
                case "azure":
                    // In a real implementation, we would add Azure Key Vault provider here
                    throw new NotImplementedException("Azure Key Vault provider is not implemented yet");
                
                case "aws":
                    // In a real implementation, we would add AWS Secrets Manager provider here
                    throw new NotImplementedException("AWS Secrets Manager provider is not implemented yet");
                
                case "file":
                    // In a real implementation, we would add file-based provider here
                    throw new NotImplementedException("File-based provider is not implemented yet");
                
                default:
                    throw new ArgumentException($"Unsupported secret provider type: {providerType}");
            }
            
            return services;
        }
    }
}
