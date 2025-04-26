using System;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.DatabaseService.Middleware;
using GenericDataPlatform.DatabaseService.Repositories;
using GenericDataPlatform.DatabaseService.Services.SchemaEvolution;
using GenericDataPlatform.DatabaseService.Services.SchemaEvolution.MigrationPlan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform Database API", Version = "v1" });
});

// Configure database options
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("DatabaseOptions"));

// Get logger for resilience policies
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Register database resilience policies
builder.Services.AddSingleton(DatabaseResiliencePolicies.GetSqlServerCombinedPolicy(logger));
builder.Services.AddSingleton(DatabaseResiliencePolicies.GetMySqlCombinedPolicy(logger));
builder.Services.AddSingleton(DatabaseResiliencePolicies.GetPostgreSqlCombinedPolicy(logger));

// Register repositories
builder.Services.AddScoped<PostgresRepository>();
builder.Services.AddScoped<SqlServerRepository>();
builder.Services.AddScoped<MySqlRepository>();
builder.Services.AddScoped<DbRepositoryFactory>();
builder.Services.AddScoped<IDbRepository>(provider =>
{
    var options = provider.GetRequiredService<IOptions<DatabaseOptions>>();
    var factory = provider.GetRequiredService<DbRepositoryFactory>();
    return factory.CreateRepository(options.Value.DefaultDatabaseType);
});

// Register schema evolution services
builder.Services.AddScoped<SchemaComparer>();
builder.Services.AddScoped<SchemaValidator>();
builder.Services.AddScoped<MigrationPlanGeneratorFactory>();
builder.Services.AddScoped<ISchemaEvolutionService, SchemaEvolutionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform Database API v1"));
}

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
