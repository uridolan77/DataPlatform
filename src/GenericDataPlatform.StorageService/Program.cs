using System;
using GenericDataPlatform.Common.Clients;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Common.Security;
using GenericDataPlatform.Protos;
using GenericDataPlatform.StorageService.Middleware;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Get logger for resilience policies
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Add resilience policies
builder.Services.AddSingleton(HttpClientResiliencePolicies.GetCombinedPolicy(logger));

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
    options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
});

// Add certificate manager for secure gRPC
builder.Services.AddSingleton<ICertificateManager, CertificateManager>();

// Add resilient gRPC client factory
builder.Services.AddResilientGrpcClientFactory();

// Add gRPC client interceptors
builder.Services.AddGrpcClientInterceptors();

// Register the StorageService gRPC client
var storageServiceUrl = builder.Configuration["Services:StorageService:Url"] ?? "https://localhost:5207";
builder.Services.AddSingleton(provider =>
{
    var factory = provider.GetRequiredService<GrpcClientFactory>();
    var channel = factory.CreateChannel(storageServiceUrl);
    return new StorageService.StorageServiceClient(channel);
});

// Register the resilient storage service client
builder.Services.AddSingleton<ResilientStorageServiceClient>();

// Configure gRPC server interceptors
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcErrorInterceptor>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
