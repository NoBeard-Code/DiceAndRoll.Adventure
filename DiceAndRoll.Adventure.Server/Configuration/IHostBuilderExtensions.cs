using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceAndRoll.Adventure.Server.Configuration;

public static class IHostBuilderExtensions
{
    public static void Configure(this IHostBuilder builder)
    {
        if (AspireUtils.IsAspireConfigured())
        {
            builder.AddServiceDefaults();
        }

        builder.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();
            siloBuilder.AddMemoryGrainStorage("game");
            siloBuilder.UseDashboard();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddFilter("Orleans", LogLevel.Information);
        });

        builder.ConfigureServices(services => {
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<ICampaignService, CampaignService>();
        });
    }
}
