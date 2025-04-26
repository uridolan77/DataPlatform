using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebScraper.RegulatoryFramework.Implementation
{
    /// <summary>
    /// HTTP client handler with rate limiting and timeout management
    /// </summary>
    public class RateLimitedHttpClientHandler : HttpClientHandler
    {
        private readonly SemaphoreSlim _throttler;
        private readonly int _timeoutSeconds;
        
        /// <summary>
        /// Initializes a new instance of the RateLimitedHttpClientHandler class
        /// </summary>
        /// <param name="maxConcurrent">Maximum number of concurrent requests</param>
        /// <param name="timeoutSeconds">Timeout in seconds for each request</param>
        public RateLimitedHttpClientHandler(int maxConcurrent, int timeoutSeconds)
        {
            _throttler = new SemaphoreSlim(maxConcurrent);
            _timeoutSeconds = timeoutSeconds;
        }
        
        /// <summary>
        /// Sends an HTTP request with rate limiting and timeout
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Wait for semaphore with cancellation
            await _throttler.WaitAsync(cancellationToken);
            
            try
            {
                // Create a linked token source with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));
                
                // Send request with the linked token
                return await base.SendAsync(request, cts.Token);
            }
            finally
            {
                // Release semaphore
                _throttler.Release();
            }
        }
        
        /// <summary>
        /// Disposes the HTTP client handler and resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _throttler?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}