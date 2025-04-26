using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenericDataPlatform.Common.Observability.HealthChecks
{
    /// <summary>
    /// Extensions for configuring health checks
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Adds health checks to the service collection
        /// </summary>
        public static IServiceCollection AddHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName)
        {
            // Add health checks
            var healthChecks = services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "api" });
            
            // Add SQL Server health check
            var sqlServerConnectionString = configuration.GetConnectionString("SqlServer");
            if (!string.IsNullOrEmpty(sqlServerConnectionString))
            {
                healthChecks.AddSqlServer(
                    sqlServerConnectionString,
                    name: "sql-server-check",
                    tags: new[] { "database", "sql-server" });
            }
            
            // Add PostgreSQL health check
            var postgresConnectionString = configuration.GetConnectionString("Postgres");
            if (!string.IsNullOrEmpty(postgresConnectionString))
            {
                healthChecks.AddNpgSql(
                    postgresConnectionString,
                    name: "postgres-check",
                    tags: new[] { "database", "postgres" });
            }
            
            // Add Redis health check
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                healthChecks.AddRedis(
                    redisConnectionString,
                    name: "redis-check",
                    tags: new[] { "cache", "redis" });
            }
            
            // Add RabbitMQ health check
            var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ");
            if (!string.IsNullOrEmpty(rabbitMqConnectionString))
            {
                healthChecks.AddRabbitMQ(
                    rabbitMqConnectionString,
                    name: "rabbitmq-check",
                    tags: new[] { "messaging", "rabbitmq" });
            }
            
            // Add Kafka health check
            var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"];
            if (!string.IsNullOrEmpty(kafkaBootstrapServers))
            {
                healthChecks.AddKafka(
                    kafkaBootstrapServers,
                    name: "kafka-check",
                    tags: new[] { "messaging", "kafka" });
            }
            
            // Add URL health check for dependent services
            var serviceEndpoints = configuration.GetSection("ServiceEndpoints").GetChildren();
            foreach (var endpoint in serviceEndpoints)
            {
                var url = endpoint.Value;
                if (!string.IsNullOrEmpty(url))
                {
                    healthChecks.AddUrlGroup(
                        new Uri($"{url}/health"),
                        name: $"{endpoint.Key}-check",
                        tags: new[] { "service", endpoint.Key.ToLowerInvariant() });
                }
            }
            
            return services;
        }
        
        /// <summary>
        /// Maps health check endpoints
        /// </summary>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
        {
            // Map health check endpoints
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    var result = new JObject(
                        new JProperty("status", report.Status.ToString()),
                        new JProperty("results", new JObject(report.Entries.Select(entry => new JProperty(entry.Key, new JObject(
                            new JProperty("status", entry.Value.Status.ToString()),
                            new JProperty("description", entry.Value.Description),
                            new JProperty("data", JObject.FromObject(entry.Value.Data))
                        ))))));
                    
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result.ToString(Formatting.Indented));
                }
            });
            
            // Map liveness probe endpoint
            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live"),
                ResponseWriter = async (context, report) =>
                {
                    var result = JObject.FromObject(new
                    {
                        status = report.Status.ToString()
                    });
                    
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result.ToString(Formatting.Indented));
                }
            });
            
            // Map readiness probe endpoint
            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("ready"),
                ResponseWriter = async (context, report) =>
                {
                    var result = JObject.FromObject(new
                    {
                        status = report.Status.ToString()
                    });
                    
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result.ToString(Formatting.Indented));
                }
            });
            
            return app;
        }
    }
}
