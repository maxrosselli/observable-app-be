using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Diagnostics;

namespace MonitoringPOC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SampleController : ControllerBase
    {
        private readonly ILogger<SampleController> _logger;
        private readonly ServiceBusClient _serviceBusClient;

        public SampleController(ILogger<SampleController> logger, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            // Ottieni l'OperationId corrente da Application Insights
            var operationId = Activity.Current?.RootId ?? HttpContext.TraceIdentifier;
            
            _logger.LogInformation("Sample endpoint was called. OperationId: {OperationId}", operationId);

            await Task.Delay(100); // Simula elaborazione

            // Pubblicazione messaggio su Azure Service Bus
            try
            {
                _logger.LogInformation($"Inizio pubblicazione messaggio su ServiceBus. OperationId: {operationId}");
                
                const string queueName = "observablequeue"; // Nome della coda ServiceBus
                
                // Verifica che il ServiceBusClient sia inizializzato
                if (_serviceBusClient == null)
                {
                    _logger.LogError("ServiceBusClient è null! OperationId: {OperationId}", operationId);
                    throw new InvalidOperationException("ServiceBusClient non inizializzato");
                }
                
                _logger.LogInformation("ServiceBusClient inizializzato correttamente. Endpoint: {Endpoint}", _serviceBusClient.FullyQualifiedNamespace);
                
                // Creazione del messaggio con OperationId
                var messageData = new
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Message = "Messaggio inviato dall'endpoint GetAsync",
                    Source = "MonitoringPOC.SampleController",
                    OperationId = operationId, // Includi l'OperationId nel payload
                    TraceId = Activity.Current?.TraceId.ToString(),
                    SpanId = Activity.Current?.SpanId.ToString()
                };

                var messageBody = JsonSerializer.Serialize(messageData);
                var message = new ServiceBusMessage(messageBody);
                
                // Aggiungi l'OperationId anche nelle proprietà del messaggio ServiceBus
                message.ApplicationProperties["OperationId"] = operationId;
                message.ApplicationProperties["TraceId"] = Activity.Current?.TraceId.ToString();
                message.ApplicationProperties["SpanId"] = Activity.Current?.SpanId.ToString();
                message.ApplicationProperties["ParentId"] = Activity.Current?.Id;
                
                // Aggiungiamo proprietà al messaggio per identificarlo meglio
                message.Subject = "Sample Request";
                message.ContentType = "application/json";

                // Invio del messaggio
                await using var sender = _serviceBusClient.CreateSender(queueName);
                
                _logger.LogInformation("Tentativo di invio messaggio su ServiceBus. Queue: {QueueName}, OperationId: {OperationId}", queueName, operationId);
                
                // Invio con gestione del risultato
                try
                {
                    await sender.SendMessageAsync(message);
                    _logger.LogInformation("SendMessageAsync completato senza eccezioni. OperationId: {OperationId}", operationId);
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "Eccezione durante SendMessageAsync. OperationId: {OperationId}", operationId);
                    throw;
                }
                
                // Aspetta un momento per assicurarsi che il MessageId sia assegnato
                await Task.Delay(10);

                // Log dopo l'invio - ora MessageId dovrebbe essere valorizzato
                _logger.LogInformation("Messaggio pubblicato su ServiceBus. MessageId: {MessageId}, Queue: {QueueName}, OperationId: {OperationId}", 
                    message.MessageId ?? "NULL", queueName, operationId);
                    
                // Debug aggiuntivo - controlla tutte le proprietà del messaggio
                _logger.LogInformation("Debug messaggio - Subject: {Subject}, ContentType: {ContentType}, Size: {Size}", 
                    message.Subject, message.ContentType, message.Body.ToString().Length);
                    
                // Debug aggiuntivo
                if (string.IsNullOrEmpty(message.MessageId))
                {
                    _logger.LogWarning("ATTENZIONE: MessageId è null o vuoto dopo l'invio! OperationId: {OperationId}", operationId);
                    _logger.LogWarning("Altre proprietà - Subject: {Subject}, ContentType: {ContentType}", 
                        message.Subject, message.ContentType);
                }
            }
            catch (ServiceBusException sbEx)
            {
                _logger.LogError(sbEx, "Errore ServiceBus durante la pubblicazione del messaggio. Codice: {ErrorCode}, Motivo: {Reason}, OperationId: {OperationId}", 
                    sbEx.Reason, sbEx.Message, operationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico durante la pubblicazione del messaggio su ServiceBus. OperationId: {OperationId}", operationId);
            }

            return Ok(new
            {
                message = "Sample endpoint response",
                operationId = operationId
            });

        }

        [HttpGet("error")]
        public IActionResult GenerateError()
        {
            // Ottieni l'OperationId corrente da Application Insights
            var operationId = Activity.Current?.RootId ?? HttpContext.TraceIdentifier;
            
            _logger.LogError("Simulated error triggered. OperationId: {OperationId}", operationId);

            throw new InvalidOperationException($"This is a simulated exception for alert testing. OperationId: {operationId}");
        }
    }
}