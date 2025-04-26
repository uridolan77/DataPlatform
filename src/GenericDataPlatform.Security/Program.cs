using System;
using GenericDataPlatform.Common.Logging;
using GenericDataPlatform.Common.Observability;
using GenericDataPlatform.Common.Observability.HealthChecks;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Common.Security.Secrets;
using GenericDataPlatform.Security.Models;
using GenericDataPlatform.Security.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Serilog;

// Configure Serilog
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilog("Security");

// Configure user secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform Security API", Version = "v1" });

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

// Add secret provider
builder.Services.AddSecretProvider(builder.Configuration);

// Add OpenTelemetry
builder.Services.AddOpenTelemetryServices(builder.Configuration, "Security", "1.0.0");

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration, "Security");

// Configure security options
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

// Register SQL Server resilience policy
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
builder.Services.AddSingleton(SqlServerResiliencePolicies.GetCombinedAsyncPolicy(logger));

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["IdentityServer:Issuer"],
            ValidAudiences = builder.Configuration.GetSection("IdentityServer:Audiences").Get<string[]>(),
            ClockSkew = TimeSpan.Zero
        };
    });

// Register security services
builder.Services.AddScoped<IDependencyScanner, DependencyScanner>();
builder.Services.AddScoped<ICodeScanner, CodeScanner>();
builder.Services.AddScoped<ISecurityScanner, SecurityScanner>();
builder.Services.AddScoped<IVulnerabilityRepository, VulnerabilityRepository>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IDataLineageService, DataLineageService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// Register repositories
// Use database repositories if SQL Server connection string is configured, otherwise use file-based repositories
var connectionString = builder.Configuration.GetConnectionString("SqlServer");
if (!string.IsNullOrEmpty(connectionString))
{
    // Register database repositories
    builder.Services.AddScoped<IAlertRepository, DatabaseAlertRepository>();
    builder.Services.AddScoped<IDataLineageRepository, DatabaseDataLineageRepository>();
    builder.Logger.LogInformation("Using database repositories with SQL Server");
}
else
{
    // Register file-based repositories
    builder.Services.AddScoped<IAlertRepository, AlertRepository>();
    builder.Services.AddScoped<IDataLineageRepository, DataLineageRepository>();
    builder.Logger.LogInformation("Using file-based repositories");
}

// Register background services
builder.Services.AddHostedService<SecurityScanBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform Security API v1"));
    app.UseDeveloperExceptionPage();
}

// Add request logging
app.UseRequestLogging();

// Add health checks
app.UseHealthChecks();

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    app.Logger.LogInformation("Starting Security Service");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Security Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
