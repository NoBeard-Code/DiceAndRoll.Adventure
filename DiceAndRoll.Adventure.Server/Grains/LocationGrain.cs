using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class LocationGrain(
    [PersistentState("location", Constants.ORLEANS_GRAIN_STORAGE)] 
    IPersistentState<LocationInfo> state) : Grain, ILocationGrain
{
    private readonly Dictionary<string, ILocationGrain> _connections = [];
    private readonly List<PlayerInfo> _players = [];

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

    public Task Enter(PlayerInfo player)
    {
        _players.RemoveAll(x => x.Key == player.Key);
        _players.Add(player);

        return Task.CompletedTask;
    }

    public Task Exit(PlayerInfo player)
    {
        _players.RemoveAll(x => x.Key == player.Key);
        return Task.CompletedTask;
    }

    public async Task<string> Describe(PlayerInfo toMe)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[purple]{state.State.Description}[/]");
        sb.AppendLine();

        var others = _players.Where(x => x.Key != toMe.Key); 
        if (others.Any())
        {
            sb.AppendLine("[maroon]Beware! These guys are in the room with you:[/]");
            foreach (var other in others)
            {
                sb.Append("  ").AppendLine($"[olive]{other.Name}[/]");
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

    public Task<List<PlayerInfo>> GetPlayers()
    {
        return Task.FromResult(_players); 
    }
}
