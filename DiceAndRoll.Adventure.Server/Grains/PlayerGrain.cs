using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class PlayerState(string name) : BelligerentState
{
    public string Name { get; set; } = name;

    public CharacterInfo? Info { get; set; }

    public ILocationGrain? CurrentLocation { get; set; }

    public string? PreviousLocation { get; set; }

    public bool Died { get; set; }   
}

internal sealed class PlayerGrain(
    [PersistentState("player", Constants.ORLEANS_GRAIN_STORAGE)]
        IPersistentState<PlayerState> state) : BelligerentGrain<PlayerState>(state), IPlayerGrain
{
    private readonly IPersistentState<PlayerState> _state = state;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _state.State = new PlayerState("nobody");

        await _state.WriteStateAsync();

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task SetCharacter(string name, CharacterInfo character)
    {
        if (_state.RecordExists)
        {
            await _state.ClearStateAsync();
        }

        _state.State.Name = name;
        _state.State.Info = character;
        _state.State.Died = false;

        await _state.WriteStateAsync();
    }

    public async Task Appear(string location)
    {
        var firstLocation = GrainFactory.GetGrain<ILocationGrain>(location);
        await SetLocations(firstLocation); 
    }

    private PlayerInfo GetPlayerInfo()
    {
        return new PlayerInfo(this.GetPrimaryKey(), _state.State.Name);
    }

    public async Task<string> Look()
    {
        if (_state.State.CurrentLocation is null)
        {
            return "[grey]You are in the void. Nobody returns from here, but ghosts![/]";
        } 
        else
        {
            return await _state.State.CurrentLocation.Describe(GetPlayerInfo());
        }
    }

    public async Task<string> GoTo(string direction)
    {
        ILocationGrain? destination = null;
        if (_state.State.CurrentLocation is not null)
        {
            destination = await _state.State.CurrentLocation.ExitTo(direction);
        }

        var description = new StringBuilder();

        if (_state.State.CurrentLocation is not null && destination is not null)
        {
            description.AppendLine("Allright, let's go there!");

            await _state.State.CurrentLocation.Exit(GetPlayerInfo());

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

        _state.State.CurrentLocation = location;
        _state.State.PreviousLocation = previous; // for going back

        await _state.WriteStateAsync();
    }

    public async Task<string> GoBack()
    {
        if (_state.State.PreviousLocation is null)
        {
            return "[fuchsia]You cannot go back from here.[/]";
        }

        return await GoTo(_state.State.PreviousLocation);
    }

    public async Task<Dictionary<string, string>?> FindDirections()
    {
        if (_state.State.CurrentLocation is null) return null;

        var connections = await _state.State.CurrentLocation.GetConnections();

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

    public async Task<string> Die()
    {
        // TODO: drop all belongings

        // exit the room and the game
        if (_state.State.CurrentLocation != null)
        {
            await _state.State.CurrentLocation.Exit(GetPlayerInfo());
            _state.State.CurrentLocation = null;
        }

        _state.State.Died = true;

        await _state.WriteStateAsync();

        return string.Empty;
    }

    public Task<bool> AmIDead()
    {
        return Task.FromResult(_state.State.Died);
    }

    [Obsolete]
    public async Task<List<PlayerInfo>?> FindOthers()
    {
        if (_state.State.CurrentLocation is null) return null;

        var players = await _state.State.CurrentLocation.GetPlayers();

        return players.Where(x => x.Key != this.GetPrimaryKey()).ToList();
    }

    [Obsolete]
    public async Task<string> Kill(PlayerInfo targetPlayer)
    {
        var target = GrainFactory.GetGrain<IPlayerGrain>(targetPlayer.Key);

        await target.Die();

        return $"{targetPlayer.Name} is now dead.";
    }

    public async Task<IEncounterGrain?> Fight()
    {
        if (_state.State.CurrentLocation is null)
        {
            return null;
        }

        // reset battle stats
        _state.State.Stats = new BattleStats()
        {
            Health = _state.State.Info!.Health,
            Attack = _state.State.Info!.Attack,
            Defense = _state.State.Info!.Defense
        };

        AnsiConsole.MarkupLine($"Initial player stats: {_state.State.Stats}");

        await _state.WriteStateAsync();

        return await _state.State.CurrentLocation.EngageInEncounter(this);
    }

    #region Beligerent members 

    public async override Task Kill()
    {
        await Die();

        // DeactivateOnIdle();
        // no deactivation, as this will cause another grain to be created!
    }

    #endregion
}
