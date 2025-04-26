using System;
using System.Threading.Tasks;
using GenericDataPlatform.API.Models.Auth;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.API.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IJwtTokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find the user by username
                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                // Verify the password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                // Check if the user is active
                if (!user.IsActive)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Account is disabled"
                    };
                }

                // Generate JWT token
                var token = _tokenService.GenerateToken(user);
                var tokenExpiration = DateTime.UtcNow.AddMinutes(60); // This should match the JWT expiration

                // Update last login time
                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                return new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = user.Roles,
                    Token = token,
                    TokenExpiration = tokenExpiration,
                    IsSuccess = true,
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", request.Username);
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if username already exists
                var existingUserByUsername = await _userRepository.GetByUsernameAsync(request.Username);
                if (existingUserByUsername != null)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Username already exists"
                    };
                }

                // Check if email already exists
                var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUserByEmail != null)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Email already exists"
                    };
                }

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Roles = new System.Collections.Generic.List<string> { "User" }, // Default role
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Save the user
                var result = await _userRepository.CreateAsync(user);
                if (!result)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to create user"
                    };
                }

                // Generate JWT token
                var token = _tokenService.GenerateToken(user);
                var tokenExpiration = DateTime.UtcNow.AddMinutes(60); // This should match the JWT expiration

                return new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = user.Roles,
                    Token = token,
                    TokenExpiration = tokenExpiration,
                    IsSuccess = true,
                    Message = "Registration successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            return Task.FromResult(_tokenService.ValidateToken(token));
        }
    }
}
