using System;
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
                // 🎯 Log via ILogger (va automaticamente in 'traces' con il provider configurato)
                _logger.LogInformation("Ricevuto ordine {OrderId}", order.OrderId);
                
                // 🎯 Trace esplicito via TelemetryClient (va direttamente in 'traces')
                _telemetryClient.TrackTrace($"Processing order: {order.OrderId}");
                
                await _publisher.PublishOrderAsync(order);
                
                // 🎯 Log di successo (va in 'traces')
                _logger.LogInformation("Ordine pubblicato con successo su ServiceBus. OrderId: {OrderId}", order.OrderId);
                
                // 🎯 Trace di successo esplicito
                _telemetryClient.TrackTrace($"Order {order.OrderId} successfully published to ServiceBus");
                
                return Ok(new { message = "Ordine ricevuto e pubblicato", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                // 🎯 Log con eccezione (va in 'exceptions')
                _logger.LogError(ex, "Errore durante la pubblicazione dell'ordine {OrderId}", order.OrderId);
                
                // 🎯 Exception esplicita via TelemetryClient (va in 'exceptions')
                _telemetryClient.TrackException(ex);
                
                return BadRequest(new { 
                    error = "Errore durante la pubblicazione", 
                    message = ex.Message,
                    orderId = order.OrderId 
                });
            }
        }
    }
}