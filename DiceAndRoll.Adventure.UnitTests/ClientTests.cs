using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.UnitTests.Fixtures;

namespace DiceAndRoll.Adventure.UnitTests;

public class ClientTests(ClusterFixture cluster) : IClassFixture<ClusterFixture>
{
    [Fact]
    public async Task TestSingleClientConnection()
    {
        var client = cluster.GetClient();

        var grain = client.GetGrain<IPlayerGrain>(Guid.NewGuid());
        var result = await grain.Look();

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    public async Task TestClientConnections(int numberOfClients)
    {
        var clients = new List<IClusterClient>();

        for (int i = 0; i < numberOfClients; i++)
        {
            var client = cluster.GetClient();
            clients.Add(client);
        }

        var tasks = new List<Task>();
        foreach (var client in clients)
        {
            tasks.Add(Task.Run(async () =>
            {
                var grain = client.GetGrain<IPlayerGrain>(Guid.NewGuid());
                var result = await grain.Look();
            }));
        }

        await Task.WhenAll(tasks);
    }
}
