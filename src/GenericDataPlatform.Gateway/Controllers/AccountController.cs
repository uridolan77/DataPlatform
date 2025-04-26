using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using GenericDataPlatform.Gateway.Identity;
using GenericDataPlatform.Gateway.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IIdentityServerInteractionService interaction,
            IEventService events,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _events = events;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true, // For simplicity, auto-confirm email
                IsOnboardingCompleted = false,
                HasAcceptedTerms = model.AcceptTerms,
                TermsAcceptedAt = model.AcceptTerms ? DateTime.UtcNow : null
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created a new account", model.Email);

                // Add user to default role
                await _userManager.AddToRoleAsync(user, "User");

                // Add claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName),
                    new Claim("fullName", user.FullName),
                    new Claim("permission", "read_only")
                };

                await _userManager.AddClaimsAsync(user, claims);

                // Auto sign-in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                return Ok(new { message = "Registration successful" });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        /// <summary>
        /// Login
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { error = "Invalid email or password" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in", model.Email);

                // Update last login timestamp
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Record login event
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

                return Ok(new { message = "Login successful" });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", model.Email);
                return BadRequest(new { error = "Account is locked out. Please try again later." });
            }

            _logger.LogWarning("Invalid login attempt for user {Email}", model.Email);
            return BadRequest(new { error = "Invalid email or password" });
        }

        /// <summary>
        /// Logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                _logger.LogInformation("User {Email} logged out", user.Email);
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.Identity.Name, user.Id));
            }

            await _signInManager.SignOutAsync();

            return Ok(new { message = "Logout successful" });
        }

        /// <summary>
        /// Get current user
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var userInfo = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.ProfilePictureUrl,
                user.PhoneNumber,
                user.IsOnboardingCompleted,
                Roles = roles,
                Claims = claims.Select(c => new { c.Type, c.Value })
            };

            return Ok(userInfo);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} updated their profile", user.Email);
                return Ok(new { message = "Profile updated successfully" });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} changed their password", user.Email);
                return Ok(new { message = "Password changed successfully" });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        /// <summary>
        /// External login callback
        /// </summary>
        [HttpGet("external-login-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return BadRequest(new { error = "Error loading external login information" });
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {LoginProvider} provider", info.LoginProvider);
                return Redirect(returnUrl ?? "~/");
            }

            if (result.IsLockedOut)
            {
                return BadRequest(new { error = "Account is locked out. Please try again later." });
            }

            // If the user does not have an account, create one
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "",
                    LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "",
                    EmailConfirmed = true,
                    IsOnboardingCompleted = false,
                    HasAcceptedTerms = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    ExternalProvider = info.LoginProvider,
                    ExternalProviderId = info.ProviderKey
                };

                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    _logger.LogInformation("User {Email} created an account using {LoginProvider} provider", email, info.LoginProvider);

                    // Add user to default role
                    await _userManager.AddToRoleAsync(user, "User");

                    // Add claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.GivenName, user.FirstName),
                        new Claim(ClaimTypes.Surname, user.LastName),
                        new Claim("fullName", user.FullName),
                        new Claim("permission", "read_only")
                    };

                    await _userManager.AddClaimsAsync(user, claims);

                    // Add the external login
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return Redirect(returnUrl ?? "~/");
                    }
                }

                var errors = createResult.Errors.Select(e => e.Description);
                return BadRequest(new { errors });
            }

            // Add the external login to the existing user
            var addExternalLoginResult = await _userManager.AddLoginAsync(user, info);
            if (addExternalLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Redirect(returnUrl ?? "~/");
            }

            var addLoginErrors = addExternalLoginResult.Errors.Select(e => e.Description);
            return BadRequest(new { errors = addLoginErrors });
        }
    }
}
