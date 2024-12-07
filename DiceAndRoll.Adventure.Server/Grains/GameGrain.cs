using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class GameGrain(ICampaignService campaignService,
    [PersistentState("game", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<GameInfo> state) : Grain, IGameGrain
{
    private readonly List<IPlayerGrain> _players = [];
    private readonly List<LocationInfo> _locations = [];

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

        _locations.Clear();
        _locations.AddRange(locations);

        // Initialize the game world using the game data
        foreach (var location in locations)
        {
            await AddLocationAsync(location);
        }
    }

    private async Task<ILocationGrain> AddLocationAsync(LocationInfo location)
    {
        var grain = GrainFactory.GetGrain<ILocationGrain>(location.Name);
        await grain.Configure(location);
        return grain;
    }

    public Task End()
    {
        return Task.CompletedTask;
    }

    public async Task Join(IPlayerGrain player, bool randomLocation = false)
    {
        Console.WriteLine($"New player joined: {this.GetGrainId()}, {player.GetGrainId()}");

        // drop player into a random location?
        var startLocation = randomLocation ? _locations[Random.Shared.Next(0, _locations.Count - 1)] : _locations.First();

        _players.Add(player);

        await player.Appear(startLocation.Name);
    }

    public async Task Leave(IPlayerGrain player)
    {
        Console.WriteLine($"Player left: {this.GetGrainId()}, {player.GetGrainId()}");

        _players.Remove(player);

        await player.Die();
    }
}
