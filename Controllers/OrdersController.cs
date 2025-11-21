using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Diagnostics;
using MonitoringPOC.Utils;
using MonitoringPOC.Models;

namespace MonitoringPOC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IServiceBusPublisher _publisher;
        private readonly ILogger<OrdersController> _logger;
        private readonly TelemetryClient _telemetryClient;

        public OrdersController(IServiceBusPublisher publisher, ILogger<OrdersController> logger, TelemetryClient telemetryClient)
        {
            _publisher = publisher;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("OrdersController GET chiamato");
            
            return Ok(new { 
                message = "OrdersController funziona!", 
                timestamp = DateTime.UtcNow,
                serviceBusStatus = "ServiceBus configurato",
                publisherType = _publisher.GetType().Name
            });
        }

        [HttpPost]
        public async Task<IActionResult> RiceviOrdine([FromBody] OrderDto order)
        {
            try
            {
                if (order.ItemType == "FAIL")
                {
                    throw new Exception("Errore simulato per demo");
                }
                // 🎯 Log via ILogger (va automaticamente in 'traces' con il provider configurato)
                //_logger.LogInformation("Ricevuto ordine {OrderId}", order.OrderId);
                
                // 🎯 Trace esplicito via TelemetryClient (va direttamente in 'traces')
                _telemetryClient.TrackTrace($"Processing order: {order.OrderId}");
                
                await _publisher.PublishOrderAsync(order);
                
                // 🎯 Log di successo (va in 'traces')
                //_logger.LogInformation("Ordine pubblicato con successo su ServiceBus. OrderId: {OrderId}", order.OrderId);
                
                // 🎯 Trace di successo esplicito
                _telemetryClient.TrackTrace($"Order {order.OrderId} successfully published to ServiceBus");
                
                return Ok(new { message = "Ordine ricevuto e pubblicato", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                // 🎯 Log con eccezione (va in 'exceptions')
                //_logger.LogError(ex, "Errore durante la pubblicazione dell'ordine {OrderId}", order.OrderId);
                
                // 🎯 Exception esplicita via TelemetryClient (va in 'exceptions')
                _telemetryClient.TrackException(ex);
                
                return BadRequest(new { 
                    error = "Errore durante la pubblicazione", 
                    message = ex.Message,
                    orderId = order.OrderId 
                });
            }
        }

        [HttpPost("loadtest")]
        public async Task<IActionResult> SimulaCaricoPesante()
        {
            var stopwatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString("N")[..8];
            
            try
            {
                // Genera 10 ordini mock
                var ordini = GeneraOrdiniMock(10, batchId);
                
                // Lista per tracciare i risultati
                var risultati = new List<Task<(bool Success, string OrderId, string Error)>>();

                // Esegui tutti gli ordini in parallelo
                foreach (var ordine in ordini)
                {
                    risultati.Add(ProcessaSingoloOrdine(ordine));
                }

                // Aspetta che tutti completino
                var esiti = await Task.WhenAll(risultati);

                stopwatch.Stop();

                // Analizza risultati
                var successi = esiti.Count(e => e.Success);
                var fallimenti = esiti.Count(e => !e.Success);
                
                _telemetryClient.TrackTrace($"✅ Simulazione completata - BatchId: {batchId}, Successi: {successi}, Fallimenti: {fallimenti}, Tempo: {stopwatch.ElapsedMilliseconds}ms");
                //_logger.LogInformation("Simulazione completata - BatchId: {BatchId}, Successi: {Successi}, Fallimenti: {Fallimenti}, Tempo: {Tempo}ms", 
                    //batchId, successi, fallimenti, stopwatch.ElapsedMilliseconds);

                return Ok(new 
                { 
                    batchId = batchId,
                    totaleOrdini = ordini.Count,
                    successi = successi,
                    fallimenti = fallimenti,
                    tempoTotaleMs = stopwatch.ElapsedMilliseconds,
                    ordiniProcessati = esiti.Select(e => new 
                    { 
                        orderId = e.OrderId, 
                        success = e.Success, 
                        error = e.Error 
                    })
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _telemetryClient.TrackException(ex);
                _logger.LogError(ex, "Errore durante la simulazione del carico pesante - BatchId: {BatchId}", batchId);
                
                return BadRequest(new 
                { 
                    error = "Errore durante la simulazione del carico pesante",
                    batchId = batchId,
                    message = ex.Message,
                    tempoMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        private List<OrderDto> GeneraOrdiniMock(int numero, string batchId)
        {
            var ordini = new List<OrderDto>();
            var destinazioni = new[] { "Milano", "Roma", "Napoli", "Torino", "Palermo", "Genova", "Bologna", "Firenze", "Bari", "Catania" };
            var tipologie = new[] { "Electronics", "Clothing", "Food", "Books", "Sports" };
            var random = new Random();

            for (int i = 1; i <= numero; i++)
            {
                ordini.Add(new OrderDto
                {
                    OrderId = $"{batchId}-{i:D3}",
                    Destination = destinazioni[random.Next(destinazioni.Length)],
                    ItemType = tipologie[random.Next(tipologie.Length)],
                    WeightKg = Math.Round(random.NextDouble() * 50 + 1, 2),
                    Priority = random.Next(1, 6).ToString()
                });
            }

            return ordini;
        }

        private async Task<(bool Success, string OrderId, string Error)> ProcessaSingoloOrdine(OrderDto ordine)
        {
            try
            {
                await _publisher.PublishOrderAsync(ordine);
                
                _telemetryClient.TrackTrace($"✅ Ordine pubblicato: {ordine.OrderId}");
                
                return (true, ordine.OrderId, null);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                _logger.LogError(ex, "Errore nell'elaborazione dell'ordine {OrderId}", ordine.OrderId);
                
                return (false, ordine.OrderId, ex.Message);
            }
        }
    }
}