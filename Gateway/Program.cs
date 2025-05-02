using Control;
using Gateway.Internal;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Shared;

AppLogging.Initialise();

var builder = WebApplication.CreateBuilder(args);

// use serilog
builder.Host.UseSerilog();

// we'll use our logger service from the Shared project
builder.Services.AddSharedKernel();

builder.Services.AddSingleton(sp =>
{
    // polling agent will communicate over port 6001, we need to configure this going forward
    var channel = GrpcChannel.ForAddress("http://localhost:6001",new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        }
    });
    return new CommandChannel.CommandChannelClient(channel);
});

builder.Services.AddGrpc();


builder.WebHost.ConfigureKestrel(opts =>
{
    // port 5130 and plain http for our web socket connections, this needs to be secured going forward
    opts.ListenLocalhost(5130, lo => lo.Protocols = HttpProtocols.Http1);
    
    // port 5131 with HTTP/2 for our gRPC 
    opts.ListenLocalhost(5131, lo =>
    {
        lo.Protocols = HttpProtocols.Http1AndHttp2;
        lo.UseHttps();
    });
});

var app = builder.Build();

// Enable WebSockets for data-plane
app.UseWebSockets();

// map the gRPC
app.MapGrpcService<CommandChannelService>();

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