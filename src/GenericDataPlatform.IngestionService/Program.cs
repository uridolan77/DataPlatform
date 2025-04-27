using System;
using System.Net;
using System.Net.Http;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.IngestionService.Connectors;
using GenericDataPlatform.IngestionService.Connectors.Database;
using GenericDataPlatform.IngestionService.Connectors.FileSystem;
using GenericDataPlatform.IngestionService.Connectors.Rest;
using GenericDataPlatform.IngestionService.Connectors.Streaming;
using GenericDataPlatform.IngestionService.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform Ingestion API", Version = "v1" });
});

// Add HTTP client factory with resilience policies
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Default HTTP client with resilience policies
builder.Services.AddHttpClient("DefaultClient", client => {
    client.DefaultRequestHeaders.Add("User-Agent", "GenericDataPlatform.IngestionService");
})
    .AddPolicyHandler(HttpClientResiliencePolicies.GetRetryPolicy(logger))
    .AddPolicyHandler(HttpClientResiliencePolicies.GetCircuitBreakerPolicy(logger))
    .AddPolicyHandler(HttpClientResiliencePolicies.GetTimeoutPolicy(logger));

// Named HTTP client for REST API connector
builder.Services.AddHttpClient("RestApiClient")
    .AddPolicyHandler((request) =>
    {
        var policyContext = new Context
        {
            ["url"] = request.RequestUri?.ToString()
        };
        return HttpClientResiliencePolicies.GetCombinedPolicy(logger).WithPolicyKey("RestApiClient");
    });

// Register database connectors
builder.Services.AddScoped<SqlServerConnector>();
builder.Services.AddScoped<MySqlConnector>();
builder.Services.AddScoped<PostgreSqlConnector>();

// Register file system connectors
builder.Services.AddScoped<LocalFileSystemConnector>();
builder.Services.AddScoped<SftpConnector>();

// Register REST API connector
builder.Services.AddScoped<RestApiConnector>();

// Register streaming connectors
builder.Services.AddScoped<KafkaConnector>();
builder.Services.AddScoped<EventHubsConnector>();

// Register connector factory
builder.Services.AddScoped<ConnectorFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform Ingestion API v1"));
}

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
