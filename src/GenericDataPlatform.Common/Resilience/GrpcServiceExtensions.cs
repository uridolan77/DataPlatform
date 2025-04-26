using System;
using GenericDataPlatform.Common.Security;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Extensions for registering resilient gRPC services
    /// </summary>
    public static class GrpcServiceExtensions
    {
        /// <summary>
        /// Adds resilient gRPC client factory to the service collection
        /// </summary>
        public static IServiceCollection AddResilientGrpcClientFactory(this IServiceCollection services)
        {
            // Register the error interceptor
            services.AddSingleton<GrpcErrorInterceptor>();

            // Register the GrpcClientFactory with default policies
            services.AddSingleton<GrpcClientFactory>(provider =>
            {
                var certificateManager = provider.GetRequiredService<ICertificateManager>();
                var logger = provider.GetRequiredService<ILogger<GrpcClientFactory>>();
                var policy = GrpcClientResiliencePolicies.GetCombinedPolicy(logger);

                return new GrpcClientFactory(certificateManager, logger, policy);
            });

            return services;
        }

        /// <summary>
        /// Adds resilient gRPC client factory to the service collection with custom policies
        /// </summary>
        public static IServiceCollection AddResilientGrpcClientFactory(
            this IServiceCollection services,
            Func<ILogger, IAsyncPolicy> policyFactory)
        {
            // Register the error interceptor
            services.AddSingleton<GrpcErrorInterceptor>();

            // Register the GrpcClientFactory with custom policies
            services.AddSingleton<GrpcClientFactory>(provider =>
            {
                var certificateManager = provider.GetRequiredService<ICertificateManager>();
                var logger = provider.GetRequiredService<ILogger<GrpcClientFactory>>();
                var policy = policyFactory(logger);

                return new GrpcClientFactory(certificateManager, logger, policy);
            });

            return services;
        }

        /// <summary>
        /// Adds gRPC client interceptors to the service collection
        /// </summary>
        public static IServiceCollection AddGrpcClientInterceptors(this IServiceCollection services)
        {
            // Register the error interceptor
            services.AddSingleton<GrpcErrorInterceptor>();

            // Configure gRPC client factory to use interceptors
            services.ConfigureGrpcClientFactory(options =>
            {
                var provider = services.BuildServiceProvider();
                var interceptor = provider.GetRequiredService<GrpcErrorInterceptor>();

                options.Interceptors.Add(interceptor);
            });

            return services;
        }

        /// <summary>
        /// Adds a resilient gRPC client of the specified type to the service collection
        /// </summary>
        public static IServiceCollection AddResilientGrpcClient<TClient, TImplementation>(
            this IServiceCollection services,
            string address,
            bool secure = true)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddSingleton<TClient>(provider =>
            {
                var factory = provider.GetRequiredService<GrpcClientFactory>();
                return factory.CreateClient<TImplementation>(address, secure);
            });

            return services;
        }

        /// <summary>
        /// Adds a resilient gRPC client of the specified type to the service collection
        /// with the address from configuration
        /// </summary>
        public static IServiceCollection AddResilientGrpcClient<TClient, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, string> addressFactory,
            bool secure = true)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddSingleton<TClient>(provider =>
            {
                var factory = provider.GetRequiredService<GrpcClientFactory>();
                var address = addressFactory(provider);
                return factory.CreateClient<TImplementation>(address, secure);
            });

            return services;
        }
    }
}
