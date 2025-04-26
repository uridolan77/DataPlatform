using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using GenericDataPlatform.Common.Security.Certificates;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Common.Security
{
    /// <summary>
    /// Extensions for configuring gRPC with mTLS
    /// </summary>
    public static class GrpcExtensions
    {
        /// <summary>
        /// Adds gRPC services with mTLS
        /// </summary>
        public static IServiceCollection AddSecureGrpcServices(this IServiceCollection services)
        {
            // Add certificate manager
            services.AddSingleton<ICertificateManager, CertificateManager>();
            
            // Add gRPC services
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
                options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
            });
            
            return services;
        }
        
        /// <summary>
        /// Configures Kestrel to use mTLS for gRPC
        /// </summary>
        public static IWebHostBuilder ConfigureSecureGrpc(this IWebHostBuilder builder, int grpcPort = 5001)
        {
            return builder.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(grpcPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        var serviceProvider = options.ApplicationServices;
                        var certificateManager = serviceProvider.GetRequiredService<ICertificateManager>();
                        
                        httpsOptions.ServerCertificate = certificateManager.GetServerCertificate();
                        httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
                        httpsOptions.ClientCertificateValidation = (certificate, chain, errors) =>
                        {
                            // Validate client certificate against CA
                            var caCertificate = certificateManager.GetCACertificate();
                            var clientChain = new X509Chain();
                            clientChain.ChainPolicy.ExtraStore.Add(caCertificate);
                            clientChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                            clientChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                            
                            return clientChain.Build(new X509Certificate2(certificate));
                        };
                    });
                });
            });
        }
        
        /// <summary>
        /// Creates a secure gRPC channel with mTLS
        /// </summary>
        public static GrpcChannel CreateSecureChannel(string address, ICertificateManager certificateManager, ILogger logger)
        {
            try
            {
                var clientCertificate = certificateManager.GetClientCertificate();
                var caCertificate = certificateManager.GetCACertificate();
                
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
                    LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole())
                });
                
                logger.LogInformation("Created secure gRPC channel to {Address}", address);
                
                return channel;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating secure gRPC channel to {Address}", address);
                throw;
            }
        }
    }
}
