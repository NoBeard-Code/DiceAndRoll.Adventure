using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.UnitTests.Fixtures;
using Orleans.TestingHost;

namespace DiceAndRoll.Adventure.UnitTests;

public class GameTests
{
    [Theory]
    [InlineData(5)]
    public async Task TestGame_BattleRoyale(int numberOfPlayers)
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TestSiloConfiguration>()
            .Build();

        cluster.Deploy();

        var players = new List<IPlayerGrain>();
        var characters = new List<CharacterInfo>();

        for (int i = 0; i < numberOfPlayers; i++)
        {
            var client = cluster.Client;

            var player = client.GetGrain<IPlayerGrain>(Guid.NewGuid());
            if (i == 0)
            {
                characters = await player.GetCharacters();
            }

            players.Add(player);
        }

        cluster.StopAllSilos();
    }
}
