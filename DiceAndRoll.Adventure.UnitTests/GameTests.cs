using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.UnitTests.Fixtures;
using Orleans.TestingHost;
using System.Reflection;

namespace DiceAndRoll.Adventure.UnitTests;

public class GameTests
{
    [Fact]
    public async Task TestGame_ConfigureAndStart()
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfiguration>()
            .Build();

        cluster.Deploy();

        var game = await ConfigureGameAsync(cluster);

        await game.End();

        cluster.StopAllSilos();
    }

    [Fact]
    public async Task TestGame_SinglePlayer()
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfiguration>()
            .Build();

        cluster.Deploy();

        var game = await ConfigureGameAsync(cluster);

        var directory = cluster.GrainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);

        var character = (await directory.GetCharacters()).First();
        
        var client = cluster.Client;

        var name = Guid.NewGuid();
        var player = client.GetGrain<IPlayerGrain>(name);

        await player.SetCharacter(name.ToString(), character!);

        await game.Join(player);

        await player.Die();

        Assert.True(await player.AmIDead());

        await game.End();

        cluster.StopAllSilos();
    }

    [Theory]
    [InlineData(100)]
    public async Task TestGame_BattleRoyale(int numberOfPlayers)
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<SiloConfigurationWithDashboard>()
            .Build();

        cluster.Deploy();

        // use real campaign map for this test
        var game = await ConfigureGameAsync(cluster);

        var directory = cluster.GrainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);

        var character = (await directory.GetCharacters()).First();

        var players = new List<IPlayerGrain>();

        for (int i = 0; i < numberOfPlayers; i++)
        {
            var client = cluster.Client;

            var name = Guid.NewGuid();
            var player = client.GetGrain<IPlayerGrain>(name);

            players.Add(player);

            Thread.Sleep(200); // players join every 200+ ms

            await player.SetCharacter(name.ToString(), character!);

            await game.Join(player, true);
        }

        var remainingPlayers = players.Count;

        // randomize player's actions for creating a dispersed environment
        while (remainingPlayers > 0)
        {
            foreach (var player in players)
            {
                var isDead = Task.Run(async () => await SimulatePlayerMove(player));

                if (await isDead)
                {
                    remainingPlayers--;
                }
            }
        }

        await game.End();

        cluster.StopAllSilos();
    }

    private static async Task<IGameGrain> ConfigureGameAsync(TestCluster cluster)
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var campaignFile = Path.Combine(path, "Data/campaign.json");

        var game = cluster.GrainFactory.GetGrain<IGameGrain>(Guid.NewGuid());
        await game.ConfigureAndStart(campaignFile);
        return game;
    }

    private static async Task<bool> SimulatePlayerMove(IPlayerGrain player)
    {
        // check if alive still
        if (await player.AmIDead())
        {
            // we're done for quit game
            return true;
        }

        Thread.Sleep(Random.Shared.Next(0, 2000));

        await player.Look();

        var others = await player.FindOthers();

        // if alone - move forward on the map
        if (others == null || others.Count == 0)
        {
            var directions = await player.FindDirections();

            if (directions is null || directions.Count == 0)
            {
                // we're stuck somewhere, wait to be killed :(
            }
            else
            {
                // move to a random location
                var choices = directions.Keys.ToList();                
                var choice = choices[Random.Shared.Next(0, choices.Count - 1)];
                await player.GoTo(choice);
            }
        }
        else
        {
            // try to kill first enemy!
            await player.Kill(others[0]);
        }

        return false;
    }
}
