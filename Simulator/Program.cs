using System.Net;
using System.Net.Sockets;
using Shared;

AppLogging.Initialise();


//NB, write to port 50000,
//our local agent will listen on this port and process the data as required
var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 50000);

listener.Start();

var logger = Serilog.Log.ForContext<Program>();

logger.Information("Legacy simulator listening on 127.0.0.1:50000");

while (true)
{
    using var client = await listener.AcceptTcpClientAsync();
    
    logger.Information($"Client connected: {client.Client.RemoteEndPoint}");
    
    await using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

    try
    {
        for (var i = 0; i < 1000; i++)
        {
            var hr = 60 + Random.Shared.Next(0, 40);
            var spo2 = 95 + Random.Shared.Next(0, 4);
            await writer.WriteLineAsync($"PID|P{i}|HR={hr}|SPO2={spo2}");
            logger.Information($"PID|P{i}|HR={hr}|SPO2={spo2}");
            await Task.Delay(800);
        }
    }
    catch (IOException)
    {
        // eks nie veronderstel om exceptions te kry nie
    }
}