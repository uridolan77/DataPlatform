using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace GenericDataPlatform.Common.Logging
{
    /// <summary>
    /// Extensions for configuring Serilog
    /// </summary>
    public static class SerilogExtensions
    {
        /// <summary>
        /// Configures Serilog for the application
        /// </summary>
        public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder, string serviceName)
        {
            // Create Serilog logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironment(builder.Environment.EnvironmentName)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    new JsonFormatter(), 
                    $"logs/{serviceName}-.log", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            // Use Serilog for logging
            builder.Host.UseSerilog();
            
            return builder;
        }
        
        /// <summary>
        /// Adds a request logging middleware
        /// </summary>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress);
                    
                    if (httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgent))
                    {
                        diagnosticContext.Set("UserAgent", userAgent.ToString());
                    }
                    
                    if (httpContext.Request.Headers.TryGetValue("Referer", out var referer))
                    {
                        diagnosticContext.Set("Referer", referer.ToString());
                    }
                    
                    if (httpContext.User.Identity?.IsAuthenticated == true)
                    {
                        diagnosticContext.Set("UserId", httpContext.User.Identity.Name);
                    }
                };
            });
            
            return app;
        }
    }
}
