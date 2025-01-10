using DiceAndRoll.Adventure.Server.Extensions;
using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared;
using DiceAndRoll.Adventure.Shared.Extensions;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceAndRoll.Adventure.Server.Configuration;

internal static class IHostBuilderExtensions
{
    internal static void Configure(this IHostBuilder builder)
    {
        if (AspireUtils.IsAspireConfigured())
        {
            builder.AddServiceDefaults();
        }

        builder.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();
            siloBuilder.AddMemoryGrainStorage(Constants.ORLEANS_GRAIN_STORAGE);
            siloBuilder.UseDashboard();
            siloBuilder.AddGrainExtension<IPublisherExtension<IGameLobby>, PublisherExtension<IGameLobby>>();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddFilter("Orleans", LogLevel.Information);
        });

        builder.ConfigureServices(services => {
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<IMonsterService, MonsterService>();
            services.AddSingleton<ICampaignService, CampaignService>();
        });
    }
}
