using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class GameState
{
    public List<IPlayerGrain> Players { get; set; } = [];

    public List<ILocationGrain> Locations { get; set; } = [];
}

internal sealed class GameGrain(ICampaignService campaignService,
    [PersistentState("game", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<GameState> state) : Grain, IGameGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var directory = GrainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);
        await directory.RegisterGame(this.GetPrimaryKey());
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var directory = GrainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);
        await directory.UnregisterGame(this.GetPrimaryKey());
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task ConfigureAndStart(string campaignFile)
    {
        var campaign = await campaignService.LoadAsync(campaignFile);

        var locations = await campaignService.GetLocationsAsync();

        if (state.RecordExists)
            return;

        state.State = new GameState();

        // Initialize the game world using the game data
        foreach (var location in locations)
        {
            var grain = GrainFactory.GetGrain<ILocationGrain>(location.Name);
            await grain.Configure(location);
            state.State.Locations.Add(grain);
        }

        await state.WriteStateAsync();

        Console.WriteLine($"Game configured and started.");
    }

    public Task End()
    {
        Console.WriteLine($"Game ended.");

        return Task.CompletedTask;
    }

    public async Task Join(IPlayerGrain player, bool randomLocation = false)
    {
        Console.WriteLine($"New player joined: {this.GetGrainId()}, {player.GetGrainId()}");

        // drop player into a random location?
        var locations = state.State.Locations;

        var startLocation = randomLocation ? locations[Random.Shared.Next(0, locations.Count - 1)] : locations.First();

        state.State.Players.Add(player);

        await state.WriteStateAsync();

        await player.Appear(startLocation.GetPrimaryKeyString());
    }

    public async Task Leave(IPlayerGrain player)
    {
        Console.WriteLine($"Player left: {this.GetGrainId()}, {player.GetGrainId()}");

        state.State.Players.Remove(player);

        await state.WriteStateAsync();

        await player.Die();
    }
}
