using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using PollingAgent;

Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((ctx, svc) =>
    {
        // Add a singleton of our worker class
        svc.AddSingleton<Worker>();
        svc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<Worker>());

        // add gRPC support
        svc.AddGrpc();
    })
    .ConfigureWebHostDefaults(web =>
    {
        // Listen on http://localhost:6001 using HTTP/2 (h2c, no TLS)
        web.UseKestrel(o =>
        {
            o.ListenLocalhost(6001, lo => lo.Protocols = HttpProtocols.Http2);
        });
        
        web.UseUrls("http://localhost:6001");

        web.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // 4️⃣ Expose the control-plane gRPC service
                endpoints.MapGrpcService<CommandChannelService>();
            });
        });
    })
    .Build()
    .Run();
