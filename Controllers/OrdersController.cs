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

        public OrdersController(IServiceBusPublisher publisher, ILogger<OrdersController> logger)
        {
            _publisher = publisher;
            _logger = logger;
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
                // Log informativo -> va in 'traces'
                _logger.LogInformation("Ricevuto ordine {OrderId}", order.OrderId);
                
                await _publisher.PublishOrderAsync(order);
                
                // Log di successo -> va in 'traces'
                _logger.LogInformation("Ordine pubblicato con successo su ServiceBus. OrderId: {OrderId}", order.OrderId);
                return Ok(new { message = "Ordine ricevuto e pubblicato", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                // Log con eccezione -> va in 'exceptions'
                _logger.LogError(ex, "Errore durante la pubblicazione dell'ordine {OrderId}", order.OrderId);
                return BadRequest(new { 
                    error = "Errore durante la pubblicazione", 
                    message = ex.Message,
                    orderId = order.OrderId 
                });
            }
        }
    }
}