using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Shared;

// Logger class
public class AppLogging
{
    public static void Initialise()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();
    }
}