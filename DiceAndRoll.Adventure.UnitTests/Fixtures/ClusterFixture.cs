using Orleans.TestingHost;

namespace DiceAndRoll.Adventure.UnitTests.Fixtures;

public sealed class ClusterFixture : IDisposable
{
    private readonly TestCluster _cluster;

    public ClusterFixture()
    {
        _cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfiguration>()
            .Build();

        _cluster.Deploy();
    }

    public IGrainFactory GrainFactory => _cluster.GrainFactory;

    public IClusterClient GetClient() => _cluster.Client;

    public void Dispose()
    {
        _cluster.StopAllSilos();
    }
}
