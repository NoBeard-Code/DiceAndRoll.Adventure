using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace DiceAndRoll.Adventure.UnitTests.Fixtures;

public sealed class ClusterFixture : IDisposable
{
    private readonly TestCluster _cluster;

    public ClusterFixture()
    {
        _cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfigurations>()
            .Build();

        _cluster.Deploy();
    }

    public IGrainFactory GrainFactory => _cluster.GrainFactory;

    public void Dispose()
    {
        _cluster.StopAllSilos();
    }
}

internal class TestSiloConfigurations : ISiloConfigurator
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