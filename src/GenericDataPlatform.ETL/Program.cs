using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using GenericDataPlatform.Common.Configuration;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.ETL.Workflows.Simple;
using GenericDataPlatform.ETL.Enrichers;
using GenericDataPlatform.ETL.Extractors.Base;
using GenericDataPlatform.ETL.Extractors.Rest;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Loaders.Database;
using GenericDataPlatform.ETL.Middleware;
using GenericDataPlatform.ETL.Transformers.Base;
using GenericDataPlatform.ETL.Transformers.Csv;
using GenericDataPlatform.ETL.Transformers.Json;
using GenericDataPlatform.ETL.Transformers.Xml;
using GenericDataPlatform.ETL.Validators;
using GenericDataPlatform.ETL.Workflows;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Monitoring;
using GenericDataPlatform.ETL.Workflows.Repositories;
using GenericDataPlatform.ETL.Workflows.Tracking;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure user secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register configuration service
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

// Add OpenAPI/Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform ETL API", Version = "v1" });
});

// Add HTTP client factory with resilience policies
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Default HTTP client with resilience policies
builder.Services.AddHttpClient("DefaultClient")
    .AddPolicyHandler((services, request) => HttpClientResiliencePolicies.GetRetryPolicy(logger))
    .AddPolicyHandler((services, request) => HttpClientResiliencePolicies.GetCircuitBreakerPolicy(logger))
    .AddPolicyHandler((services, request) => HttpClientResiliencePolicies.GetTimeoutPolicy(logger));

// Named HTTP client for REST API extractor
builder.Services.AddHttpClient("RestApiExtractor")
    .AddPolicyHandler((services, request) =>
    {
        request.SetPolicyExecutionContext(new Context
        {
            ["url"] = request.RequestUri?.ToString()
        });
        return HttpClientResiliencePolicies.GetCombinedPolicy(logger);
    });

// Register ETL components
// Extractors
builder.Services.AddScoped<IExtractor, RestApiExtractor>();

// Transformers
builder.Services.AddScoped<ITransformer, JsonTransformer>();
builder.Services.AddScoped<ITransformer, CsvTransformer>();
builder.Services.AddScoped<ITransformer, XmlTransformer>();

// Validators
builder.Services.AddScoped<IValidator, SchemaValidator>();
builder.Services.AddScoped<IValidator, DataQualityValidator>();

// Enrichers
builder.Services.AddScoped<IEnricher, DataEnricher>();
builder.Services.AddScoped<IEnricher, LookupEnricher>();

// Loaders
builder.Services.AddScoped<ILoader, DatabaseLoader>();

// Register workflow repositories and services
builder.Services.AddScoped<IWorkflowEngine, SimpleWorkflowEngine>();

// Configure workflow options
builder.Services.Configure<WorkflowOptions>(builder.Configuration.GetSection("WorkflowOptions"));

// Register SQL Server resilience policy for workflow repository
var workflowLogger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
builder.Services.AddSingleton<IAsyncPolicy>(SqlServerResiliencePolicies.GetCombinedAsyncPolicy(workflowLogger));

// Register workflow repositories and services
builder.Services.AddScoped<IWorkflowRepository, InMemoryWorkflowRepository>();
builder.Services.AddScoped<IWorkflowMonitor, BasicMonitor>();
builder.Services.AddScoped<IWorkflowEngine, SimpleWorkflowEngine>();
builder.Services.AddScoped<IEtlWorkflowService, SimpleEtlWorkflowService>();
builder.Services.AddScoped<IWorkflowDefinitionBuilder, SimpleWorkflowDefinitionBuilder>();
builder.Services.AddScoped<IDataLineageService, SimpleDataLineageService>();

// Comment out the HTTP client registration for IDataLineageService since we're using a simple implementation
// builder.Services.AddHttpClient<IDataLineageService, DataLineageService>(client =>
// {
//     client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:SecurityService"] ?? "https://localhost:5001");
// });
builder.Services.AddScoped<ILineageTracker, SimpleLineageTracker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform ETL API v1"));
}

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

// Map API endpoints
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
