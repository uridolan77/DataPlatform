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
    /// Implementation of IClientStore that uses Entity Framework
    /// </summary>
    public class ClientStore : IClientStore
    {
        private readonly ConfigurationDbContext _context;
        private readonly ILogger<ClientStore> _logger;

        public ClientStore(ConfigurationDbContext context, ILogger<ClientStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Finds a client by ID
        /// </summary>
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            try
            {
                var client = await _context.Clients
                    .Include(x => x.AllowedGrantTypes)
                    .Include(x => x.RedirectUris)
                    .Include(x => x.PostLogoutRedirectUris)
                    .Include(x => x.AllowedScopes)
                    .Include(x => x.ClientSecrets)
                    .Include(x => x.Claims)
                    .Include(x => x.IdentityProviderRestrictions)
                    .Include(x => x.AllowedCorsOrigins)
                    .Include(x => x.Properties)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ClientId == clientId);

                if (client == null)
                {
                    _logger.LogWarning("Client {ClientId} not found", clientId);
                    return null;
                }

                return client.ToModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding client {ClientId}", clientId);
                throw;
            }
        }
    }
}
