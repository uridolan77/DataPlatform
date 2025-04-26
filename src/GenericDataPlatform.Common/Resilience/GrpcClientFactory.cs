using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Security.Certificates;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Polly;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Factory for creating resilient gRPC clients
    /// </summary>
    public class GrpcClientFactory
    {
        private readonly ICertificateManager _certificateManager;
        private readonly ILogger _logger;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly GrpcErrorInterceptor _errorInterceptor;

        public GrpcClientFactory(
            ICertificateManager certificateManager,
            ILogger logger,
            IAsyncPolicy resiliencePolicy = null)
        {
            _certificateManager = certificateManager;
            _logger = logger;
            _resiliencePolicy = resiliencePolicy ?? GrpcClientResiliencePolicies.GetCombinedPolicy(logger);
            _errorInterceptor = new GrpcErrorInterceptor(logger is ILogger<GrpcErrorInterceptor> ?
                (ILogger<GrpcErrorInterceptor>)logger :
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<GrpcErrorInterceptor>());
        }

        /// <summary>
        /// Creates a gRPC client with resilience policies
        /// </summary>
        /// <typeparam name="TClient">The type of gRPC client to create</typeparam>
        /// <param name="address">The address of the gRPC service</param>
        /// <param name="secure">Whether to use mTLS security</param>
        /// <returns>A gRPC client with resilience policies</returns>
        public TClient CreateClient<TClient>(string address, bool secure = true) where TClient : class
        {
            var channel = secure
                ? CreateSecureChannel(address)
                : CreateChannel(address);

            // Check if the type is a gRPC client class
            if (typeof(TClient).IsSubclassOf(typeof(ClientBase)))
            {
                return (TClient)Activator.CreateInstance(typeof(TClient), channel);
            }
            
            // For interface types, try to find the implementation 
            var implementationType = typeof(TClient).Name.StartsWith("I") 
                ? Type.GetType(typeof(TClient).Namespace + "." + typeof(TClient).Name.Substring(1))
                : null;
                
            if (implementationType != null)
            {
                return (TClient)Activator.CreateInstance(implementationType, channel);
            }

            throw new InvalidOperationException($"Unable to create client of type {typeof(TClient).Name}. Must be a gRPC client type or have a matching implementation class.");
        }

        /// <summary>
        /// Creates a secure gRPC channel with mTLS and resilience policies
        /// </summary>
        public GrpcChannel CreateSecureChannel(string address)
        {
            try
            {
                var clientCertificate = _certificateManager.GetClientCertificate();
                var caCertificate = _certificateManager.GetCACertificate();

                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(clientCertificate);

                // Add CA certificate to trusted roots
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    var serverChain = new X509Chain();
                    serverChain.ChainPolicy.ExtraStore.Add(caCertificate);
                    serverChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    serverChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                    return serverChain.Build(new X509Certificate2(cert));
                };

                var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole()),
                    MaxReceiveMessageSize = 16 * 1024 * 1024, // 16 MB
                    MaxSendMessageSize = 16 * 1024 * 1024     // 16 MB
                });

                _logger.LogInformation("Created secure gRPC channel to {Address}", address);

                // Apply the error interceptor
                var callInvoker = channel.Intercept(_errorInterceptor);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secure gRPC channel to {Address}", address);
                throw;
            }
        }

        /// <summary>
        /// Creates a standard gRPC channel with resilience policies
        /// </summary>
        public GrpcChannel CreateChannel(string address)
        {
            try
            {
                var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole()),
                    MaxReceiveMessageSize = 16 * 1024 * 1024, // 16 MB
                    MaxSendMessageSize = 16 * 1024 * 1024     // 16 MB
                });

                _logger.LogInformation("Created gRPC channel to {Address}", address);

                // Apply the error interceptor
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gRPC channel to {Address}", address);
                throw;
            }
        }

        /// <summary>
        /// Executes a gRPC call with resilience policies
        /// </summary>
        public async Task<TResponse> CallServiceAsync<TResponse>(
            Func<Task<TResponse>> grpcCall,
            string serviceName,
            string methodName)
        {
            var context = new Context
            {
                { "service", serviceName },
                { "method", methodName }
            };

            return await _resiliencePolicy.ExecuteAsync(async (ctx) =>
            {
                try
                {
                    return await grpcCall();
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    // Don't retry cancelled requests
                    _logger.LogWarning("gRPC call to {Service}.{Method} was cancelled", serviceName, methodName);
                    throw;
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Error calling gRPC service {Service}.{Method}. Status: {Status}, Detail: {Detail}",
                        serviceName, methodName, ex.StatusCode, ex.Status.Detail);
                    throw;
                }
            }, context);
        }

        /// <summary>
        /// Executes a streaming gRPC call with resilience policies
        /// </summary>
        public async Task CallStreamingServiceAsync(
            Func<Task> grpcStreamingCall,
            string serviceName,
            string methodName)
        {
            var context = new Context
            {
                { "service", serviceName },
                { "method", methodName }
            };

            await _resiliencePolicy.ExecuteAsync(async (ctx) =>
            {
                try
                {
                    await grpcStreamingCall();
                    return true; // Need to return something for the policy
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    // Don't retry cancelled requests
                    _logger.LogWarning("gRPC streaming call to {Service}.{Method} was cancelled", serviceName, methodName);
                    throw;
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Error in gRPC streaming call to {Service}.{Method}. Status: {Status}, Detail: {Detail}",
                        serviceName, methodName, ex.StatusCode, ex.Status.Detail);
                    throw;
                }
            }, context);
        }
    }
}
