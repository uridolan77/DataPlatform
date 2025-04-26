using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.API.Models.Auth;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.API.Services.Auth
{
    // This is a mock repository for demonstration purposes
    // In a real application, this would be replaced with a database repository
    public class MockUserRepository : IUserRepository
    {
        private readonly List<User> _users;
        private readonly ILogger<MockUserRepository> _logger;

        public MockUserRepository(ILogger<MockUserRepository> logger)
        {
            _logger = logger;
            
            // Initialize with some test users
            _users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "admin",
                    Email = "admin@example.com",
                    // In a real application, this would be a properly hashed password
                    // For demonstration, we're using a simple hash
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FirstName = "Admin",
                    LastName = "User",
                    Roles = new List<string> { "Admin" },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "user",
                    Email = "user@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                    FirstName = "Regular",
                    LastName = "User",
                    Roles = new List<string> { "User" },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        public Task<User> GetByIdAsync(string id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }

        public Task<User> GetByUsernameAsync(string username)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<User> GetByEmailAsync(string email)
        {
            var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<bool> CreateAsync(User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Id))
                {
                    user.Id = Guid.NewGuid().ToString();
                }
                
                user.CreatedAt = DateTime.UtcNow;
                _users.Add(user);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", user.Username);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateAsync(User user)
        {
            try
            {
                var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
                if (existingUser == null)
                    return Task.FromResult(false);

                // Remove the old user and add the updated one
                _users.Remove(existingUser);
                _users.Add(user);
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Username}", user.Username);
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteAsync(string id)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    return Task.FromResult(false);

                _users.Remove(user);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return Task.FromResult(false);
            }
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<User>>(_users);
        }
    }
}
