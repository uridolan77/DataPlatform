using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Compliance.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GenericDataPlatform.Compliance.Repositories
{
    /// <summary>
    /// Repository for storing and retrieving permissions
    /// </summary>
    public class PermissionRepository : IPermissionRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<PermissionRepository> _logger;

        public PermissionRepository(IOptions<ComplianceOptions> options, ILogger<PermissionRepository> logger)
        {
            _connectionString = options.Value.DatabaseConnectionString;
            _logger = logger;
        }

        /// <summary>
        /// Saves a permission to the database
        /// </summary>
        public async Task SavePermissionAsync(Permission permission)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO permissions (
                        id, user_id, role_id, resource_id, resource_type, 
                        action, granted_by, granted_at, expires_at
                    ) VALUES (
                        @Id, @UserId, @RoleId, @ResourceId, @ResourceType, 
                        @Action, @GrantedBy, @GrantedAt, @ExpiresAt
                    )
                    ON CONFLICT (id) DO UPDATE SET
                        user_id = @UserId,
                        role_id = @RoleId,
                        resource_id = @ResourceId,
                        resource_type = @ResourceType,
                        action = @Action,
                        granted_by = @GrantedBy,
                        granted_at = @GrantedAt,
                        expires_at = @ExpiresAt";

                await connection.ExecuteAsync(sql, permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving permission {PermissionId}", permission.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a permission from the database
        /// </summary>
        public async Task DeletePermissionAsync(string userId, string resourceId, string action)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    DELETE FROM permissions
                    WHERE user_id = @UserId
                    AND resource_id = @ResourceId
                    AND action = @Action";

                await connection.ExecuteAsync(sql, new { UserId = userId, ResourceId = resourceId, Action = action });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission for user {UserId} on resource {ResourceId} for action {Action}", 
                    userId, resourceId, action);
                throw;
            }
        }

        /// <summary>
        /// Gets permissions for a user
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissionsForUserAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM permissions
                    WHERE user_id = @UserId
                    AND (expires_at IS NULL OR expires_at > @Now)";

                return await connection.QueryAsync<Permission>(sql, new { UserId = userId, Now = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets permissions for a resource
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissionsForResourceAsync(string resourceId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM permissions
                    WHERE resource_id = @ResourceId
                    AND (expires_at IS NULL OR expires_at > @Now)";

                return await connection.QueryAsync<Permission>(sql, new { ResourceId = resourceId, Now = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for resource {ResourceId}", resourceId);
                throw;
            }
        }

        /// <summary>
        /// Gets permissions for a role
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(string roleId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM permissions
                    WHERE role_id = @RoleId
                    AND (expires_at IS NULL OR expires_at > @Now)";

                return await connection.QueryAsync<Permission>(sql, new { RoleId = roleId, Now = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
                throw;
            }
        }

        /// <summary>
        /// Gets a resource by ID
        /// </summary>
        public async Task<Resource> GetResourceAsync(string resourceId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM resources
                    WHERE id = @ResourceId";

                var resourceDto = await connection.QueryFirstOrDefaultAsync<ResourceDto>(sql, new { ResourceId = resourceId });
                
                if (resourceDto == null)
                {
                    return null;
                }
                
                return MapToResource(resourceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resource {ResourceId}", resourceId);
                throw;
            }
        }

        /// <summary>
        /// Adds a user to a role
        /// </summary>
        public async Task AddUserToRoleAsync(string userId, string role)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO user_roles (user_id, role_id)
                    VALUES (@UserId, @RoleId)
                    ON CONFLICT (user_id, role_id) DO NOTHING";

                await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to role {Role}", userId, role);
                throw;
            }
        }

        /// <summary>
        /// Removes a user from a role
        /// </summary>
        public async Task RemoveUserFromRoleAsync(string userId, string role)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    DELETE FROM user_roles
                    WHERE user_id = @UserId
                    AND role_id = @RoleId";

                await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from role {Role}", userId, role);
                throw;
            }
        }

        /// <summary>
        /// Gets roles for a user
        /// </summary>
        public async Task<IEnumerable<string>> GetRolesForUserAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT role_id FROM user_roles
                    WHERE user_id = @UserId";

                return await connection.QueryAsync<string>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets policies for a resource type
        /// </summary>
        public async Task<IEnumerable<Policy>> GetPoliciesForResourceTypeAsync(string resourceType)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * FROM policies
                    WHERE resource_type = @ResourceType
                    ORDER BY priority DESC";

                var policyDtos = await connection.QueryAsync<PolicyDto>(sql, new { ResourceType = resourceType });
                
                var policies = new List<Policy>();
                foreach (var dto in policyDtos)
                {
                    policies.Add(MapToPolicy(dto));
                }
                
                return policies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting policies for resource type {ResourceType}", resourceType);
                throw;
            }
        }

        /// <summary>
        /// Maps database DTOs to domain models
        /// </summary>
        private Resource MapToResource(ResourceDto dto)
        {
            var resource = new Resource
            {
                Id = dto.id,
                Name = dto.name,
                ResourceType = dto.resource_type,
                OwnerId = dto.owner_id,
                CreatedAt = dto.created_at,
                UpdatedAt = dto.updated_at
            };
            
            if (!string.IsNullOrEmpty(dto.attributes))
            {
                try
                {
                    resource.Attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.attributes);
                }
                catch
                {
                    resource.Attributes = new Dictionary<string, object>();
                }
            }
            
            return resource;
        }

        /// <summary>
        /// Maps database DTOs to domain models
        /// </summary>
        private Policy MapToPolicy(PolicyDto dto)
        {
            var policy = new Policy
            {
                Id = dto.id,
                Name = dto.name,
                Description = dto.description,
                ResourceType = dto.resource_type,
                Action = dto.action,
                Effect = dto.effect,
                Priority = dto.priority,
                CreatedAt = dto.created_at,
                UpdatedAt = dto.updated_at
            };
            
            if (!string.IsNullOrEmpty(dto.conditions))
            {
                try
                {
                    policy.Conditions = JsonSerializer.Deserialize<List<PolicyCondition>>(dto.conditions);
                }
                catch
                {
                    policy.Conditions = new List<PolicyCondition>();
                }
            }
            
            return policy;
        }

        /// <summary>
        /// DTO for mapping database records
        /// </summary>
        private class ResourceDto
        {
            public string id { get; set; }
            public string name { get; set; }
            public string resource_type { get; set; }
            public string owner_id { get; set; }
            public string attributes { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }

        /// <summary>
        /// DTO for mapping database records
        /// </summary>
        private class PolicyDto
        {
            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string resource_type { get; set; }
            public string action { get; set; }
            public string effect { get; set; }
            public string conditions { get; set; }
            public int priority { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }
    }

    /// <summary>
    /// Interface for permission repository
    /// </summary>
    public interface IPermissionRepository
    {
        Task SavePermissionAsync(Permission permission);
        Task DeletePermissionAsync(string userId, string resourceId, string action);
        Task<IEnumerable<Permission>> GetPermissionsForUserAsync(string userId);
        Task<IEnumerable<Permission>> GetPermissionsForResourceAsync(string resourceId);
        Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(string roleId);
        Task<Resource> GetResourceAsync(string resourceId);
        Task AddUserToRoleAsync(string userId, string role);
        Task RemoveUserFromRoleAsync(string userId, string role);
        Task<IEnumerable<string>> GetRolesForUserAsync(string userId);
        Task<IEnumerable<Policy>> GetPoliciesForResourceTypeAsync(string resourceType);
    }
}
