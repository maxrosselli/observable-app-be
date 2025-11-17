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
        // Per sviluppo locale - usa una connection string di default o mock
        return null; // Temporaneo per debug
    }
    return new ServiceBusClient(connectionString);
});

// Registrazione del publisher ServiceBus
builder.Services.AddScoped<IServiceBusPublisher>(provider =>
{
    var client = provider.GetService<ServiceBusClient>();
    if (client == null)
    {
        // Mock publisher per sviluppo locale
        return new MockServiceBusPublisher();
    }
    return new ServiceBusPublisher(client);
});

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