using System;
using GenericDataPlatform.Common.Security;
using GenericDataPlatform.Common.Security.Certificates;
using GenericDataPlatform.Compliance.AccessControl;
using GenericDataPlatform.Compliance.Auditing;
using GenericDataPlatform.Compliance.Models;
using GenericDataPlatform.Compliance.Privacy;
using GenericDataPlatform.Compliance.Repositories;
using GenericDataPlatform.Compliance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure user secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "logs/compliance-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform Compliance API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure compliance options
builder.Services.Configure<ComplianceOptions>(builder.Configuration.GetSection("ComplianceOptions"));
builder.Services.Configure<PIIOptions>(builder.Configuration.GetSection("PIIOptions"));

// Configure OpenTelemetry
var serviceName = "ComplianceService";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion)
        .AddTelemetrySdk()
        .AddEnvironmentVariableDetector())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(serviceName)
        .AddOtlpExporter(options => options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(serviceName)
        .AddOtlpExporter(options => options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317")));

// Register repositories
builder.Services.AddSingleton<IAuditRepository, AuditRepository>();
builder.Services.AddSingleton<IPermissionRepository, PermissionRepository>();

// Register services
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddSingleton<IAccessControlService, AccessControlService>();
builder.Services.AddSingleton<IPIIDetectionService, PIIDetectionService>();

// Register gRPC services
builder.Services.AddSecureGrpcServices();
builder.Services.AddGrpc();

// Configure certificates for mTLS
builder.Services.Configure<CertificateOptions>(options =>
{
    options.CertificateDirectory = builder.Configuration["Certificates:Directory"] ?? "certificates";
    options.CertificatePassword = builder.Configuration["Certificates:Password"] ?? "changeme";
    options.ServiceName = "compliance";
});

// Configure Kestrel for gRPC with mTLS
builder.WebHost.ConfigureSecureGrpc(int.Parse(builder.Configuration["GrpcPort"] ?? "5001"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform Compliance API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<ComplianceGrpcService>();

try
{
    Log.Information("Starting Compliance Service");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Compliance Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
