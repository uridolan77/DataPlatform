using System;
using System.Text;
using GenericDataPlatform.Common.Logging;
using GenericDataPlatform.Common.Observability;
using GenericDataPlatform.Common.Observability.HealthChecks;
using GenericDataPlatform.Common.Security.Secrets;
using GenericDataPlatform.Gateway.Configuration;
using GenericDataPlatform.Gateway.Identity;
using GenericDataPlatform.Gateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Ocelot.Cache.CacheManager;
using Serilog;

// Configure Serilog
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilog("Gateway");

// Configure user secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add secret provider
builder.Services.AddSecretProvider(builder.Configuration);

// Add OpenTelemetry
builder.Services.AddOpenTelemetryServices(builder.Configuration, "Gateway", "1.0.0");

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration, "Gateway");

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

// Configure Identity Database
var connectionString = builder.Configuration.GetConnectionString("IdentityDatabase");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure IdentityServer
var identityServerBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    
    options.EmitStaticAudienceClaim = true;
})
.AddAspNetIdentity<ApplicationUser>()
.AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
})
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
    options.EnableTokenCleanup = true;
    options.TokenCleanupInterval = 3600; // seconds
});

// Get signing credential from secret provider
var secretProvider = builder.Services.BuildServiceProvider().GetRequiredService<ISecretProvider>();
var signingKey = secretProvider.GetSecretAsync("identityserver/signing-key").GetAwaiter().GetResult();

if (!string.IsNullOrEmpty(signingKey))
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    identityServerBuilder.AddSigningCredential(key, SecurityAlgorithms.HmacSha256);
}
else
{
    // For development only - not for production
    identityServerBuilder.AddDeveloperSigningCredential();
}

// Configure external authentication providers
if (builder.Environment.IsDevelopment())
{
    // Add Google authentication
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
        builder.Services.AddAuthentication()
            .AddGoogle("Google", options =>
            {
                options.SignInScheme = "Identity.External";
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
            });
    }
    
    // Add Microsoft authentication
    var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
    var microsoftClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    
    if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
    {
        builder.Services.AddAuthentication()
            .AddMicrosoftAccount("Microsoft", options =>
            {
                options.SignInScheme = "Identity.External";
                options.ClientId = microsoftClientId;
                options.ClientSecret = microsoftClientSecret;
            });
    }
}

// Configure JWT authentication for API Gateway
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

// Configure Ocelot
builder.Services.AddOcelot(builder.Configuration)
    .AddPolly()
    .AddCacheManager(x =>
    {
        x.WithDictionaryHandle();
    });

// Configure API Gateway services
builder.Services.AddScoped<IClientStore, ClientStore>();
builder.Services.AddScoped<IResourceStore, ResourceStore>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Add request logging
app.UseRequestLogging();

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Add health checks
app.UseHealthChecks();

app.UseHttpsRedirection();
app.UseCors();

// Use IdentityServer
app.UseIdentityServer();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Use Ocelot middleware
await app.UseOcelot();

try
{
    // Initialize database and seed data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await DatabaseInitializer.InitializeAsync(services);
    }

    app.Logger.LogInformation("Starting API Gateway");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "API Gateway terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
