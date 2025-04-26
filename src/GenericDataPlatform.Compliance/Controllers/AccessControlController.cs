using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.AccessControl;
using GenericDataPlatform.Compliance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Compliance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccessControlController : ControllerBase
    {
        private readonly IAccessControlService _accessControlService;
        private readonly ILogger<AccessControlController> _logger;

        public AccessControlController(IAccessControlService accessControlService, ILogger<AccessControlController> logger)
        {
            _accessControlService = accessControlService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a user has permission to perform an action on a resource
        /// </summary>
        [HttpGet("check")]
        public async Task<IActionResult> CheckPermission([FromQuery] string userId, [FromQuery] string resourceId, [FromQuery] string action)
        {
            try
            {
                var hasPermission = await _accessControlService.HasPermissionAsync(userId, resourceId, action);
                return Ok(new { HasPermission = hasPermission });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for user {UserId} on resource {ResourceId} for action {Action}", 
                    userId, resourceId, action);
                return StatusCode(500, "Error checking permission");
            }
        }

        /// <summary>
        /// Grants a permission to a user
        /// </summary>
        [HttpPost("grant")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GrantPermission([FromBody] PermissionRequest request)
        {
            try
            {
                var grantedBy = User.Identity?.Name;
                var result = await _accessControlService.GrantPermissionAsync(request.UserId, request.ResourceId, request.Action, grantedBy);
                
                if (result)
                {
                    return Ok(new { Success = true, Message = "Permission granted successfully" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to grant permission" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission to user {UserId} on resource {ResourceId} for action {Action}", 
                    request.UserId, request.ResourceId, request.Action);
                return StatusCode(500, "Error granting permission");
            }
        }

        /// <summary>
        /// Revokes a permission from a user
        /// </summary>
        [HttpPost("revoke")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokePermission([FromBody] PermissionRequest request)
        {
            try
            {
                var revokedBy = User.Identity?.Name;
                var result = await _accessControlService.RevokePermissionAsync(request.UserId, request.ResourceId, request.Action, revokedBy);
                
                if (result)
                {
                    return Ok(new { Success = true, Message = "Permission revoked successfully" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to revoke permission" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission from user {UserId} on resource {ResourceId} for action {Action}", 
                    request.UserId, request.ResourceId, request.Action);
                return StatusCode(500, "Error revoking permission");
            }
        }

        /// <summary>
        /// Gets permissions for a user
        /// </summary>
        [HttpGet("user/{userId}/permissions")]
        public async Task<IActionResult> GetPermissionsForUser(string userId)
        {
            try
            {
                var permissions = await _accessControlService.GetPermissionsForUserAsync(userId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return StatusCode(500, "Error getting permissions");
            }
        }

        /// <summary>
        /// Gets permissions for a resource
        /// </summary>
        [HttpGet("resource/{resourceId}/permissions")]
        public async Task<IActionResult> GetPermissionsForResource(string resourceId)
        {
            try
            {
                var permissions = await _accessControlService.GetPermissionsForResourceAsync(resourceId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for resource {ResourceId}", resourceId);
                return StatusCode(500, "Error getting permissions");
            }
        }

        /// <summary>
        /// Adds a user to a role
        /// </summary>
        [HttpPost("role/add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUserToRole([FromBody] RoleRequest request)
        {
            try
            {
                var addedBy = User.Identity?.Name;
                var result = await _accessControlService.AddUserToRoleAsync(request.UserId, request.Role, addedBy);
                
                if (result)
                {
                    return Ok(new { Success = true, Message = "User added to role successfully" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to add user to role" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to role {Role}", request.UserId, request.Role);
                return StatusCode(500, "Error adding user to role");
            }
        }

        /// <summary>
        /// Removes a user from a role
        /// </summary>
        [HttpPost("role/remove")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveUserFromRole([FromBody] RoleRequest request)
        {
            try
            {
                var removedBy = User.Identity?.Name;
                var result = await _accessControlService.RemoveUserFromRoleAsync(request.UserId, request.Role, removedBy);
                
                if (result)
                {
                    return Ok(new { Success = true, Message = "User removed from role successfully" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to remove user from role" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from role {Role}", request.UserId, request.Role);
                return StatusCode(500, "Error removing user from role");
            }
        }

        /// <summary>
        /// Gets roles for a user
        /// </summary>
        [HttpGet("user/{userId}/roles")]
        public async Task<IActionResult> GetRolesForUser(string userId)
        {
            try
            {
                var roles = await _accessControlService.GetRolesForUserAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                return StatusCode(500, "Error getting roles");
            }
        }
    }

    /// <summary>
    /// Request model for permission operations
    /// </summary>
    public class PermissionRequest
    {
        public string UserId { get; set; }
        public string ResourceId { get; set; }
        public string Action { get; set; }
    }

    /// <summary>
    /// Request model for role operations
    /// </summary>
    public class RoleRequest
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }
}
