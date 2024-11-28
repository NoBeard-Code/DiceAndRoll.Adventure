using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class LocationGrain(
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
        _players.Add(playerName);
        return Task.CompletedTask;
    }

    public Task Exit(string playerName)
    {
        _players.RemoveAll(x => x == playerName);
        return Task.CompletedTask;
    }

    public Task<string> Describe()
    {
        var sb = new StringBuilder();
        sb.AppendLine(state.State.Description);

        sb.AppendLine("You can go in the following directions:");
        foreach (var location in _connections.Keys)
        {
            sb.Append("  ").AppendLine(location);
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
