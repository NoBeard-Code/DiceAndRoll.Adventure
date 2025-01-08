using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class GameState
{
    public List<IPlayerGrain> Players { get; set; } = [];

    public List<ILocationGrain> Locations { get; set; } = [];
}

internal sealed class GameGrain(
    ICampaignService campaignService, IMonsterService monsterService,
    ILogger<GameGrain> logger,
    [PersistentState("game", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<GameState> state) : Grain, IGameGrain
{
    private readonly ObserverManager<IGameObserver> _observerManager = new(TimeSpan.FromMinutes(5), logger);

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
        await campaignService.LoadAsync(campaignFile);

        var locations = await campaignService.GetLocationsAsync();
        var monsters = await monsterService.GetMonstersAsync();

        if (state.RecordExists)
            return;

        state.State = new GameState();

        // Initialize the game world using the game data
        foreach (var location in locations)
        {
            var grain = GrainFactory.GetGrain<ILocationGrain>(location.Name);
            await grain.Configure(location, monsters);
            state.State.Locations.Add(grain);
        }

        await state.WriteStateAsync();

        Console.WriteLine($"Game configured and started.");
    }

    public async Task End()
    {
        await _observerManager.Notify(o => o.GameOver());
    }

    public async Task Join(IPlayerGrain player, bool randomLocation = false)
    {
        // drop player into a random location?
        var locations = state.State.Locations;

        var startLocation = randomLocation ? locations[Random.Shared.Next(0, locations.Count - 1)] : locations.First();

        state.State.Players.Add(player);

        await state.WriteStateAsync();

        await player.Appear(startLocation.GetPrimaryKeyString());

        await _observerManager.Notify(o => o.PlayerJoined(player.GetGrainId().ToString()));
    }

    public async Task Leave(IPlayerGrain player)
    {
        state.State.Players.Remove(player);

        await state.WriteStateAsync();

        await player.Die();

        await _observerManager.Notify(o => o.PlayerLeft(player.GetGrainId().ToString()));
    }

    public Task Subscribe(IGameObserver observer)
    {
        _observerManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IGameObserver observer)
    {
        _observerManager.Unsubscribe(observer);
        return Task.CompletedTask;
    }
}
