using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

public sealed class LocationGrain(
    [PersistentState("location", "game")] IPersistentState<LocationInfo> state, 
    ILogger<LocationGrain> logger) 
    : Grain, ILocationGrain
{
    private readonly ObserverManager<ILocationObserver> _observerManager 
        = new(TimeSpan.FromSeconds(2), logger);

    private readonly Dictionary<string, ILocationGrain> _connections = [];
    private readonly List<string> _players = [];

    // Clients call this to subscribe.
    public Task Subscribe(ILocationObserver observer)
    {
        _observerManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    //Clients use this to unsubscribe and no longer receive messages.
    public Task Unsubscribe(ILocationObserver observer)
    {
        _observerManager.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    public async Task Configure(LocationInfo info)
    {
        if (state.RecordExists) 
            return; 

        state.State = info;
        await state.WriteStateAsync();

        foreach (var connection in info.ConnectsTo)
        {
            var grain = GrainFactory.GetGrain<ILocationGrain>(connection);
            if (grain != null)
            {
                _connections.Add(connection, grain);               
            }
        }
    }

    public Task<LocationInfo?> GetInfo()
    {
        return Task.FromResult((state.RecordExists) ? state.State : null);
    }

    public Task Enter(string playerName)
    {
        _players.RemoveAll(x => x == playerName);
        _players.Add(playerName);

        _observerManager.Notify(x => x.PlayerEnteredLocation(playerName));

        return Task.CompletedTask;
    }

    public Task Exit(string playerName)
    {
        _players.RemoveAll(x => x == playerName);
        return Task.CompletedTask;
    }

    public async Task<string> Describe(string playerName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[purple]{state.State.Description}[/]");
        sb.AppendLine();

        var others = _players.Where(x => x != playerName); 
        if (others.Any())
        {
            sb.AppendLine("[maroon]Beware! These guys are in the room with you:[/]");
            foreach (var other in others)
            {
                sb.Append("  ").AppendLine($"[olive]{other}[/]");
            }
            sb.AppendLine();
        }

        sb.AppendLine("[grey]You can go in the following directions:[/]");
        foreach (var location in _connections.Values)
        {
            var info = await location.GetInfo();
            if (info != null)
            {
                sb.Append("  ").AppendLine($"[silver]{info.Title}[/]");
            }
        }

        return sb.ToString();
    }

    public Task<ILocationGrain?> ExitTo(string destination)
    {
        return Task.FromResult(
            _connections.ContainsKey(destination) ? _connections[destination] : null
        );
    }

    public Task<Dictionary<string, ILocationGrain>> GetConnections()
    {
        return Task.FromResult(_connections);
    }
}
