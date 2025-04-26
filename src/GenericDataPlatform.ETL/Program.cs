using GenericDataPlatform.ETL.Enrichers;
using GenericDataPlatform.ETL.Extractors.Base;
using GenericDataPlatform.ETL.Extractors.Rest;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Loaders.Database;
using GenericDataPlatform.ETL.Processors;
using GenericDataPlatform.ETL.Transformers.Base;
using GenericDataPlatform.ETL.Transformers.Csv;
using GenericDataPlatform.ETL.Transformers.Json;
using GenericDataPlatform.ETL.Transformers.Xml;
using GenericDataPlatform.ETL.Validators;
using GenericDataPlatform.ETL.Workflows;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.StepProcessors;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add OpenAPI/Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform ETL API", Version = "v1" });
});

// Add HTTP client factory
builder.Services.AddHttpClient();

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
builder.Services.AddScoped<IPipelineProcessor, PipelineProcessor>();

// Workflow engine components
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IWorkflowStepProcessor, ExtractStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, TransformStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, ValidateStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, EnrichStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, LoadStepProcessor>();
builder.Services.AddScoped<IWorkflowStepProcessor, BranchStepProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform ETL API v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
