using System.Text;
using System.Net;
using System.Net.Http;
using GenericDataPlatform.API.Middleware;
using GenericDataPlatform.API.Models;
using GenericDataPlatform.API.Models.Auth;
using GenericDataPlatform.API.Repositories;
using GenericDataPlatform.API.Services.Auth;
using GenericDataPlatform.API.Services.Data;
using GenericDataPlatform.Common.Logging;
using GenericDataPlatform.Common.Observability;
using GenericDataPlatform.Common.Observability.HealthChecks;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Common.Security.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Polly.Retry;

// Configure Serilog
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilog("API");

// Configure user secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add secret provider
builder.Services.AddSecretProvider(builder.Configuration);

// Add OpenTelemetry
builder.Services.AddOpenTelemetryServices(builder.Configuration, "API", "1.0.0");

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration, "API");

// Configure API options
builder.Services.Configure<ApiOptions>(options =>
{
    options.ConnectionStrings = builder.Configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>();
    options.ServiceEndpoints = builder.Configuration.GetSection("ServiceEndpoints").Get<ServiceEndpoints>();
});

// Register SQL Server resilience policy
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
builder.Services.AddSingleton(SqlServerResiliencePolicies.GetCombinedAsyncPolicy(logger));

// Add HTTP client factory
builder.Services.AddHttpClient();

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

// Get JWT secret from secret provider
var secretProvider = builder.Services.BuildServiceProvider().GetRequiredService<ISecretProvider>();
var secret = secretProvider.GetSecretAsync("jwt/secret").GetAwaiter().GetResult()
    ?? jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JWT secret not configured");

var key = Encoding.ASCII.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Generic Data Platform API", Version = "v1" });

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

// Register services
// Use DatabaseUserRepository if SQL Server connection string is configured, otherwise use MockUserRepository
var connectionString = builder.Configuration.GetConnectionString("SqlServer");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddScoped<IUserRepository, DatabaseUserRepository>();
    builder.Logger.LogInformation("Using DatabaseUserRepository with SQL Server");
}
else
{
    builder.Services.AddScoped<IUserRepository, MockUserRepository>();
    builder.Logger.LogInformation("Using MockUserRepository");
}

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register data services
builder.Services.AddScoped<IDataService, DataService>();

// Configure Polly for HTTP client resilience
builder.Services.AddHttpClient("ApiClient")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generic Data Platform API v1"));
}

// Add request logging
app.UseRequestLogging();

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

// Add health checks
app.UseHealthChecks();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    app.Logger.LogInformation("Starting API Service");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "API Service terminated unexpectedly");
    return 1;
}
finally
{
    Serilog.Log.CloseAndFlush();
}

// Polly policy methods
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // HttpRequestException, 5XX and 408
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // 429
        .WaitAndRetryAsync(
            retryCount: 3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                context.GetLogger()?.LogWarning("Delaying for {delay}ms, then making retry {retry}.",
                    timespan.TotalMilliseconds, retryAttempt);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, timespan, context) =>
            {
                context.GetLogger()?.LogWarning("Circuit breaker opened for {duration}s due to: {outcome}.",
                    timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            },
            onReset: context =>
            {
                context.GetLogger()?.LogInformation("Circuit breaker reset.");
            },
            onHalfOpen: () =>
            {
                // Called when the circuit transitions to half-open state
            });
}
