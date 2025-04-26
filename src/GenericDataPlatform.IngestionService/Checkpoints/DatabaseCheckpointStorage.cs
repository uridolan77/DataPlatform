using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dapper;

namespace GenericDataPlatform.IngestionService.Checkpoints
{
    /// <summary>
    /// Database-based implementation of checkpoint storage
    /// </summary>
    public class DatabaseCheckpointStorage : ICheckpointStorage
    {
        private readonly DatabaseCheckpointStorageOptions _options;
        private readonly ILogger<DatabaseCheckpointStorage> _logger;
        private readonly DbProviderFactory _dbProviderFactory;

        public DatabaseCheckpointStorage(
            IOptions<DatabaseCheckpointStorageOptions> options, 
            ILogger<DatabaseCheckpointStorage> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get the provider factory
            _dbProviderFactory = DbProviderFactories.GetFactory(_options.ProviderName);
            
            // Ensure the checkpoint table exists
            EnsureTableExistsAsync().GetAwaiter().GetResult();
        }

        public async Task<string> GetValueAsync(string key)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var sql = $"SELECT Value FROM {_options.TableName} WHERE [Key] = @Key";
                var value = await connection.QueryFirstOrDefaultAsync<string>(sql, new { Key = key });

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checkpoint for key {Key}", key);
                return null;
            }
        }

        public async Task SetValueAsync(string key, string value)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Check if the key exists
                var existingSql = $"SELECT COUNT(1) FROM {_options.TableName} WHERE [Key] = @Key";
                var exists = await connection.ExecuteScalarAsync<int>(existingSql, new { Key = key }) > 0;

                if (exists)
                {
                    // Update existing record
                    var updateSql = $"UPDATE {_options.TableName} SET Value = @Value, UpdatedAt = @UpdatedAt WHERE [Key] = @Key";
                    await connection.ExecuteAsync(updateSql, new { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
                }
                else
                {
                    // Insert new record
                    var insertSql = $"INSERT INTO {_options.TableName} ([Key], Value, CreatedAt, UpdatedAt) VALUES (@Key, @Value, @CreatedAt, @UpdatedAt)";
                    await connection.ExecuteAsync(insertSql, new { Key = key, Value = value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }

                _logger.LogDebug("Checkpoint saved to database for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving checkpoint for key {Key}", key);
                throw;
            }
        }

        private DbConnection CreateConnection()
        {
            var connection = _dbProviderFactory.CreateConnection();
            connection.ConnectionString = _options.ConnectionString;
            return connection;
        }

        private async Task EnsureTableExistsAsync()
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Check if the table exists
                var tableExists = false;
                
                // SQL Server
                if (_options.ProviderName.Contains("SqlClient"))
                {
                    var sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
                    tableExists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = _options.TableName }) > 0;
                }
                // PostgreSQL
                else if (_options.ProviderName.Contains("Npgsql"))
                {
                    var sql = "SELECT COUNT(1) FROM information_schema.tables WHERE table_name = @TableName";
                    tableExists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = _options.TableName.ToLower() }) > 0;
                }
                // MySQL
                else if (_options.ProviderName.Contains("MySql"))
                {
                    var sql = "SELECT COUNT(1) FROM information_schema.tables WHERE table_name = @TableName";
                    tableExists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = _options.TableName }) > 0;
                }
                // SQLite
                else if (_options.ProviderName.Contains("Sqlite"))
                {
                    var sql = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@TableName";
                    tableExists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = _options.TableName }) > 0;
                }

                if (!tableExists)
                {
                    // Create the table
                    string createTableSql;
                    
                    // SQL Server
                    if (_options.ProviderName.Contains("SqlClient"))
                    {
                        createTableSql = $@"
                            CREATE TABLE {_options.TableName} (
                                [Key] NVARCHAR(255) NOT NULL PRIMARY KEY,
                                Value NVARCHAR(MAX) NOT NULL,
                                CreatedAt DATETIME2 NOT NULL,
                                UpdatedAt DATETIME2 NOT NULL
                            )";
                    }
                    // PostgreSQL
                    else if (_options.ProviderName.Contains("Npgsql"))
                    {
                        createTableSql = $@"
                            CREATE TABLE {_options.TableName} (
                                ""Key"" VARCHAR(255) NOT NULL PRIMARY KEY,
                                ""Value"" TEXT NOT NULL,
                                ""CreatedAt"" TIMESTAMP NOT NULL,
                                ""UpdatedAt"" TIMESTAMP NOT NULL
                            )";
                    }
                    // MySQL
                    else if (_options.ProviderName.Contains("MySql"))
                    {
                        createTableSql = $@"
                            CREATE TABLE {_options.TableName} (
                                `Key` VARCHAR(255) NOT NULL PRIMARY KEY,
                                `Value` TEXT NOT NULL,
                                `CreatedAt` DATETIME NOT NULL,
                                `UpdatedAt` DATETIME NOT NULL
                            )";
                    }
                    // SQLite
                    else if (_options.ProviderName.Contains("Sqlite"))
                    {
                        createTableSql = $@"
                            CREATE TABLE {_options.TableName} (
                                [Key] TEXT NOT NULL PRIMARY KEY,
                                Value TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NOT NULL
                            )";
                    }
                    else
                    {
                        throw new NotSupportedException($"Provider {_options.ProviderName} is not supported");
                    }

                    await connection.ExecuteAsync(createTableSql);
                    _logger.LogInformation("Created checkpoint table: {TableName}", _options.TableName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring checkpoint table exists");
                throw;
            }
        }
    }

    /// <summary>
    /// Options for database-based checkpoint storage
    /// </summary>
    public class DatabaseCheckpointStorageOptions
    {
        /// <summary>
        /// Database provider name
        /// </summary>
        public string ProviderName { get; set; } = "Microsoft.Data.SqlClient";

        /// <summary>
        /// Connection string to the database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Table name for storing checkpoints
        /// </summary>
        public string TableName { get; set; } = "Checkpoints";
    }
}
