using System.Text.Json;
using Contracts.Messages;
using EventBridge;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared;

// init logging
AppLogging.Initialise();

var log = Log.ForContext("SourceContext", "EventBridge");

// configure rabbitmq, we want it to be asynchronous
var factory = new ConnectionFactory
{
    HostName  = "127.0.0.1",
    Port      = 5672,
    UserName  = "guest",
    Password  = "guest",
    DispatchConsumersAsync = true,
    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
};

IConnection conn = default!;
while (conn is null)
{
    try
    {
        log.Information("Trying RabbitMQ at {Host}:{Port}", factory.HostName, factory.Port);
        
        conn = factory.CreateConnection();
        
        log.Information("RabbitMQ connection established.");
    }
    catch (Exception ex)
    {
        log.Warning(ex, "RabbitMQ not ready – retry in 3 s");
        Thread.Sleep(TimeSpan.FromSeconds(3));
    }
}

// create our channel
using var channel = conn.CreateModel();
log.Information("RabbitMQ connected.");

channel.ExchangeDeclare("patient.events", ExchangeType.Topic, durable: true);

var queue = channel.QueueDeclare().QueueName;

// we need to have extchange and routing key configurable  
channel.QueueBind(queue, "patient.events", "clinic.*");

// init db
await using var db = new EventDb();

// create consumer to respond to polling agent
var consumer = new AsyncEventingBasicConsumer(channel);

// when we receive a messgae, deserialize and save to db
// Alternatively, we can in the future call additional services to respond to more complex data being recieved and perform additional checks
consumer.Received += async (_, ea) =>
{
    log.Information("Received message with routing key: {Key}", ea.RoutingKey);

    try
    {
        var ev = JsonSerializer.Deserialize<PatientEvent>(ea.Body.Span, (JsonSerializerOptions?)null);

        if (ev == null)
        {
            log.Warning("Received null or malformed PatientEvent");
            return;
        }

        db.Add(ev);
        await db.SaveChangesAsync();

        log.Information("Persisted event Id={Id} Patient={PatientId}", ev.Id, ev.PatientId);
    }
    catch (Exception ex)
    {
        log.Error(ex, "Failed to handle message");
    }
};

// start consuming from the queue
channel.BasicConsume(queue, autoAck: true, consumer);

log.Information("Consuming from queue {Queue}", queue);

// we'll run indefinitely, we can also setup a RPC service and repond to the same command from the thin client 
await Task.Delay(Timeout.Infinite);