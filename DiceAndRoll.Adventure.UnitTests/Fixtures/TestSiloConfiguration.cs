using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace DiceAndRoll.Adventure.UnitTests.Fixtures;

internal class TestSiloConfiguration : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage("game");
        siloBuilder.ConfigureServices(static services =>
        {
            services.AddSingleton<ICharacterService, FakeCharacterService>();
        });
    }
}