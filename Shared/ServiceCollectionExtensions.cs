using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        // use Serilog as the single provider
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // we can in the future add more services here, currently we'll only make use of the logger
        return services;
    }
}