using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Compliance.Models
{
    /// <summary>
    /// Represents a permission in the system
    /// </summary>
    public class Permission
    {
        /// <summary>
        /// Unique identifier for the permission
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// ID of the user who has the permission (null for role-based permissions)
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// ID of the role that has the permission (null for user-based permissions)
        /// </summary>
        public string RoleId { get; set; }
        
        /// <summary>
        /// ID of the resource (null for resource type permissions)
        /// </summary>
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Type of the resource
        /// </summary>
        public string ResourceType { get; set; }
        
        /// <summary>
        /// Action that is permitted (e.g., Read, Write, Delete, *)
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// ID of the user who granted the permission
        /// </summary>
        public string GrantedBy { get; set; }
        
        /// <summary>
        /// Timestamp when the permission was granted
        /// </summary>
        public DateTime GrantedAt { get; set; }
        
        /// <summary>
        /// Timestamp when the permission expires (null for no expiration)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
    
    /// <summary>
    /// Represents a resource in the system
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// Unique identifier for the resource
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Type of the resource
        /// </summary>
        public string ResourceType { get; set; }
        
        /// <summary>
        /// Owner of the resource
        /// </summary>
        public string OwnerId { get; set; }
        
        /// <summary>
        /// Attributes of the resource
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Timestamp when the resource was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Timestamp when the resource was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
    
    /// <summary>
    /// Represents a policy for attribute-based access control
    /// </summary>
    public class Policy
    {
        /// <summary>
        /// Unique identifier for the policy
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the policy
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the policy
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Type of the resource this policy applies to
        /// </summary>
        public string ResourceType { get; set; }
        
        /// <summary>
        /// Action this policy applies to
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Effect of the policy (Allow or Deny)
        /// </summary>
        public string Effect { get; set; }
        
        /// <summary>
        /// Conditions that must be met for the policy to apply
        /// </summary>
        public List<PolicyCondition> Conditions { get; set; } = new List<PolicyCondition>();
        
        /// <summary>
        /// Priority of the policy (higher priority policies are evaluated first)
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Timestamp when the policy was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Timestamp when the policy was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
    
    /// <summary>
    /// Represents a condition in a policy
    /// </summary>
    public class PolicyCondition
    {
        /// <summary>
        /// Type of the condition (e.g., ClaimEquals, ResourceAttributeEquals, TimeOfDay)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Attribute to check
        /// </summary>
        public string Attribute { get; set; }
        
        /// <summary>
        /// Value to compare against
        /// </summary>
        public string Value { get; set; }
    }
    
    /// <summary>
    /// Common permission actions
    /// </summary>
    public static class PermissionActions
    {
        public const string Read = "Read";
        public const string Write = "Write";
        public const string Delete = "Delete";
        public const string Execute = "Execute";
        public const string All = "*";
    }
}
