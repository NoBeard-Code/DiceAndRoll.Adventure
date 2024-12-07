using DiceAndRoll.Adventure.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceAndRoll.Adventure.ConsoleApp.Configuration;

internal static class IHostBuilderExtensions
{
    internal static void Configure(this IHostBuilder builder)
    {
        if (AspireUtils.IsAspireConfigured())
        {
            builder.AddServiceDefaults();
        }

        builder.UseOrleansClient(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddFilter("Orleans", LogLevel.None);
        });
    }
}
