using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class PlayerState(string Name)
{
    public string Name { get; set; } = Name;

    public CharacterInfo? Info { get; set; }

    public ILocationGrain? CurrentLocation { get; set; }

    public string? PreviousLocation { get; set; }

    public BattleStats Stats { get; set; } = new();

    public bool Died { get; set; }   
}

internal sealed class PlayerGrain(
    [PersistentState("player", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<PlayerState> state) : Grain, IPlayerGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        state.State = new PlayerState("nobody");

        await state.WriteStateAsync();

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task SetCharacter(string name, CharacterInfo character)
    {
        if (state.RecordExists)
        {
            await state.ClearStateAsync();
        }

        state.State.Name = name;
        state.State.Info = character;
        state.State.Died = false;

        await state.WriteStateAsync();
    }

    public async Task Appear(string location)
    {
        var firstLocation = GrainFactory.GetGrain<ILocationGrain>(location);
        await SetLocations(firstLocation); 
    }

    private PlayerInfo GetPlayerInfo()
    {
        return new PlayerInfo(this.GetPrimaryKey(), state.State.Name);
    }

    public async Task<string> Look()
    {
        if (state.State.CurrentLocation is null)
        {
            return "[grey]You are in the void. Nobody returns from here, but ghosts![/]";
        } 
        else
        {
            return await state.State.CurrentLocation.Describe(GetPlayerInfo());
        }
    }

    public async Task<string> GoTo(string direction)
    {
        ILocationGrain? destination = null;
        if (state.State.CurrentLocation is not null)
        {
            destination = await state.State.CurrentLocation.ExitTo(direction);
        }

        var description = new StringBuilder();

        if (state.State.CurrentLocation is not null && destination is not null)
        {
            description.AppendLine("Allright, let's go there!");

            await state.State.CurrentLocation.Exit(GetPlayerInfo());

            await SetLocations(destination, direction);

            var desc = await destination.Describe(GetPlayerInfo());
            if (desc != null)
            {
                description.AppendLine(desc);
            }
        }
        else
        {
            description.AppendLine("[fuchsia]You cannot go in that direction.[/]");
        }

        return description.ToString();
    }

    private async Task SetLocations(ILocationGrain location, string? previous = null)
    {
        await location.Enter(GetPlayerInfo());

        state.State.CurrentLocation = location;
        state.State.PreviousLocation = previous; // for going back

        await state.WriteStateAsync();
    }

    public async Task<string> GoBack()
    {
        if (state.State.PreviousLocation is null)
        {
            return "[fuchsia]You cannot go back from here.[/]";
        }

        return await GoTo(state.State.PreviousLocation);
    }

    public async Task<string> Fight()
    {
        if (state.State.CurrentLocation is null)
        {
            AnsiConsole.MarkupLine("[fuchsia]You are not in a combat situation.[/]");
            return "[fuchsia]You are not in a combat situation.[/]";
        }

        await state.State.CurrentLocation.EngageInEncounter(this);

        // reset battle stats
        state.State.Stats = new BattleStats()
        {
            Health = state.State.Info!.Health,
            Attack = state.State.Info!.Attack,
            Defense = state.State.Info!.Defense
        };

        await state.WriteStateAsync();

        AnsiConsole.MarkupLine("[red bold rapidblink]You have been engaged in combat![/]");
        return "[red bold rapidblink]You have been engaged in combat![/]";
    }

    public async Task<string> Attack(Guid targetId)
    {
        if (state.State.CurrentLocation is null)
        {
            AnsiConsole.MarkupLine("[fuchsia]You are not in a combat situation.[/]");
            return "[fuchsia]You are not in a combat situation.[/]";
        }

        var encounter = await state.State.CurrentLocation.GetEncounterReference();
        if (encounter is null)
        {
            AnsiConsole.MarkupLine("[fuchsia]You are not in a combat situation.[/]");
            return "[fuchsia]You are not in a combat situation.[/]";
        }

        await encounter.AttackMonster(this, targetId);

        return string.Empty;
    }

    public async Task<string> Flee()
    {
        if (state.State.CurrentLocation is null)
        {
            AnsiConsole.MarkupLine("[fuchsia]You are not in a combat situation.[/]");
            return "[fuchsia]You are not in a combat situation.[/]";
        }

        await state.State.CurrentLocation.LeaveEncounter(this);

        AnsiConsole.MarkupLine("[green]You have fled from combat![/]");
        return "[teal]You have fled from combat![/]";
    }

    public async Task<List<MonsterIdentification>?> GetMonstersInEncounter()
    {
        if (state.State.CurrentLocation is null)
        {
            return null;
        }

        return await state.State.CurrentLocation.GetMonsters(true);
    }

    public Task<BattleStats> GetStats()
    {
        return Task.FromResult(state.State.Stats);
    }

    public async Task<string> Kill(PlayerInfo targetPlayer)
    {
        var target = GrainFactory.GetGrain<IPlayerGrain>(targetPlayer.Key);

        await target.Die();

        return $"{targetPlayer.Name} is now dead.";
    }

    public async Task<string> Die()
    {
        // TODO: drop all belongings

        // exit the room and the game
        if (state.State.CurrentLocation != null)
        {
            await state.State.CurrentLocation.Exit(GetPlayerInfo());
            state.State.CurrentLocation = null;
        }

        state.State.Died = true;

        await state.WriteStateAsync();

        return string.Empty;
    }

    public async Task<Dictionary<string, string>?> FindDirections()
    {
        if (state.State.CurrentLocation is null) return null;

        var connections = await state.State.CurrentLocation.GetConnections();

        var result = new Dictionary<string, string>();  

        foreach (var location in connections)
        {
            var info = await location.Value.GetInfo();
            if (info != null)
            {
                result.Add(location.Key, info.Title);
            }
        }

        return result;
    }

    public async Task<List<PlayerInfo>?> FindOthers()
    {
        if (state.State.CurrentLocation is null) return null;

        var players = await state.State.CurrentLocation.GetPlayers();

        return players.Where(x => x.Key != this.GetPrimaryKey()).ToList();
    }

    public Task<bool> AmIDead()
    {
        return Task.FromResult(state.State.Died);
    }
}
