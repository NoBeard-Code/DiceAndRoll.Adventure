using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class LocationState(LocationInfo Info)
{
    public LocationInfo Info { get; set; } = Info;

    public Dictionary<string, ILocationGrain> Connections { get; set; } = [];

    public List<PlayerInfo> Players { get; set; } = [];

    public IEncounterGrain? Encounter { get; set; }

    public List<MonsterInfo> Monsters { get; set; } = [];
}

internal sealed class LocationGrain(
    [PersistentState("location", Constants.ORLEANS_GRAIN_STORAGE)] 
    IPersistentState<LocationState> state) : Grain, ILocationGrain
{
    public async Task Configure(LocationInfo info)
    {
        if (state.RecordExists) 
            return; 

        state.State = new LocationState(info);

        foreach (var connection in info.ConnectsTo)
        {
            var grain = GrainFactory.GetGrain<ILocationGrain>(connection);
            if (grain != null)
            {
                state.State.Connections.Add(connection, grain);               
            }
        }

        await state.WriteStateAsync();
    }

    public Task<LocationInfo?> GetInfo()
    {
        return Task.FromResult((state.RecordExists) ? state.State.Info : null);
    }

    public async Task Enter(PlayerInfo player)
    {
        state.State.Players.RemoveAll(x => x.Key == player.Key);
        state.State.Players.Add(player);

        await state.WriteStateAsync();
    }

    public async Task Exit(PlayerInfo player)
    {
        state.State.Players.RemoveAll(x => x.Key == player.Key);

        await state.WriteStateAsync();
    }

    public async Task<string> Describe(PlayerInfo toMe)
    {
        var sb = new StringBuilder();
        var info = state.State.Info;

        sb.Append($"[fuchsia]{info.Title}[/] ");
        sb.AppendLine($"[olive]({info.Terrain})[/]");
        sb.AppendLine($"[purple]{info.Description}[/]");
        sb.AppendLine();

        var others = state.State.Players.Where(x => x.Key != toMe.Key); 
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
        foreach (var location in state.State.Connections.Values)
        {
            var locationInfo = await location.GetInfo();
            if (locationInfo != null)
            {
                sb.Append("  ").AppendLine($"[silver]{locationInfo.Title}[/]");
            }
        }

        return sb.ToString();
    }

    public Task<ILocationGrain?> ExitTo(string destination)
    {
        return Task.FromResult(
            state.State.Connections.ContainsKey(destination) ? state.State.Connections[destination] : null
        );
    }

    public Task<Dictionary<string, ILocationGrain>> GetConnections()
    {
        return Task.FromResult(state.State.Connections);
    }

    public Task<List<PlayerInfo>> GetPlayers()
    {
        return Task.FromResult(state.State.Players); 
    }
}
