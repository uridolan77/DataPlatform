using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Resilience policies for SQL Server operations
    /// </summary>
    public static class SqlServerResiliencePolicies
    {
        /// <summary>
        /// Gets a retry policy for SQL Server operations
        /// </summary>
        public static AsyncRetryPolicy GetRetryAsyncPolicy(ILogger logger)
        {
            return Policy
                .Handle<SqlException>(ex => 
                    ex.Number == -2 ||    // Timeout
                    ex.Number == 4060 ||  // Cannot open database
                    ex.Number == 40613 || // Database unavailable
                    ex.Number == 40197 || // Error processing request
                    ex.Number == 40501 || // Service busy
                    ex.Number == 40549 || // Session terminated
                    ex.Number == 40550 || // Server busy
                    ex.Number == 49918 || // Not enough resources
                    ex.Number == 49919 || // Not enough resources
                    ex.Number == 49920    // Service busy
                )
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(
                            exception,
                            "SQL Server operation failed. Retrying in {RetryTimeSpan}. Retry attempt {RetryCount}",
                            timeSpan,
                            retryCount);
                    }
                );
        }

        /// <summary>
        /// Gets a timeout policy for SQL Server operations
        /// </summary>
        public static AsyncTimeoutPolicy GetTimeoutAsyncPolicy(ILogger logger)
        {
            return Policy
                .TimeoutAsync(
                    timeout: TimeSpan.FromSeconds(30),
                    timeoutStrategy: TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (context, timeSpan, task) =>
                    {
                        logger.LogWarning(
                            "SQL Server operation timed out after {TimeoutTimeSpan}",
                            timeSpan);
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                );
        }

        /// <summary>
        /// Gets a circuit breaker policy for SQL Server operations
        /// </summary>
        public static AsyncPolicy GetCircuitBreakerAsyncPolicy(ILogger logger)
        {
            return Policy
                .Handle<SqlException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (exception, timeSpan) =>
                    {
                        logger.LogWarning(
                            exception,
                            "SQL Server circuit breaker opened for {DurationOfBreak}",
                            timeSpan);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("SQL Server circuit breaker reset");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("SQL Server circuit breaker half-open");
                    }
                );
        }

        /// <summary>
        /// Gets a combined policy for SQL Server operations
        /// </summary>
        public static IAsyncPolicy GetCombinedAsyncPolicy(ILogger logger)
        {
            return Policy.WrapAsync(
                GetRetryAsyncPolicy(logger),
                GetCircuitBreakerAsyncPolicy(logger),
                GetTimeoutAsyncPolicy(logger)
            );
        }
    }
}
