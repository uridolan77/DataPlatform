using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Interceptor for handling gRPC errors
    /// </summary>
    public class GrpcErrorInterceptor : Interceptor
    {
        private readonly ILogger<GrpcErrorInterceptor> _logger;

        public GrpcErrorInterceptor(ILogger<GrpcErrorInterceptor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Intercepts unary calls to add error handling
        /// </summary>
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(request, context);

            return new AsyncUnaryCall<TResponse>(
                HandleResponse(call.ResponseAsync, context.Method.Name),
                call.ResponseHeadersAsync,
                call.GetStatus,
                call.GetTrailers,
                call.Dispose);
        }

        /// <summary>
        /// Handles the response and logs any errors
        /// </summary>
        private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> responseTask, string methodName)
        {
            try
            {
                return await responseTask;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC call failed. Method: {Method}, Status: {Status}, Detail: {Detail}",
                    methodName, ex.StatusCode, ex.Status.Detail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in gRPC call. Method: {Method}", methodName);
                throw;
            }
        }

        /// <summary>
        /// Intercepts server streaming calls to add error handling
        /// </summary>
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.LogInformation("Starting server streaming call to {Method}", context.Method.Name);
            
            try
            {
                var call = continuation(request, context);
                return new AsyncServerStreamingCall<TResponse>(
                    new ErrorHandlingStreamReader<TResponse>(call.ResponseStream, _logger, context.Method.Name),
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting server streaming call to {Method}", context.Method.Name);
                throw;
            }
        }

        /// <summary>
        /// Intercepts client streaming calls to add error handling
        /// </summary>
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.LogInformation("Starting client streaming call to {Method}", context.Method.Name);
            
            try
            {
                var call = continuation(context);
                return new AsyncClientStreamingCall<TRequest, TResponse>(
                    call.RequestStream,
                    HandleResponse(call.ResponseAsync, context.Method.Name),
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting client streaming call to {Method}", context.Method.Name);
                throw;
            }
        }

        /// <summary>
        /// Intercepts duplex streaming calls to add error handling
        /// </summary>
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.LogInformation("Starting duplex streaming call to {Method}", context.Method.Name);
            
            try
            {
                var call = continuation(context);
                return new AsyncDuplexStreamingCall<TRequest, TResponse>(
                    call.RequestStream,
                    new ErrorHandlingStreamReader<TResponse>(call.ResponseStream, _logger, context.Method.Name),
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting duplex streaming call to {Method}", context.Method.Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Stream reader that handles errors
    /// </summary>
    internal class ErrorHandlingStreamReader<T> : IAsyncStreamReader<T>
    {
        private readonly IAsyncStreamReader<T> _inner;
        private readonly ILogger _logger;
        private readonly string _methodName;

        public ErrorHandlingStreamReader(IAsyncStreamReader<T> inner, ILogger logger, string methodName)
        {
            _inner = inner;
            _logger = logger;
            _methodName = methodName;
        }

        public T Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            try
            {
                return await _inner.MoveNext(cancellationToken);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error reading from stream. Method: {Method}, Status: {Status}, Detail: {Detail}",
                    _methodName, ex.StatusCode, ex.Status.Detail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading from stream. Method: {Method}", _methodName);
                throw;
            }
        }
    }
}
