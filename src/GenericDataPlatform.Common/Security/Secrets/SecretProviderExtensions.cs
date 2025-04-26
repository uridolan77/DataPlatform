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
                    // Configure Azure Key Vault options
                    services.Configure<AzureKeyVaultOptions>(configuration.GetSection("Secrets:Azure"));

                    // Register Azure Key Vault secret provider
                    services.AddSingleton<ISecretProvider, AzureKeyVaultProvider>();
                    break;

                case "aws":
                    // Configure AWS Secrets Manager options
                    services.Configure<AwsSecretsManagerOptions>(configuration.GetSection("Secrets:AWS"));

                    // Register AWS Secrets Manager secret provider
                    services.AddSingleton<ISecretProvider, AwsSecretsManagerProvider>();
                    break;

                case "file":
                    // Configure file-based options
                    services.Configure<FileSecretProviderOptions>(configuration.GetSection("Secrets:File"));

                    // Register file-based secret provider
                    services.AddSingleton<ISecretProvider, FileSecretProvider>();
                    break;

                default:
                    throw new ArgumentException($"Unsupported secret provider type: {providerType}");
            }

            return services;
        }
    }
}
