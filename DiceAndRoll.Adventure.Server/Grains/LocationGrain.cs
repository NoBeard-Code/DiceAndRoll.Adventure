using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

public sealed class LocationGrain(
    [PersistentState("location", "game")] IPersistentState<LocationInfo> state) 
    : Grain, ILocationGrain
{
    private readonly Dictionary<string, ILocationGrain> _connections = [];
    private readonly List<string> _players = [];

    public Task Configure(LocationInfo info)
    {
        if (state.RecordExists) 
            return Task.CompletedTask; 

        state.State = info;
        state.WriteStateAsync();

        foreach (var connection in info.ConnectsTo)
        {
            var grain = GrainFactory.GetGrain<ILocationGrain>(connection);
            if (grain != null)
            {
                _connections.Add(connection, grain);
            }
        }

        return Task.CompletedTask;
    }

    public Task Enter(string playerName)
    {
        _players.RemoveAll(x => x == playerName);
        _players.Add(playerName);
        return Task.CompletedTask;
    }

    public Task Exit(string playerName)
    {
        _players.RemoveAll(x => x == playerName);
        return Task.CompletedTask;
    }

    public Task<string> Describe(string playerName)
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
        foreach (var location in _connections.Keys)
        {
            sb.Append("  ").AppendLine($"[silver]{location}[/]");
        }

        return Task.FromResult(sb.ToString());
    }

    public Task<ILocationGrain?> ExitTo(string destination)
    {
        return Task.FromResult(
            _connections.ContainsKey(destination) ? _connections[destination] : null
        );
    }
}
