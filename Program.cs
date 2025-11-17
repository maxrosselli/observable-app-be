using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using MonitoringPOC.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();

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

// 🔹 Aggiungi Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();         // 1️⃣

app.UseAuthorization();   // 2️⃣

app.MapControllers();     // 3️⃣

app.UseSwagger();         // 4️⃣
app.UseSwaggerUI();       // 5️⃣

app.Run();