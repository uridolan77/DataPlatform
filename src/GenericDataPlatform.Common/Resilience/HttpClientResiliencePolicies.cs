using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Common HTTP client resilience policies
    /// </summary>
    public static class HttpClientResiliencePolicies
    {
        /// <summary>
        /// Creates a retry policy for HTTP requests
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger = null, int retryCount = 3)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException, 5XX and 408
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // 429
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning("Delaying for {delay}ms, then making retry {retry} for {url}.", 
                            timespan.TotalMilliseconds, retryAttempt, context.GetOrDefault<string>("url"));
                    });
        }
        
        /// <summary>
        /// Creates a circuit breaker policy for HTTP requests
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger = null, int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (outcome, timespan, context) =>
                    {
                        logger?.LogWarning("Circuit breaker opened for {duration}s due to: {outcome} for {url}.", 
                            timespan.TotalSeconds, 
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(),
                            context.GetOrDefault<string>("url"));
                    },
                    onReset: context =>
                    {
                        logger?.LogInformation("Circuit breaker reset for {url}.", 
                            context.GetOrDefault<string>("url"));
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogInformation("Circuit breaker half-open.");
                    });
        }
        
        /// <summary>
        /// Creates a timeout policy for HTTP requests
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ILogger logger = null, int timeoutInSeconds = 10)
        {
            return Policy
                .TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromSeconds(timeoutInSeconds),
                    TimeoutStrategy.Optimistic,
                    onTimeoutAsync: (context, timespan, task) =>
                    {
                        logger?.LogWarning("Request timed out after {timeout}s for {url}.", 
                            timespan.TotalSeconds, context.GetOrDefault<string>("url"));
                        return System.Threading.Tasks.Task.CompletedTask;
                    });
        }
        
        /// <summary>
        /// Creates a combined policy with retry, circuit breaker, and timeout
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger = null)
        {
            return Policy.WrapAsync(
                GetRetryPolicy(logger),
                GetCircuitBreakerPolicy(logger),
                GetTimeoutPolicy(logger));
        }
    }
}
