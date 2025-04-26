using System.Collections.Generic;
using Duende.IdentityServer.Models;

namespace GenericDataPlatform.Gateway.Configuration
{
    /// <summary>
    /// Configuration for IdentityServer
    /// </summary>
    public static class IdentityServerConfig
    {
        /// <summary>
        /// Gets identity resources
        /// </summary>
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource
                {
                    Name = "roles",
                    DisplayName = "User roles",
                    UserClaims = { "role" }
                },
                new IdentityResource
                {
                    Name = "permissions",
                    DisplayName = "User permissions",
                    UserClaims = { "permission" }
                }
            };
        }

        /// <summary>
        /// Gets API scopes
        /// </summary>
        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new List<ApiScope>
            {
                new ApiScope("api", "Generic Data Platform API"),
                new ApiScope("ingestion", "Data Ingestion Service"),
                new ApiScope("storage", "Data Storage Service"),
                new ApiScope("database", "Database Service"),
                new ApiScope("etl", "ETL Service"),
                new ApiScope("compliance", "Compliance Service")
            };
        }

        /// <summary>
        /// Gets API resources
        /// </summary>
        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("api", "Generic Data Platform API")
                {
                    Scopes = { "api" },
                    UserClaims = { "role", "permission" }
                },
                new ApiResource("ingestion", "Data Ingestion Service")
                {
                    Scopes = { "ingestion" },
                    UserClaims = { "role", "permission" }
                },
                new ApiResource("storage", "Data Storage Service")
                {
                    Scopes = { "storage" },
                    UserClaims = { "role", "permission" }
                },
                new ApiResource("database", "Database Service")
                {
                    Scopes = { "database" },
                    UserClaims = { "role", "permission" }
                },
                new ApiResource("etl", "ETL Service")
                {
                    Scopes = { "etl" },
                    UserClaims = { "role", "permission" }
                },
                new ApiResource("compliance", "Compliance Service")
                {
                    Scopes = { "compliance" },
                    UserClaims = { "role", "permission" }
                }
            };
        }

        /// <summary>
        /// Gets clients
        /// </summary>
        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                // SPA client
                new Client
                {
                    ClientId = "spa-client",
                    ClientName = "SPA Client",
                    ClientUri = "http://localhost:3000",
                    
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,
                    
                    RedirectUris =
                    {
                        "http://localhost:3000/callback",
                        "http://localhost:3000/silent-renew.html"
                    },
                    
                    PostLogoutRedirectUris = { "http://localhost:3000" },
                    AllowedCorsOrigins = { "http://localhost:3000" },
                    
                    AllowOfflineAccess = true,
                    AllowedScopes = { "openid", "profile", "email", "api", "ingestion", "storage", "database", "etl", "compliance" },
                    
                    AccessTokenLifetime = 3600, // 1 hour
                    IdentityTokenLifetime = 3600, // 1 hour
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Absolute,
                    AbsoluteRefreshTokenLifetime = 2592000, // 30 days
                    SlidingRefreshTokenLifetime = 1296000, // 15 days
                    
                    RequireConsent = false
                },
                
                // API client
                new Client
                {
                    ClientId = "api-client",
                    ClientName = "API Client",
                    
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = { new Secret("api-client-secret".Sha256()) },
                    
                    AllowedScopes = { "api", "ingestion", "storage", "database", "etl", "compliance" },
                    
                    AccessTokenLifetime = 3600, // 1 hour
                    
                    RequireConsent = false
                },
                
                // Swagger client
                new Client
                {
                    ClientId = "swagger-client",
                    ClientName = "Swagger Client",
                    
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,
                    
                    RedirectUris = { "https://localhost:5000/swagger/oauth2-redirect.html" },
                    AllowedCorsOrigins = { "https://localhost:5000" },
                    
                    AllowedScopes = { "openid", "profile", "email", "api", "ingestion", "storage", "database", "etl", "compliance" },
                    
                    AccessTokenLifetime = 3600, // 1 hour
                    
                    RequireConsent = false
                }
            };
        }
    }
}
