using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Gateway.Identity
{
    /// <summary>
    /// Implementation of IResourceStore that uses Entity Framework
    /// </summary>
    public class ResourceStore : IResourceStore
    {
        private readonly ConfigurationDbContext _context;
        private readonly ILogger<ResourceStore> _logger;

        public ResourceStore(ConfigurationDbContext context, ILogger<ResourceStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all resources
        /// </summary>
        public async Task<Resources> GetAllResourcesAsync()
        {
            try
            {
                var identity = await _context.IdentityResources
                    .Include(x => x.UserClaims)
                    .Include(x => x.Properties)
                    .AsNoTracking()
                    .ToListAsync();

                var apis = await _context.ApiResources
                    .Include(x => x.UserClaims)
                    .Include(x => x.Scopes)
                    .Include(x => x.Properties)
                    .Include(x => x.Secrets)
                    .AsNoTracking()
                    .ToListAsync();

                var scopes = await _context.ApiScopes
                    .Include(x => x.UserClaims)
                    .Include(x => x.Properties)
                    .AsNoTracking()
                    .ToListAsync();

                var identityResources = identity.Select(x => x.ToModel()).ToList();
                var apiResources = apis.Select(x => x.ToModel()).ToList();
                var apiScopes = scopes.Select(x => x.ToModel()).ToList();

                return new Resources(identityResources, apiResources, apiScopes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all resources");
                throw;
            }
        }

        /// <summary>
        /// Finds API resources by name
        /// </summary>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            try
            {
                if (apiResourceNames == null || !apiResourceNames.Any())
                {
                    return Enumerable.Empty<ApiResource>();
                }

                var names = apiResourceNames.ToArray();

                var apis = await _context.ApiResources
                    .Include(x => x.UserClaims)
                    .Include(x => x.Scopes)
                    .Include(x => x.Properties)
                    .Include(x => x.Secrets)
                    .Where(x => names.Contains(x.Name))
                    .AsNoTracking()
                    .ToListAsync();

                return apis.Select(x => x.ToModel()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding API resources by name");
                throw;
            }
        }

        /// <summary>
        /// Finds API resources by scope
        /// </summary>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            try
            {
                if (scopeNames == null || !scopeNames.Any())
                {
                    return Enumerable.Empty<ApiResource>();
                }

                var names = scopeNames.ToArray();

                var apis = await _context.ApiResources
                    .Include(x => x.UserClaims)
                    .Include(x => x.Scopes)
                    .Include(x => x.Properties)
                    .Include(x => x.Secrets)
                    .Where(x => x.Scopes.Any(s => names.Contains(s.Scope)))
                    .AsNoTracking()
                    .ToListAsync();

                return apis.Select(x => x.ToModel()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding API resources by scope");
                throw;
            }
        }

        /// <summary>
        /// Finds API scopes by name
        /// </summary>
        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            try
            {
                if (scopeNames == null || !scopeNames.Any())
                {
                    return Enumerable.Empty<ApiScope>();
                }

                var names = scopeNames.ToArray();

                var scopes = await _context.ApiScopes
                    .Include(x => x.UserClaims)
                    .Include(x => x.Properties)
                    .Where(x => names.Contains(x.Name))
                    .AsNoTracking()
                    .ToListAsync();

                return scopes.Select(x => x.ToModel()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding API scopes by name");
                throw;
            }
        }

        /// <summary>
        /// Finds identity resources by scope
        /// </summary>
        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            try
            {
                if (scopeNames == null || !scopeNames.Any())
                {
                    return Enumerable.Empty<IdentityResource>();
                }

                var names = scopeNames.ToArray();

                var identity = await _context.IdentityResources
                    .Include(x => x.UserClaims)
                    .Include(x => x.Properties)
                    .Where(x => names.Contains(x.Name))
                    .AsNoTracking()
                    .ToListAsync();

                return identity.Select(x => x.ToModel()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding identity resources by scope");
                throw;
            }
        }
    }
}
