using System.ComponentModel.DataAnnotations;

namespace GenericDataPlatform.Gateway.Models
{
    /// <summary>
    /// Register request model
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }
        
        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
        
        [Required]
        public bool AcceptTerms { get; set; }
    }
    
    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        public bool RememberMe { get; set; }
    }
    
    /// <summary>
    /// Update profile request model
    /// </summary>
    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
        
        [Phone]
        public string PhoneNumber { get; set; }
    }
    
    /// <summary>
    /// Change password request model
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; }
        
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
    
    /// <summary>
    /// Forgot password request model
    /// </summary>
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
    
    /// <summary>
    /// Reset password request model
    /// </summary>
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Token { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }
        
        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
