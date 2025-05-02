using System.Text.Json;
using RabbitMQ.Client;
using Serilog;

namespace Gateway.Internal;

internal static class Bus
{
    // we need to connect using the same setup as in EventBridge
    private static readonly ConnectionFactory Factory = new()
    {
        HostName = "127.0.0.1",
        Port     = 5672,
        UserName = "guest",
        Password = "guest",
        DispatchConsumersAsync = true,
        RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
    };
    
    // Method to publish messages on the bus, they can then be intercepted by EventBridge which is bound to the queue via
    // the specific exchange and routingkey
    public static void Publish<T>(T message)
    {
        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            
            using var conn    = Factory.CreateConnection();
            
            using var channel = conn.CreateModel();
            
            channel.ExchangeDeclare("patient.events", ExchangeType.Topic, durable: true);
            
            // we use a clinic wildarc in EventBridge so 
            channel.BasicPublish("patient.events", routingKey: "clinic.patientEvent",mandatory: false, body: body);
            
            Log.Information("Published event to patient.events");
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to publish event");
            
            throw;
        }

    }
}