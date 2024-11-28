using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.UnitTests.Fixtures;
using Orleans.TestingHost;
using System.Diagnostics;

namespace DiceAndRoll.Adventure.UnitTests;

public class DashboardTests
{
    [Theory]
    [InlineData(25000)]
    public async Task TestClientConnections(int numberOfClients)
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<SiloConfigurationWithDashboard>()
            .Build();

        cluster.Deploy();

        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:8080",
            UseShellExecute = true
        });

        var clients = new List<IClusterClient>();

        for (int i = 0; i < numberOfClients; i++)
        {
            var client = cluster.Client;
            clients.Add(client);
        }

        var tasks = new List<Task>();
        foreach (var client in clients)
        {
            tasks.Add(Task.Run(async () =>
            {
                var grain = client.GetGrain<IPlayerGrain>(Guid.NewGuid());
                var result = await grain.Play("nothing");
            }));
        }

        await Task.WhenAll(tasks);

        Thread.Sleep(2 * 60 * 1000);
    }
}
