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
    public async Task Configure(LocationInfo info, List<MonsterInfo> monsters)
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

        if (info.Enemies != null)
        {
            foreach (var enemy in info.Enemies)
            {
                var monster = monsters.FirstOrDefault(x => x.Name == enemy);
                if (monster != null)
                {
                    state.State.Monsters.Add(monster);
                }
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
            sb.AppendLine("[maroon]These guys are in the room with you:[/]");
            foreach (var other in others)
            {
                sb.Append("  ").AppendLine($"[olive]{other.Name}[/]");
            }
            sb.AppendLine();
        }

        if (state.State.Encounter != null)
        {
            sb.AppendLine("[red]The combat is underway![/]");
            sb.AppendLine();
        } 
        else if (state.State.Monsters.Count != 0)
        {
            sb.AppendLine("[red]Beware! You see some monsters here![/]");
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

    public async Task<IEncounterGrain> EngageInEncounter(IPlayerGrain player)
    {
        // check for active encounter, if not created: create encounter, add monster info, stats and this player info
        // if already active, add this player info to it, return it

        var encounter = state.State.Encounter;
        if (encounter == null)
        {
            // encounter grain is identified by the location grain key and its own guid
            encounter = GrainFactory.GetGrain<IEncounterGrain>(Guid.NewGuid(), this.GetPrimaryKeyString());

            // create monster grains to track their stats
            await encounter.Start(state.State.Monsters, player);

            state.State.Encounter = encounter;
        }
        else
        {
            await encounter.Join(player);
        }

        await state.WriteStateAsync();

        return encounter;
    }

    public async Task EndEncounter()
    {
        // TODO: write some battle statistics here

        // remove the encounter from the location grain
        var encounter = state.State.Encounter;
        if (encounter != null)
        {
            state.State.Encounter = null;
            await state.WriteStateAsync();
        }
    }

    public Task<IEncounterGrain?> GetEncounterReference()
    {
        return Task.FromResult(state.State.Encounter);
    }
}
