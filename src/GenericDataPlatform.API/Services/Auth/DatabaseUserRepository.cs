using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.API.Models.Auth;
using GenericDataPlatform.Common.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace GenericDataPlatform.API.Services.Auth
{
    /// <summary>
    /// SQL Server implementation of the user repository
    /// </summary>
    public class DatabaseUserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseUserRepository> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly JsonSerializerOptions _jsonOptions;

        public DatabaseUserRepository(
            IOptions<ApiOptions> options,
            ILogger<DatabaseUserRepository> logger,
            IAsyncPolicy resiliencePolicy)
        {
            _connectionString = options.Value.ConnectionStrings?.SqlServer 
                ?? throw new ArgumentNullException(nameof(options.Value.ConnectionStrings.SqlServer), 
                    "SQL Server connection string is required for DatabaseUserRepository");
            _logger = logger;
            _resiliencePolicy = resiliencePolicy;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Ensure database tables exist
            EnsureTablesExistAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureTablesExistAsync()
        {
            try
            {
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Check if Users table exists
                    var tableExists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'");

                    if (tableExists == 0)
                    {
                        _logger.LogInformation("Creating users table");
                        
                        // Read SQL script from embedded resource
                        var assembly = typeof(DatabaseUserRepository).Assembly;
                        var resourceName = "GenericDataPlatform.API.Database.Scripts.CreateUserTable.sql";
                        
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
                        }
                        
                        using var reader = new System.IO.StreamReader(stream);
                        var sql = await reader.ReadToEndAsync();
                        
                        // Execute script
                        await connection.ExecuteAsync(sql);
                        
                        _logger.LogInformation("Users table created successfully");
                        
                        // Add default users
                        await SeedDefaultUsersAsync(connection);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring users table exists");
                throw;
            }
        }

        private async Task SeedDefaultUsersAsync(IDbConnection connection)
        {
            try
            {
                _logger.LogInformation("Seeding default users");
                
                // Create admin user
                var adminUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FirstName = "Admin",
                    LastName = "User",
                    Roles = new List<string> { "Admin" },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                // Create regular user
                var regularUser = new User
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
                };
                
                // Insert users
                var sql = @"
                    INSERT INTO Users (Id, Username, Email, PasswordHash, FirstName, LastName, Roles, IsActive, CreatedAt)
                    VALUES (@Id, @Username, @Email, @PasswordHash, @FirstName, @LastName, @Roles, @IsActive, @CreatedAt)";
                
                await connection.ExecuteAsync(sql, new[]
                {
                    new
                    {
                        adminUser.Id,
                        adminUser.Username,
                        adminUser.Email,
                        adminUser.PasswordHash,
                        adminUser.FirstName,
                        adminUser.LastName,
                        Roles = JsonSerializer.Serialize(adminUser.Roles, _jsonOptions),
                        IsActive = adminUser.IsActive ? 1 : 0,
                        adminUser.CreatedAt
                    },
                    new
                    {
                        regularUser.Id,
                        regularUser.Username,
                        regularUser.Email,
                        regularUser.PasswordHash,
                        regularUser.FirstName,
                        regularUser.LastName,
                        Roles = JsonSerializer.Serialize(regularUser.Roles, _jsonOptions),
                        IsActive = regularUser.IsActive ? 1 : 0,
                        regularUser.CreatedAt
                    }
                });
                
                _logger.LogInformation("Default users seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default users");
                throw;
            }
        }

        public async Task<User> GetByIdAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM Users WHERE Id = @Id";
                    var user = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
                    
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found", id);
                        return null;
                    }
                    
                    return MapToUser(user);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return null;
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM Users WHERE Username = @Username";
                    var user = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Username = username });
                    
                    if (user == null)
                    {
                        _logger.LogWarning("User with username {Username} not found", username);
                        return null;
                    }
                    
                    return MapToUser(user);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username {Username}", username);
                return null;
            }
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM Users WHERE Email = @Email";
                    var user = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Email = email });
                    
                    if (user == null)
                    {
                        _logger.LogWarning("User with email {Email} not found", email);
                        return null;
                    }
                    
                    return MapToUser(user);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                return null;
            }
        }

        public async Task<bool> CreateAsync(User user)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    if (string.IsNullOrEmpty(user.Id))
                    {
                        user.Id = Guid.NewGuid().ToString();
                    }
                    
                    // Set created timestamp
                    user.CreatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO Users (Id, Username, Email, PasswordHash, FirstName, LastName, Roles, IsActive, CreatedAt)
                        VALUES (@Id, @Username, @Email, @PasswordHash, @FirstName, @LastName, @Roles, @IsActive, @CreatedAt)";

                    await connection.ExecuteAsync(sql, new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.PasswordHash,
                        user.FirstName,
                        user.LastName,
                        Roles = JsonSerializer.Serialize(user.Roles, _jsonOptions),
                        IsActive = user.IsActive ? 1 : 0,
                        user.CreatedAt
                    });
                    
                    _logger.LogInformation("Created user {Username} with ID {UserId}", user.Username, user.Id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", user.Username);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(User user)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Set updated timestamp
                    user.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        UPDATE Users
                        SET Username = @Username,
                            Email = @Email,
                            PasswordHash = @PasswordHash,
                            FirstName = @FirstName,
                            LastName = @LastName,
                            Roles = @Roles,
                            IsActive = @IsActive,
                            UpdatedAt = @UpdatedAt
                        WHERE Id = @Id";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.PasswordHash,
                        user.FirstName,
                        user.LastName,
                        Roles = JsonSerializer.Serialize(user.Roles, _jsonOptions),
                        IsActive = user.IsActive ? 1 : 0,
                        user.UpdatedAt
                    });
                    
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("User {UserId} not found for update", user.Id);
                        return false;
                    }
                    
                    _logger.LogInformation("Updated user {Username} with ID {UserId}", user.Username, user.Id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Username}", user.Username);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "DELETE FROM Users WHERE Id = @Id";
                    var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                    
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("User {UserId} not found for deletion", id);
                        return false;
                    }
                    
                    _logger.LogInformation("Deleted user {UserId}", id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM Users ORDER BY Username";
                    var users = await connection.QueryAsync<dynamic>(sql);
                    
                    return users.Select(MapToUser).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return Enumerable.Empty<User>();
            }
        }

        #region Helper Methods

        private User MapToUser(dynamic userRecord)
        {
            var user = new User
            {
                Id = userRecord.Id,
                Username = userRecord.Username,
                Email = userRecord.Email,
                PasswordHash = userRecord.PasswordHash,
                FirstName = userRecord.FirstName,
                LastName = userRecord.LastName,
                IsActive = Convert.ToBoolean(userRecord.IsActive),
                CreatedAt = userRecord.CreatedAt,
                UpdatedAt = userRecord.UpdatedAt
            };
            
            // Deserialize Roles
            if (!string.IsNullOrEmpty(userRecord.Roles))
            {
                user.Roles = JsonSerializer.Deserialize<List<string>>(userRecord.Roles, _jsonOptions);
            }
            
            return user;
        }

        #endregion
    }
}
