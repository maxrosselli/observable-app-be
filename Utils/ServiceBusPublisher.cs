using System;
using System.Threading.Tasks;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MonitoringPOC.Models;

namespace MonitoringPOC.Utils
{
    public interface IServiceBusPublisher
    {
        Task PublishOrderAsync(OrderDto order);
    }

    public class ServiceBusPublisher : IServiceBusPublisher
    {
        private readonly ServiceBusSender _sender;

        public ServiceBusPublisher(ServiceBusClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client), "ServiceBusClient cannot be null");
                
            _sender = client.CreateSender("observablequeue");
        }

        public async Task PublishOrderAsync(OrderDto order)
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(order));
            await _sender.SendMessageAsync(message);
        }
    }

    // Mock publisher per sviluppo locale
    public class MockServiceBusPublisher : IServiceBusPublisher
    {
        public Task PublishOrderAsync(OrderDto order)
        {
            // Mock - non invia realmente il messaggio
            Console.WriteLine($"[MOCK] Publishing order: {JsonSerializer.Serialize(order)}");
            return Task.CompletedTask;
        }
    }
}
