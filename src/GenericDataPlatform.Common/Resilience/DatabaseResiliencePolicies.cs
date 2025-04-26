using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using Polly;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Common database resilience policies
    /// </summary>
    public static class DatabaseResiliencePolicies
    {
        /// <summary>
        /// Creates a retry policy for SQL Server database operations
        /// </summary>
        public static IAsyncPolicy GetSqlServerRetryPolicy(ILogger logger = null, int retryCount = 3)
        {
            return Policy
                .Handle<SqlException>(ex => IsTransientSqlException(ex))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning(exception, "Delaying for {delay}ms, then making retry {retry} for SQL Server operation.", 
                            timespan.TotalMilliseconds, retryAttempt);
                    });
        }
        
        /// <summary>
        /// Creates a retry policy for MySQL database operations
        /// </summary>
        public static IAsyncPolicy GetMySqlRetryPolicy(ILogger logger = null, int retryCount = 3)
        {
            return Policy
                .Handle<MySqlException>(ex => IsTransientMySqlException(ex))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning(exception, "Delaying for {delay}ms, then making retry {retry} for MySQL operation.", 
                            timespan.TotalMilliseconds, retryAttempt);
                    });
        }
        
        /// <summary>
        /// Creates a retry policy for PostgreSQL database operations
        /// </summary>
        public static IAsyncPolicy GetPostgreSqlRetryPolicy(ILogger logger = null, int retryCount = 3)
        {
            return Policy
                .Handle<NpgsqlException>(ex => IsTransientPostgreSqlException(ex))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning(exception, "Delaying for {delay}ms, then making retry {retry} for PostgreSQL operation.", 
                            timespan.TotalMilliseconds, retryAttempt);
                    });
        }
        
        /// <summary>
        /// Creates a circuit breaker policy for database operations
        /// </summary>
        public static IAsyncPolicy GetCircuitBreakerPolicy(ILogger logger = null, int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return Policy
                .Handle<SqlException>(ex => IsTransientSqlException(ex))
                .Or<MySqlException>(ex => IsTransientMySqlException(ex))
                .Or<NpgsqlException>(ex => IsTransientPostgreSqlException(ex))
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (exception, timespan, context) =>
                    {
                        logger?.LogWarning(exception, "Circuit breaker opened for {duration}s due to database error.", 
                            timespan.TotalSeconds);
                    },
                    onReset: context =>
                    {
                        logger?.LogInformation("Circuit breaker reset for database operations.");
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogInformation("Circuit breaker half-open for database operations.");
                    });
        }
        
        /// <summary>
        /// Creates a timeout policy for database operations
        /// </summary>
        public static IAsyncPolicy GetTimeoutPolicy(ILogger logger = null, int timeoutInSeconds = 30)
        {
            return Policy
                .TimeoutAsync(
                    TimeSpan.FromSeconds(timeoutInSeconds),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (context, timespan, task) =>
                    {
                        logger?.LogWarning("Database operation timed out after {timeout}s.", 
                            timespan.TotalSeconds);
                        return System.Threading.Tasks.Task.CompletedTask;
                    });
        }
        
        /// <summary>
        /// Creates a combined policy with retry, circuit breaker, and timeout for SQL Server
        /// </summary>
        public static IAsyncPolicy GetSqlServerCombinedPolicy(ILogger logger = null)
        {
            return Policy.WrapAsync(
                GetSqlServerRetryPolicy(logger),
                GetCircuitBreakerPolicy(logger),
                GetTimeoutPolicy(logger));
        }
        
        /// <summary>
        /// Creates a combined policy with retry, circuit breaker, and timeout for MySQL
        /// </summary>
        public static IAsyncPolicy GetMySqlCombinedPolicy(ILogger logger = null)
        {
            return Policy.WrapAsync(
                GetMySqlRetryPolicy(logger),
                GetCircuitBreakerPolicy(logger),
                GetTimeoutPolicy(logger));
        }
        
        /// <summary>
        /// Creates a combined policy with retry, circuit breaker, and timeout for PostgreSQL
        /// </summary>
        public static IAsyncPolicy GetPostgreSqlCombinedPolicy(ILogger logger = null)
        {
            return Policy.WrapAsync(
                GetPostgreSqlRetryPolicy(logger),
                GetCircuitBreakerPolicy(logger),
                GetTimeoutPolicy(logger));
        }
        
        /// <summary>
        /// Determines if a SQL Server exception is transient
        /// </summary>
        private static bool IsTransientSqlException(SqlException ex)
        {
            // These error codes are considered transient errors in SQL Server
            // See: https://docs.microsoft.com/en-us/azure/sql-database/sql-database-connectivity-issues
            int[] transientErrorNumbers = { 4060, 40197, 40501, 40613, 49918, 49919, 49920, 11001 };
            
            foreach (SqlError error in ex.Errors)
            {
                if (Array.IndexOf(transientErrorNumbers, error.Number) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Determines if a MySQL exception is transient
        /// </summary>
        private static bool IsTransientMySqlException(MySqlException ex)
        {
            // These error codes are considered transient errors in MySQL
            // See: https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html
            int[] transientErrorNumbers = { 1040, 1042, 1043, 1152, 1203, 1205, 1213, 1614, 2002, 2003, 2006, 2013 };
            
            return Array.IndexOf(transientErrorNumbers, ex.Number) >= 0;
        }
        
        /// <summary>
        /// Determines if a PostgreSQL exception is transient
        /// </summary>
        private static bool IsTransientPostgreSqlException(NpgsqlException ex)
        {
            // These error codes are considered transient errors in PostgreSQL
            // See: https://www.postgresql.org/docs/current/errcodes-appendix.html
            string[] transientErrorCodes = { 
                "08000", // Connection Exception
                "08003", // Connection Does Not Exist
                "08006", // Connection Failure
                "08001", // SQL Client Unable to Establish SQL Connection
                "08004", // SQL Server Rejected Establishment of SQL Connection
                "40001", // Serialization Failure
                "40P01", // Deadlock Detected
                "57P01", // Admin Shutdown
                "57P02", // Crash Shutdown
                "57P03"  // Cannot Connect Now
            };
            
            if (ex.SqlState != null)
            {
                return Array.IndexOf(transientErrorCodes, ex.SqlState) >= 0;
            }
            
            return false;
        }
    }
}
