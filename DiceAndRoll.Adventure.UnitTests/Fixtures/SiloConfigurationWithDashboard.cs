using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace DiceAndRoll.Adventure.UnitTests.Fixtures;

internal class SiloConfigurationWithDashboard : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage("game");
        siloBuilder.ConfigureServices(static services =>
        {
            services.AddSingleton<ICharacterService, FakeCharacterService>();
            services.AddSingleton<ICampaignService, CampaignService>();
        });
        siloBuilder.UseDashboard(options =>
        {
            options.HostSelf = true;
            options.Port = 8080;
        });
    }
}