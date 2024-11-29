using DiceAndRoll.Adventure.Server;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.UnitTests.Fixtures;
using Orleans.TestingHost;
using System.Reflection;

namespace DiceAndRoll.Adventure.UnitTests;

public class GameTests
{
    [Theory]
    [InlineData(25)]
    public async Task TestGame_BattleRoyale(int numberOfPlayers)
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<SiloConfigurationWithDashboard>()
            .Build();

        cluster.Deploy();

        // use real campaign map for this test
        await UseCampaignMap(cluster);

        var players = new List<IPlayerGrain>();

        for (int i = 0; i < numberOfPlayers; i++)
        {
            var client = cluster.Client;

            var name = Guid.NewGuid();
            CharacterInfo character = null;

            var player = client.GetGrain<IPlayerGrain>(name);
            if (i == 0)
            {
                character = (await player.GetCharacters()).First();
            }

            players.Add(player);

            Thread.Sleep(200); // players join every 200+ ms

            await player.JoinGame(name.ToString(), character!);
            await player.Look();
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

        cluster.StopAllSilos();
    }

    private async Task UseCampaignMap(TestCluster cluster)
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var campaignFile = Path.Combine(path, "Data/campaign.json");

        var game = new Game(cluster.GrainFactory);
        await game.StartAsync(campaignFile);
    }

    private static async Task<bool> SimulatePlayerMove(IPlayerGrain player)
    {
        // check if alive still
        if (await player.AmIDead())
        {
            // we're done for quit game
            return true;
        }

        // wait for some time, look around, and if alone - move forward on the map,
        // otherwise, try to make some kill.

        Thread.Sleep(Random.Shared.Next(0, 2000));

        await player.Look();

        var others = await player.FindOthers();

        if (others == null || others.Count == 0)
        {
            var directions = await player.FindDirections();

            if (directions is null || directions.Count == 0)
            {
                // we're stuck somewhere, wait to be killed :(
            }
            else
            {
                var choice = directions.First().Key;
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
