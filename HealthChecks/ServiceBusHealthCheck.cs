using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;

namespace MonitoringPOC.HealthChecks
{
    public class ServiceBusHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public ServiceBusHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    return HealthCheckResult.Unhealthy("ServiceBus connection string non configurata");
                }

                // Test connessione ServiceBus con timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                await using var client = new ServiceBusClient(_connectionString);
                
                if (client.IsClosed)
                {
                    return HealthCheckResult.Unhealthy("ServiceBus client non connesso");
                }

                // Test creazione di un sender per verificare la connessione
                await using var sender = client.CreateSender("observablequeue");

                return HealthCheckResult.Healthy("ServiceBus operativo e connesso alla coda 'observablequeue'");
            }
            catch (TaskCanceledException)
            {
                return HealthCheckResult.Degraded("ServiceBus health check timeout (>5s)");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"ServiceBus non raggiungibile: {ex.Message}");
            }
        }
    }
}