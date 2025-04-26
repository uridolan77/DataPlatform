using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.Auditing;
using GenericDataPlatform.Compliance.Models;
using GenericDataPlatform.Compliance.Repositories;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Compliance.AccessControl
{
    /// <summary>
    /// Service for managing access control
    /// </summary>
    public class AccessControlService : IAccessControlService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccessControlService> _logger;

        public AccessControlService(
            IPermissionRepository permissionRepository,
            IAuditService auditService,
            ILogger<AccessControlService> logger)
        {
            _permissionRepository = permissionRepository;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a user has permission to perform an action on a resource
        /// </summary>
        public async Task<bool> HasPermissionAsync(string userId, string resourceId, string action)
        {
            try
            {
                var permissions = await _permissionRepository.GetPermissionsForUserAsync(userId);
                
                // Check for direct resource permission
                var hasDirectPermission = permissions.Any(p => 
                    p.ResourceId == resourceId && 
                    (p.Action == action || p.Action == "*"));
                
                if (hasDirectPermission)
                {
                    return true;
                }
                
                // Check for resource type permission
                var resource = await _permissionRepository.GetResourceAsync(resourceId);
                if (resource != null)
                {
                    var hasTypePermission = permissions.Any(p => 
                        p.ResourceType == resource.ResourceType && 
                        (p.Action == action || p.Action == "*"));
                    
                    if (hasTypePermission)
                    {
                        return true;
                    }
                }
                
                // Check for role-based permissions
                var userRoles = await _permissionRepository.GetRolesForUserAsync(userId);
                foreach (var role in userRoles)
                {
                    var rolePermissions = await _permissionRepository.GetPermissionsForRoleAsync(role);
                    
                    // Check for direct resource permission in role
                    var hasRoleDirectPermission = rolePermissions.Any(p => 
                        p.ResourceId == resourceId && 
                        (p.Action == action || p.Action == "*"));
                    
                    if (hasRoleDirectPermission)
                    {
                        return true;
                    }
                    
                    // Check for resource type permission in role
                    if (resource != null)
                    {
                        var hasRoleTypePermission = rolePermissions.Any(p => 
                            p.ResourceType == resource.ResourceType && 
                            (p.Action == action || p.Action == "*"));
                        
                        if (hasRoleTypePermission)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for user {UserId} on resource {ResourceId} for action {Action}", 
                    userId, resourceId, action);
                
                // Default to deny on error
                return false;
            }
        }

        /// <summary>
        /// Checks if a user has permission and records an audit event
        /// </summary>
        public async Task<bool> CheckPermissionAsync(string userId, string resourceId, string action, string resourceType = null, Dictionary<string, object> details = null)
        {
            var hasPermission = await HasPermissionAsync(userId, resourceId, action);
            
            // Record audit event
            var auditEvent = new AuditEvent
            {
                EventType = hasPermission ? AuditEventTypes.DataAccess : AuditEventTypes.AccessDenied,
                UserId = userId,
                ResourceId = resourceId,
                ResourceType = resourceType,
                Action = action,
                Status = hasPermission ? AuditEventStatuses.Success : AuditEventStatuses.Denied,
                Timestamp = DateTime.UtcNow,
                Details = details ?? new Dictionary<string, object>()
            };
            
            await _auditService.RecordEventAsync(auditEvent);
            
            return hasPermission;
        }

        /// <summary>
        /// Grants a permission to a user
        /// </summary>
        public async Task<bool> GrantPermissionAsync(string userId, string resourceId, string action, string grantedBy)
        {
            try
            {
                var resource = await _permissionRepository.GetResourceAsync(resourceId);
                if (resource == null)
                {
                    _logger.LogWarning("Cannot grant permission for non-existent resource {ResourceId}", resourceId);
                    return false;
                }
                
                var permission = new Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ResourceId = resourceId,
                    ResourceType = resource.ResourceType,
                    Action = action,
                    GrantedBy = grantedBy,
                    GrantedAt = DateTime.UtcNow
                };
                
                await _permissionRepository.SavePermissionAsync(permission);
                
                // Record audit event
                var auditEvent = new AuditEvent
                {
                    EventType = AuditEventTypes.PermissionGranted,
                    UserId = grantedBy,
                    ResourceId = resourceId,
                    ResourceType = resource.ResourceType,
                    Action = "Grant",
                    Status = AuditEventStatuses.Success,
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["targetUserId"] = userId,
                        ["permissionAction"] = action
                    }
                };
                
                await _auditService.RecordEventAsync(auditEvent);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission to user {UserId} on resource {ResourceId} for action {Action}", 
                    userId, resourceId, action);
                
                return false;
            }
        }

        /// <summary>
        /// Revokes a permission from a user
        /// </summary>
        public async Task<bool> RevokePermissionAsync(string userId, string resourceId, string action, string revokedBy)
        {
            try
            {
                var resource = await _permissionRepository.GetResourceAsync(resourceId);
                if (resource == null)
                {
                    _logger.LogWarning("Cannot revoke permission for non-existent resource {ResourceId}", resourceId);
                    return false;
                }
                
                await _permissionRepository.DeletePermissionAsync(userId, resourceId, action);
                
                // Record audit event
                var auditEvent = new AuditEvent
                {
                    EventType = AuditEventTypes.PermissionRevoked,
                    UserId = revokedBy,
                    ResourceId = resourceId,
                    ResourceType = resource.ResourceType,
                    Action = "Revoke",
                    Status = AuditEventStatuses.Success,
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["targetUserId"] = userId,
                        ["permissionAction"] = action
                    }
                };
                
                await _auditService.RecordEventAsync(auditEvent);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission from user {UserId} on resource {ResourceId} for action {Action}", 
                    userId, resourceId, action);
                
                return false;
            }
        }

        /// <summary>
        /// Gets permissions for a user
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissionsForUserAsync(string userId)
        {
            try
            {
                return await _permissionRepository.GetPermissionsForUserAsync(userId);
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
                return await _permissionRepository.GetPermissionsForResourceAsync(resourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for resource {ResourceId}", resourceId);
                throw;
            }
        }

        /// <summary>
        /// Adds a user to a role
        /// </summary>
        public async Task<bool> AddUserToRoleAsync(string userId, string role, string addedBy)
        {
            try
            {
                await _permissionRepository.AddUserToRoleAsync(userId, role);
                
                // Record audit event
                var auditEvent = new AuditEvent
                {
                    EventType = "RoleAssigned",
                    UserId = addedBy,
                    ResourceId = userId,
                    ResourceType = "User",
                    Action = "AddRole",
                    Status = AuditEventStatuses.Success,
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["role"] = role
                    }
                };
                
                await _auditService.RecordEventAsync(auditEvent);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to role {Role}", userId, role);
                return false;
            }
        }

        /// <summary>
        /// Removes a user from a role
        /// </summary>
        public async Task<bool> RemoveUserFromRoleAsync(string userId, string role, string removedBy)
        {
            try
            {
                await _permissionRepository.RemoveUserFromRoleAsync(userId, role);
                
                // Record audit event
                var auditEvent = new AuditEvent
                {
                    EventType = "RoleRemoved",
                    UserId = removedBy,
                    ResourceId = userId,
                    ResourceType = "User",
                    Action = "RemoveRole",
                    Status = AuditEventStatuses.Success,
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["role"] = role
                    }
                };
                
                await _auditService.RecordEventAsync(auditEvent);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from role {Role}", userId, role);
                return false;
            }
        }

        /// <summary>
        /// Gets roles for a user
        /// </summary>
        public async Task<IEnumerable<string>> GetRolesForUserAsync(string userId)
        {
            try
            {
                return await _permissionRepository.GetRolesForUserAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Evaluates attribute-based access control policies
        /// </summary>
        public async Task<bool> EvaluatePolicyAsync(ClaimsPrincipal user, string resourceId, string action, Dictionary<string, object> resourceAttributes = null)
        {
            try
            {
                // Get user ID from claims
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return false;
                }
                
                // First check RBAC permissions
                var hasRbacPermission = await HasPermissionAsync(userId, resourceId, action);
                if (hasRbacPermission)
                {
                    return true;
                }
                
                // If no RBAC permission, check ABAC policies
                var resource = await _permissionRepository.GetResourceAsync(resourceId);
                if (resource == null)
                {
                    return false;
                }
                
                // Get policies for the resource type
                var policies = await _permissionRepository.GetPoliciesForResourceTypeAsync(resource.ResourceType);
                
                // Evaluate each policy
                foreach (var policy in policies)
                {
                    if (policy.Action != action && policy.Action != "*")
                    {
                        continue;
                    }
                    
                    var policyResult = EvaluatePolicy(policy, user, resourceAttributes);
                    if (policyResult)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating policy for user on resource {ResourceId} for action {Action}", 
                    resourceId, action);
                
                // Default to deny on error
                return false;
            }
        }

        /// <summary>
        /// Evaluates a single policy
        /// </summary>
        private bool EvaluatePolicy(Policy policy, ClaimsPrincipal user, Dictionary<string, object> resourceAttributes)
        {
            // Simple policy evaluation logic - in a real implementation, this would be more sophisticated
            foreach (var condition in policy.Conditions)
            {
                switch (condition.Type)
                {
                    case "ClaimEquals":
                        var claimValue = user.FindFirstValue(condition.Attribute);
                        if (claimValue != condition.Value)
                        {
                            return false;
                        }
                        break;
                        
                    case "ResourceAttributeEquals":
                        if (resourceAttributes == null || 
                            !resourceAttributes.TryGetValue(condition.Attribute, out var attributeValue) || 
                            attributeValue?.ToString() != condition.Value)
                        {
                            return false;
                        }
                        break;
                        
                    case "TimeOfDay":
                        var currentHour = DateTime.UtcNow.Hour;
                        var startHour = int.Parse(condition.Value.Split('-')[0]);
                        var endHour = int.Parse(condition.Value.Split('-')[1]);
                        if (currentHour < startHour || currentHour >= endHour)
                        {
                            return false;
                        }
                        break;
                }
            }
            
            return true;
        }
    }

    /// <summary>
    /// Interface for access control service
    /// </summary>
    public interface IAccessControlService
    {
        Task<bool> HasPermissionAsync(string userId, string resourceId, string action);
        Task<bool> CheckPermissionAsync(string userId, string resourceId, string action, string resourceType = null, Dictionary<string, object> details = null);
        Task<bool> GrantPermissionAsync(string userId, string resourceId, string action, string grantedBy);
        Task<bool> RevokePermissionAsync(string userId, string resourceId, string action, string revokedBy);
        Task<IEnumerable<Permission>> GetPermissionsForUserAsync(string userId);
        Task<IEnumerable<Permission>> GetPermissionsForResourceAsync(string resourceId);
        Task<bool> AddUserToRoleAsync(string userId, string role, string addedBy);
        Task<bool> RemoveUserFromRoleAsync(string userId, string role, string removedBy);
        Task<IEnumerable<string>> GetRolesForUserAsync(string userId);
        Task<bool> EvaluatePolicyAsync(ClaimsPrincipal user, string resourceId, string action, Dictionary<string, object> resourceAttributes = null);
    }
}
