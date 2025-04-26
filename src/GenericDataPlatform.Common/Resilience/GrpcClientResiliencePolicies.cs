using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Polly;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Common gRPC client resilience policies
    /// </summary>
    public static class GrpcClientResiliencePolicies
    {
        // List of gRPC status codes that are considered transient and can be retried
        private static readonly HashSet<StatusCode> TransientStatusCodes = new HashSet<StatusCode>
        {
            StatusCode.DeadlineExceeded,      // Request deadline exceeded
            StatusCode.Unavailable,           // Service unavailable
            StatusCode.ResourceExhausted,     // Resource has been exhausted (e.g., rate limiting)
            StatusCode.Aborted,               // Operation was aborted
            StatusCode.Internal,              // Server internal errors that might be transient
            StatusCode.Unknown                // Unknown errors that might be transient
        };

        /// <summary>
        /// Determines if a gRPC exception is transient and can be retried
        /// </summary>
        public static bool IsTransientException(RpcException ex)
        {
            return TransientStatusCodes.Contains(ex.StatusCode);
        }

        /// <summary>
        /// Creates a retry policy for gRPC operations
        /// </summary>
        public static IAsyncPolicy GetRetryPolicy(ILogger logger = null, int retryCount = 3)
        {
            return Policy
                .Handle<RpcException>(ex => IsTransientException(ex))
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timespan, retryAttempt, context) =>
                    {
                        var rpcException = exception as RpcException;
                        logger?.LogWarning(exception, 
                            "Delaying for {delay}ms, then making retry {retry} for gRPC call to {service}. Status: {status}, Detail: {detail}", 
                            timespan.TotalMilliseconds, 
                            retryAttempt,
                            context.GetOrDefault<string>("service"),
                            rpcException?.StatusCode,
                            rpcException?.Status.Detail);
                    });
        }
        
        /// <summary>
        /// Creates a circuit breaker policy for gRPC operations
        /// </summary>
        public static IAsyncPolicy GetCircuitBreakerPolicy(ILogger logger = null, int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return Policy
                .Handle<RpcException>(ex => IsTransientException(ex))
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (exception, timespan, context) =>
                    {
                        var rpcException = exception as RpcException;
                        logger?.LogWarning(exception, 
                            "Circuit breaker opened for {duration}s due to gRPC error. Service: {service}, Status: {status}, Detail: {detail}", 
                            timespan.TotalSeconds,
                            context.GetOrDefault<string>("service"),
                            rpcException?.StatusCode,
                            rpcException?.Status.Detail);
                    },
                    onReset: context =>
                    {
                        logger?.LogInformation("Circuit breaker reset for gRPC service: {service}.", 
                            context.GetOrDefault<string>("service"));
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogInformation("Circuit breaker half-open for gRPC operations.");
                    });
        }
        
        /// <summary>
        /// Creates a timeout policy for gRPC operations
        /// </summary>
        public static IAsyncPolicy GetTimeoutPolicy(ILogger logger = null, int timeoutInSeconds = 10)
        {
            return Policy
                .TimeoutAsync(
                    TimeSpan.FromSeconds(timeoutInSeconds),
                    Polly.Timeout.TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: (context, timespan, task) =>
                    {
                        logger?.LogWarning("gRPC operation timed out after {timeout}s for service: {service}.", 
                            timespan.TotalSeconds, 
                            context.GetOrDefault<string>("service"));
                        return System.Threading.Tasks.Task.CompletedTask;
                    });
        }
        
        /// <summary>
        /// Creates a combined policy with retry, circuit breaker, and timeout for gRPC operations
        /// </summary>
        public static IAsyncPolicy GetCombinedPolicy(ILogger logger = null)
        {
            return Policy.WrapAsync(
                GetRetryPolicy(logger),
                GetCircuitBreakerPolicy(logger),
                GetTimeoutPolicy(logger));
        }
        
        /// <summary>
        /// Creates a policy specifically for handling deadline exceeded errors
        /// </summary>
        public static IAsyncPolicy GetDeadlineExceededPolicy(ILogger logger = null, int retryCount = 2)
        {
            return Policy
                .Handle<RpcException>(ex => ex.StatusCode == StatusCode.DeadlineExceeded)
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)), // Milder exponential backoff
                    onRetry: (exception, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning(exception, 
                            "Deadline exceeded for gRPC call to {service}. Retrying {retry} after {delay}ms with increased timeout.", 
                            context.GetOrDefault<string>("service"),
                            retryAttempt,
                            timespan.TotalMilliseconds);
                    });
        }
    }
}
