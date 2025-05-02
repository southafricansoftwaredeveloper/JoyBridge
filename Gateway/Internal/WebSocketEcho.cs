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

        var id = SocketPool.Add(ws);

        var buffer = new byte[8 * 1024];

        try
        {
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var slice = buffer.AsMemory()[..result.Count];

                // publish to Rabbit
                var patientEvent = JsonSerializer.Deserialize<PatientEvent>(slice.Span)!;

                Bus.Publish(patientEvent);

                // broadcast to every connected socket (including sender)
                foreach (var sock in SocketPool.OpenSockets())
                {
                    await sock.SendAsync(slice, WebSocketMessageType.Text, true, token);
                }
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            log.Warning(ex, "WS {Id} closed unexpectedly", id);
        }
        finally
        {
            SocketPool.Remove(id);

            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Gateway.API - WebSocketEcho - Closing connection", CancellationToken.None);
            }

            log.Information("WS {Id} closed (state {State})", id, ws.State);
        }
    }
}