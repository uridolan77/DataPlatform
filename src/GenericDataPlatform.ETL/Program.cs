using System;
using System.Net;
using System.Net.Http;
using GenericDataPlatform.Common.Configuration;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.ETL.Enrichers;
using GenericDataPlatform.ETL.Extractors.Base;
using GenericDataPlatform.ETL.Extractors.Rest;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Loaders.Database;
using GenericDataPlatform.ETL.Middleware;
using GenericDataPlatform.ETL.Processors;
using GenericDataPlatform.ETL.Transformers.Base;
using GenericDataPlatform.ETL.Transformers.Csv;
using GenericDataPlatform.ETL.Transformers.Json;
using GenericDataPlatform.ETL.Transformers.Xml;
using GenericDataPlatform.ETL.Validators;
using GenericDataPlatform.ETL.Workflows;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Repositories;
using GenericDataPlatform.ETL.Workflows.StepProcessors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
builder.Services.AddHttpClient()
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

// Pipeline processor
builder.Services.AddScoped<IPipelineProcessor>(sp => new PipelineProcessor(
    sp,
    sp.GetServices<IExtractor>(),
    sp.GetServices<ITransformer>(),
    sp.GetServices<ILoader>(),
    sp.GetServices<IValidator>(),
    sp.GetServices<IEnricher>(),
    sp.GetRequiredService<ILogger<PipelineProcessor>>()
));

// Register workflow options
builder.Services.Configure<WorkflowOptions>(builder.Configuration.GetSection("WorkflowOptions"));

// Register workflow repositories
builder.Services.AddSingleton<IWorkflowRepository, DatabaseWorkflowRepository>();

// Register workflow monitoring
builder.Services.AddSingleton<IWorkflowMonitor, Workflows.Monitoring.WorkflowMonitor>();

// Workflow engine components
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IWorkflowStepProcessor, ExtractStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, TransformStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, ValidateStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, EnrichStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, LoadStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, BranchStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, CustomStepProcessor>();

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

app.MapControllers();

app.Run();
