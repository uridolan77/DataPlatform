using GenericDataPlatform.ETL.Extractors.Base;
using GenericDataPlatform.ETL.Extractors.Rest;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Loaders.Database;
using GenericDataPlatform.ETL.Processors;
using GenericDataPlatform.ETL.Transformers.Base;
using GenericDataPlatform.ETL.Transformers.Json;
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

// Loaders
builder.Services.AddScoped<ILoader, DatabaseLoader>();

// Pipeline processor
builder.Services.AddScoped<IPipelineProcessor, PipelineProcessor>();

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
