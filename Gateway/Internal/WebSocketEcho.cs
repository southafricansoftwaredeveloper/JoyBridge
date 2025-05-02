using System.Net.WebSockets;
using System.Text.Json;
using Contracts.Messages;

namespace Gateway.Internal;

// Websocket helper class, we provide the existing connection, this will then recieve data from the polling agent and we can 
// publish it using Bus.Publish, EventBridge will receive it and save it to the db
internal static class WebSocketEcho
{
    
    public static async Task Run(WebSocket ws, CancellationToken token)
    {
        var log = Serilog.Log.Logger;
        
        var buffer = new byte[8 * 1024];

        try
        {
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, token);

                var slice = buffer.AsMemory()[..result.Count];

                var patientEvent = JsonSerializer.Deserialize<PatientEvent>(slice.Span)!;

                Bus.Publish(patientEvent);    
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            log.Warning(ex,"Websocket connection closed unexpectedly");
        }

    }
}