using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace GenericDataPlatform.Gateway.Identity
{
    /// <summary>
    /// Application user model extending IdentityUser
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// First name of the user
        /// </summary>
        public string FirstName { get; set; }
        
        /// <summary>
        /// Last name of the user
        /// </summary>
        public string LastName { get; set; }
        
        /// <summary>
        /// Full name of the user
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";
        
        /// <summary>
        /// Date of birth
        /// </summary>
        public DateTime? DateOfBirth { get; set; }
        
        /// <summary>
        /// User's profile picture URL
        /// </summary>
        public string ProfilePictureUrl { get; set; }
        
        /// <summary>
        /// When the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the user was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// When the user last logged in
        /// </summary>
        public DateTime? LastLogin { get; set; }
        
        /// <summary>
        /// Whether the user has completed the onboarding process
        /// </summary>
        public bool IsOnboardingCompleted { get; set; }
        
        /// <summary>
        /// Whether the user has accepted the terms and conditions
        /// </summary>
        public bool HasAcceptedTerms { get; set; }
        
        /// <summary>
        /// When the user accepted the terms and conditions
        /// </summary>
        public DateTime? TermsAcceptedAt { get; set; }
        
        /// <summary>
        /// User preferences as a JSON string
        /// </summary>
        public string Preferences { get; set; }
        
        /// <summary>
        /// External provider that was used to create this account (if any)
        /// </summary>
        public string ExternalProvider { get; set; }
        
        /// <summary>
        /// External provider user ID
        /// </summary>
        public string ExternalProviderId { get; set; }
    }
}
