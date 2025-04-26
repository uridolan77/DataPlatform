using System.Text;
using System.Net;
using System.Net.Http;
using GenericDataPlatform.API.Middleware;
using GenericDataPlatform.API.Models.Auth;
using GenericDataPlatform.API.Repositories;
using GenericDataPlatform.API.Services.Auth;
using GenericDataPlatform.API.Services.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add HTTP client factory
builder.Services.AddHttpClient();

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secret = jwtSettings["Secret"];
var key = Encoding.ASCII.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
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
builder.Services.AddScoped<IUserRepository, MockUserRepository>();
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

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

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
