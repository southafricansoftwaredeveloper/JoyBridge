using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Gateway;

// Helper class to keep track of valid WebSockets, I'm dropping them due to not sending a close frame it seems,
// This is a temporary fix!
internal static class SocketPool
{
    private static readonly ConcurrentDictionary<Guid, WebSocket> Sockets = new();

    public static Guid Add(WebSocket ws)
    {
        var id = Guid.NewGuid();
        Sockets[id] = ws;
        return id;
    }

    public static void Remove(Guid id) => Sockets.TryRemove(id, out _);

    public static IEnumerable<WebSocket> OpenSockets() => Sockets.Values.Where(s => s.State == WebSocketState.Open);
}