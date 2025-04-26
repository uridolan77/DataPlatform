using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GenericDataPlatform.IngestionService.Checkpoints
{
    /// <summary>
    /// Redis-based implementation of checkpoint storage
    /// </summary>
    public class RedisCheckpointStorage : ICheckpointStorage
    {
        private readonly RedisCheckpointStorageOptions _options;
        private readonly ILogger<RedisCheckpointStorage> _logger;
        private readonly Lazy<ConnectionMultiplexer> _connectionMultiplexer;

        public RedisCheckpointStorage(
            IOptions<RedisCheckpointStorageOptions> options,
            ILogger<RedisCheckpointStorage> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Create a lazy connection to Redis
            _connectionMultiplexer = new Lazy<ConnectionMultiplexer>(() => 
            {
                try
                {
                    return ConnectionMultiplexer.Connect(_options.ConnectionString);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to Redis");
                    throw;
                }
            });
        }

        public async Task<string> GetValueAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                var fullKey = GetFullKey(key);
                
                var value = await db.StringGetAsync(fullKey);
                
                return value.HasValue ? value.ToString() : null;
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
                var db = GetDatabase();
                var fullKey = GetFullKey(key);
                
                // Set the value with optional expiration
                var expiry = _options.ExpirationTimeInSeconds > 0 
                    ? TimeSpan.FromSeconds(_options.ExpirationTimeInSeconds) 
                    : (TimeSpan?)null;
                
                await db.StringSetAsync(fullKey, value, expiry);
                
                _logger.LogDebug("Checkpoint saved to Redis for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving checkpoint for key {Key}", key);
                throw;
            }
        }

        private IDatabase GetDatabase()
        {
            return _connectionMultiplexer.Value.GetDatabase();
        }

        private string GetFullKey(string key)
        {
            return string.IsNullOrEmpty(_options.KeyPrefix) 
                ? key 
                : $"{_options.KeyPrefix}:{key}";
        }
    }

    /// <summary>
    /// Options for Redis-based checkpoint storage
    /// </summary>
    public class RedisCheckpointStorageOptions
    {
        /// <summary>
        /// Redis connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Optional prefix for Redis keys
        /// </summary>
        public string KeyPrefix { get; set; } = "checkpoint";

        /// <summary>
        /// Optional expiration time in seconds (0 = no expiration)
        /// </summary>
        public int ExpirationTimeInSeconds { get; set; } = 0;
    }
}
