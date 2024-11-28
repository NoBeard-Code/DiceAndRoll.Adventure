using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.TestingHost;

namespace DiceAndRoll.Adventure.UnitTests;

public class ClientTests
{
    private class TestSiloConfigurations : ISiloConfigurator
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

    [Fact]
    public async Task TestSingleGrain()
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfigurations>()
            .Build();

        cluster.Deploy();

        var grain = cluster.GrainFactory.GetGrain<IPlayerGrain>(Guid.NewGuid());
        var result = await grain.Play("nothing");

        cluster.StopAllSilos();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    [InlineData(1000000)]
    public async Task TestServerLoad(int numberOfGrains)
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfigurations>()
            .Build();

        cluster.Deploy();

        for (int i = 0; i < numberOfGrains; i++)
        {
            var grain = cluster.GrainFactory.GetGrain<IPlayerGrain>(Guid.NewGuid());
            var result = await grain.Play("nothing");
        }

        cluster.StopAllSilos();
    }

    [Theory]
    [InlineData(10)]
    public async Task TestMultipleClientConnections(int numberOfClients)
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.Build();
        cluster.Deploy();

        var clients = new List<IClusterClient>();

        for (int i = 0; i < numberOfClients; i++)
        {
            var host = Host.CreateDefaultBuilder()
                .UseOrleansClient(clientBuilder =>
                {
                    clientBuilder.UseLocalhostClustering();
                })
                .Build();

            await host.StartAsync();
            var client = host.Services.GetRequiredService<IClusterClient>();
            clients.Add(client);
        }

        var tasks = new List<Task>();
        foreach (var client in clients)
        {
            tasks.Add(Task.Run(async () =>
            {
                var grain = client.GetGrain<IPlayerGrain>(Guid.NewGuid());
                await grain.GetCharacters();
            }));
        }

        await Task.WhenAll(tasks);

        cluster.StopAllSilos();
    }
}
