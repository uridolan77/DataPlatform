using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Gateway.Identity
{
    /// <summary>
    /// Database initializer for Identity and IdentityServer
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Initialize the database with seed data
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            try
            {
                // Apply migrations
                await ApplyMigrationsAsync(serviceProvider);
                
                // Seed identity data
                await SeedIdentityDataAsync(serviceProvider);
                
                // Seed IdentityServer data
                await SeedIdentityServerDataAsync(serviceProvider);
                
                logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }
        
        /// <summary>
        /// Apply database migrations
        /// </summary>
        private static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            try
            {
                // Apply Identity migrations
                var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await identityContext.Database.MigrateAsync();
                logger.LogInformation("Applied Identity migrations");
                
                // Apply IdentityServer configuration migrations
                var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                await configContext.Database.MigrateAsync();
                logger.LogInformation("Applied IdentityServer configuration migrations");
                
                // Apply IdentityServer operational migrations
                var operationalContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
                await operationalContext.Database.MigrateAsync();
                logger.LogInformation("Applied IdentityServer operational migrations");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying migrations");
                throw;
            }
        }
        
        /// <summary>
        /// Seed identity data
        /// </summary>
        private static async Task SeedIdentityDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            try
            {
                // Create roles
                var roles = new[] { "Admin", "User", "DataEngineer", "DataScientist", "Compliance" };
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                }
                
                // Create admin user
                var adminUser = await userManager.FindByNameAsync("admin");
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = "admin@example.com",
                        EmailConfirmed = true,
                        FirstName = "Admin",
                        LastName = "User",
                        IsOnboardingCompleted = true,
                        HasAcceptedTerms = true,
                        TermsAcceptedAt = DateTime.UtcNow
                    };
                    
                    var result = await userManager.CreateAsync(adminUser, "Admin123!");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created admin user");
                        
                        // Add admin to roles
                        await userManager.AddToRolesAsync(adminUser, roles);
                        logger.LogInformation("Added admin user to roles");
                        
                        // Add claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, adminUser.UserName),
                            new Claim(ClaimTypes.Email, adminUser.Email),
                            new Claim(ClaimTypes.GivenName, adminUser.FirstName),
                            new Claim(ClaimTypes.Surname, adminUser.LastName),
                            new Claim("fullName", adminUser.FullName),
                            new Claim("permission", "full_access")
                        };
                        
                        await userManager.AddClaimsAsync(adminUser, claims);
                        logger.LogInformation("Added claims to admin user");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create admin user: {Errors}", errors);
                    }
                }
                
                // Create test user
                var testUser = await userManager.FindByNameAsync("user");
                if (testUser == null)
                {
                    testUser = new ApplicationUser
                    {
                        UserName = "user",
                        Email = "user@example.com",
                        EmailConfirmed = true,
                        FirstName = "Test",
                        LastName = "User",
                        IsOnboardingCompleted = true,
                        HasAcceptedTerms = true,
                        TermsAcceptedAt = DateTime.UtcNow
                    };
                    
                    var result = await userManager.CreateAsync(testUser, "User123!");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created test user");
                        
                        // Add user to roles
                        await userManager.AddToRoleAsync(testUser, "User");
                        logger.LogInformation("Added test user to User role");
                        
                        // Add claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, testUser.UserName),
                            new Claim(ClaimTypes.Email, testUser.Email),
                            new Claim(ClaimTypes.GivenName, testUser.FirstName),
                            new Claim(ClaimTypes.Surname, testUser.LastName),
                            new Claim("fullName", testUser.FullName),
                            new Claim("permission", "read_only")
                        };
                        
                        await userManager.AddClaimsAsync(testUser, claims);
                        logger.LogInformation("Added claims to test user");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create test user: {Errors}", errors);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding identity data");
                throw;
            }
        }
        
        /// <summary>
        /// Seed IdentityServer data
        /// </summary>
        private static async Task SeedIdentityServerDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            
            try
            {
                // Seed clients if they don't exist
                if (!await context.Clients.AnyAsync())
                {
                    foreach (var client in GetClients())
                    {
                        await context.Clients.AddAsync(client.ToEntity());
                    }
                    
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded IdentityServer clients");
                }
                
                // Seed identity resources if they don't exist
                if (!await context.IdentityResources.AnyAsync())
                {
                    foreach (var resource in GetIdentityResources())
                    {
                        await context.IdentityResources.AddAsync(resource.ToEntity());
                    }
                    
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded IdentityServer identity resources");
                }
                
                // Seed API scopes if they don't exist
                if (!await context.ApiScopes.AnyAsync())
                {
                    foreach (var scope in GetApiScopes())
                    {
                        await context.ApiScopes.AddAsync(scope.ToEntity());
                    }
                    
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded IdentityServer API scopes");
                }
                
                // Seed API resources if they don't exist
                if (!await context.ApiResources.AnyAsync())
                {
                    foreach (var resource in GetApiResources())
                    {
                        await context.ApiResources.AddAsync(resource.ToEntity());
                    }
                    
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded IdentityServer API resources");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding IdentityServer data");
                throw;
            }
        }
        
        /// <summary>
        /// Get clients for IdentityServer
        /// </summary>
        private static IEnumerable<Client> GetClients()
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
        
        /// <summary>
        /// Get identity resources for IdentityServer
        /// </summary>
        private static IEnumerable<IdentityResource> GetIdentityResources()
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
        /// Get API scopes for IdentityServer
        /// </summary>
        private static IEnumerable<ApiScope> GetApiScopes()
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
        /// Get API resources for IdentityServer
        /// </summary>
        private static IEnumerable<ApiResource> GetApiResources()
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
    }
}
