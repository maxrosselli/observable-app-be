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
            return Ok(new { message = "OrdersController funziona!", timestamp = DateTime.UtcNow });
        }

        [HttpPost]
        public async Task<IActionResult> RiceviOrdine([FromBody] OrderDto order)
        {
            _logger.LogInformation("Ricevuto ordine {OrderId}", order.OrderId);
            await _publisher.PublishOrderAsync(order);
            return Ok();
        }
    }
}