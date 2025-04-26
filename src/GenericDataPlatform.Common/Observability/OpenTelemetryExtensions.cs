using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GenericDataPlatform.Common.Observability
{
    /// <summary>
    /// Extensions for configuring OpenTelemetry
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        /// <summary>
        /// Adds OpenTelemetry to the service collection
        /// </summary>
        public static IServiceCollection AddOpenTelemetryServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion = "1.0.0")
        {
            // Check if OpenTelemetry is enabled
            var enabled = configuration.GetValue<bool>("OpenTelemetry:Enabled", true);
            if (!enabled)
            {
                return services;
            }
            
            // Get OpenTelemetry endpoint
            var endpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
            
            // Configure OpenTelemetry
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName, serviceVersion)
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector())
                .WithTracing(tracing => ConfigureTracing(tracing, endpoint, serviceName, configuration))
                .WithMetrics(metrics => ConfigureMetrics(metrics, endpoint, serviceName, configuration));
            
            return services;
        }
        
        /// <summary>
        /// Configures tracing
        /// </summary>
        private static TracerProviderBuilder ConfigureTracing(
            TracerProviderBuilder builder,
            string endpoint,
            string serviceName,
            IConfiguration configuration)
        {
            // Add instrumentation
            builder
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.headers.user_agent", request.Headers["User-Agent"].ToString());
                        activity.SetTag("http.request.headers.host", request.Headers["Host"].ToString());
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response.headers.server", response.Headers["Server"].ToString());
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (request.RequestUri != null)
                        {
                            activity.SetTag("http.request.uri", request.RequestUri.ToString());
                        }
                    };
                    options.EnrichWithHttpResponseMessage = (activity, response) =>
                    {
                        if (response.Headers.Contains("Server"))
                        {
                            activity.SetTag("http.response.headers.server", response.Headers.GetValues("Server").FirstOrDefault());
                        }
                    };
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.SetDbStatementForText = true;
                    options.EnableConnectionLevelAttributes = true;
                })
                // Use custom ActivitySource for gRPC tracing instead of a direct instrumentation
                .AddSource("Grpc.Net.Client")
                .AddSource(serviceName);
            
            // Add exporters
            var exporterType = configuration["OpenTelemetry:Exporter"]?.ToLowerInvariant() ?? "otlp";
            
            switch (exporterType)
            {
                case "otlp":
                    builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                    break;
                
                case "jaeger":
                    builder.AddJaegerExporter(options => options.AgentHost = configuration["OpenTelemetry:JaegerHost"] ?? "localhost");
                    break;
                
                case "zipkin":
                    builder.AddZipkinExporter(options => options.Endpoint = new Uri(configuration["OpenTelemetry:ZipkinEndpoint"] ?? "http://localhost:9411/api/v2/spans"));
                    break;
                
                case "console":
                    builder.AddConsoleExporter();
                    break;
            }
            
            return builder;
        }
        
        /// <summary>
        /// Configures metrics
        /// </summary>
        private static MeterProviderBuilder ConfigureMetrics(
            MeterProviderBuilder builder,
            string endpoint,
            string serviceName,
            IConfiguration configuration)
        {
            // Add instrumentation
            builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // Use System.Diagnostics.Metrics for process and runtime instrumentation
                .AddMeter("System.Runtime")
                .AddMeter("System.Process")
                .AddMeter(serviceName);
            
            // Add exporters
            var exporterType = configuration["OpenTelemetry:MetricsExporter"]?.ToLowerInvariant() ?? "otlp";
            
            switch (exporterType)
            {
                case "otlp":
                    builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                    break;
                
                case "prometheus":
                    builder.AddPrometheusExporter();
                    break;
                
                case "console":
                    builder.AddConsoleExporter();
                    break;
            }
            
            return builder;
        }
        
        /// <summary>
        /// Helper extension to add EF Core with telemetry
        /// </summary>
        public static IServiceCollection AddEntityFrameworkSqlServerWithTelemetry(
            this IServiceCollection services)
        {
            // This method would be called from the Startup.cs or Program.cs
            // to register EF Core with telemetry enabled
            
            // Register a diagnostic listener for EF Core
            services.AddSingleton<DiagnosticListener>(provider => 
                new DiagnosticListener("Microsoft.EntityFrameworkCore"));
            
            return services;
        }
    }
}
