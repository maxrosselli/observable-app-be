using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Azure.Messaging.ServiceBus;
using MonitoringPOC.Utils;
using MonitoringPOC.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Application Insights - Telemetry + Logging Provider
builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddApplicationInsights();

// Configurazione Azure Service Bus
builder.Services.AddSingleton<ServiceBusClient>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("ServiceBus connection string non trovata");
    }
    return new ServiceBusClient(connectionString);
});

// Registrazione del publisher ServiceBus
builder.Services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

// 🔹 Health Checks Configuration
var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
builder.Services.AddHealthChecks()
    .AddCheck<ServiceBusHealthCheck>("servicebus", HealthStatus.Unhealthy, new[] { "servicebus", "azure" })
    .AddCheck("application-insights", () =>
    {
        // Verifica che Application Insights sia configurato
        var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        return !string.IsNullOrEmpty(aiConnectionString) 
            ? HealthCheckResult.Healthy("Application Insights configurato") 
            : HealthCheckResult.Degraded("Application Insights non configurato");
    });

// Registra il ServiceBusHealthCheck con la connection string
builder.Services.AddSingleton<ServiceBusHealthCheck>(provider => 
    new ServiceBusHealthCheck(serviceBusConnectionString));

// 🔹 Aggiungi Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();         // 1️⃣

app.UseAuthorization();   // 2️⃣

app.MapControllers();     // 3️⃣

// 🔹 Health Checks Endpoints
// Endpoint semplice (solo status)
app.MapHealthChecks("/health");

// Endpoint dettagliato (con tutti i dettagli dei componenti)
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            timestamp = DateTime.UtcNow,
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.ToString(),
                    description = kvp.Value.Description,
                    exception = kvp.Value.Exception?.Message,
                    data = kvp.Value.Data
                })
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        }));
    }
});

// Endpoint per singoli componenti
app.MapHealthChecks("/health/servicebus", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("servicebus")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Name == "servicebus"
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "application-insights"
});  // 4️⃣

app.UseSwagger();         // 5️⃣
app.UseSwaggerUI();       // 6️⃣

app.Run();