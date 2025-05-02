using Gateway.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Shared;

AppLogging.Initialise();

var builder = WebApplication.CreateBuilder(args);

// use serilog
builder.Host.UseSerilog();

// we'll use our logger service from the Shared project
builder.Services.AddSharedKernel();

builder.WebHost.ConfigureKestrel(opts =>
{
    // port 5130 and plain http for our web socket connections, this needs to be secured going forward
    opts.ListenLocalhost(5130, lo => lo.Protocols = HttpProtocols.Http1);
});

var app = builder.Build();

// Enable WebSockets for data-plane
app.UseWebSockets();

// Map WebSocket endpoint
app.Map("/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    
    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    await WebSocketEcho.Run(ws, ctx.RequestAborted);
    
});

await app.RunAsync();