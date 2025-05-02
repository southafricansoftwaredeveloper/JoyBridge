using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using Contracts.Messages;
using Microsoft.Extensions.Options;
using Serilog;
using Websocket.Client;

namespace PollingAgent;

// Our polling agent is responsible for getting data, in this case from the simulator, we need to setup it up in the 
// future such that we can configure the listener through config, allowing us to specify the connection type, i.e TCP, or COM-interop 
public class Worker : BackgroundService
{
    private readonly TcpConnectionDetails _opt;
    private readonly ILogger<Worker> _log;
    
    // we need to ensure the value changes can be seen by different threads
    private volatile bool _paused;
    private static readonly WebsocketClient GatewayWs = new WebsocketClient(new Uri("ws://localhost:5130/ws")) { ReconnectTimeout = TimeSpan.FromSeconds(5) };

    public Worker(ILogger<Worker> log, IOptions<TcpConnectionDetails> opt)
    {
        (_log, _opt) = (log, opt.Value);

        if (!GatewayWs.IsStarted)
        {
            GatewayWs.Start();
            GatewayWs.ReconnectionHappened.Subscribe(_ => _log.LogInformation("PollingAgent WS reconnected"));
        }
    }


    public void Pause()
    {
        _paused = true;
        _log.LogInformation("Pausing polling");
    }

    public void Resume()
    {
        _paused = false;
        _log.LogInformation("Resuming polling");
    }
    
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        _log.LogInformation("Worker loop starting with Host={Host} Port={Port}", _opt.Host, _opt.Port);
        
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var tcp = new TcpClient();
                
                await tcp.ConnectAsync(_opt.Host, _opt.Port, stop);
                
                _log.LogInformation("Connected to {Host}:{Port}", _opt.Host, _opt.Port);

                using var reader = new StreamReader(tcp.GetStream());

                while (!stop.IsCancellationRequested && tcp.Connected)
                {

                    if (_paused)
                    {
                        continue;
                    }
                    
                    // we cannot read here prematurely as the Simulator might not be connected.. a little bit weird
                    var line = await reader.ReadLineAsync();

                    // null check data
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    
                    // parse data and null check
                    var ev   = ParseLegacyLine(line);

                    
                    if (ev == null)
                    {
                        continue;
                    }
                    
                    // serialize so we can send
                    var json = JsonSerializer.Serialize(ev);
                    
                    // we need to forward the data to our thin client
                    if (GatewayWs.IsRunning)
                    {
                        await GatewayWs.SendInstant(json);
                    }
                    else
                    {
                        _log.LogWarning("WebSocket is not open; skipping send");
                    }

                    _log.LogInformation("Forwarded event for {Patient}", ev.PatientId);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error, retry in 3 s");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stop);
        }
    }
   
    
    // Example HL7: "PID|123|...|HR=78|SPO2=97"
    // We need to parse the data recieved via TCP from the simulator, I will assume that we can get corrupted data, for 
    // now we'll just return null but we should handle it gracefully and propagate it somehow throughout the system
    // When data is corrupted, it's very important to notify the relevant parties as clients expected outcomes won't be met
    private static PatientEvent? ParseLegacyLine(string raw)
    {
        try
        {
            
            var parts = raw.Split('|');
        
            return new PatientEvent
            {
                PatientId = parts[1],
                DeviceId  = "Legacy",
                Timestamp = DateTimeOffset.UtcNow,
                HeartRate = double.Parse(parts[^2][3..]),
                SpO2      = double.Parse(parts[^1][5..])
            };
        }
        catch
        {
            Log.Warning("Malformed data received");
            return null;
        }
        
    }
}
