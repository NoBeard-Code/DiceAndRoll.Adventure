using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using System.Security.Cryptography.Xml;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class PlayerGrain(
    ICharacterService characterService,
    [PersistentState("player", "game")] IPersistentState<CharacterInfo> state) : Grain, IPlayerGrain
{
    private const string FIRST_LOCATION = "lighthouse";

    private PlayerInfo _self = null!;
    private ILocationGrain? _currentLocation;
    private bool _died = false;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _self = new PlayerInfo(this.GetPrimaryKey(), "nobody");
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task<List<CharacterInfo>> GetCharacters()
    {
        return await characterService.GetCharactersAsync();
    }

    public async Task JoinGame(string name, CharacterInfo character)
    {
        _self = _self with { Name = name };
        _died = false;

        var firstLocation = GrainFactory.GetGrain<ILocationGrain>(FIRST_LOCATION);

        await state.ClearStateAsync();

        state.State = character;
        await state.WriteStateAsync();

        await SetCurrentLocation(firstLocation);
    }

    public async Task<string> Look()
    {
        if (_currentLocation is null)
        {
            return "[grey]You are in the void. Nobody returns from here, but ghosts![/]";
        } 
        else
        {
            return await _currentLocation.Describe(_self);
        }
    }

    public async Task<string> GoTo(string direction)
    {
        ILocationGrain? destination = null;
        if (_currentLocation is not null)
        {
            destination = await _currentLocation.ExitTo(direction);
        }

        var description = new StringBuilder();

        if (_currentLocation is not null && destination is not null)
        {
            description.AppendLine("Allright, let's go there!");

            await _currentLocation.Exit(_self);

            await SetCurrentLocation(destination);

            var desc = await destination.Describe(_self);
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

    private async Task SetCurrentLocation(ILocationGrain location)
    {
        await location.Enter(_self);
        _currentLocation = location;
    }

    public Task<string> GoBack()
    {
        throw new NotImplementedException();
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
        if (_currentLocation != null)
        {
            await _currentLocation.Exit(_self);
            _currentLocation = null;
        }

        _died = true;

        return string.Empty;
    }

    public async Task<Dictionary<string, string>> FindDirections()
    {
        if (_currentLocation is null) return null;

        var connections = await _currentLocation.GetConnections();

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

    public async Task<List<PlayerInfo>> FindOthers()
    {
        if (_currentLocation is null) return null;

        var players = await _currentLocation.GetPlayers();

        return players.Where(x => x.Key != _self.Key).ToList();
    }

    public Task<bool> AmIDead()
    {
        return Task.FromResult(_died);
    }
}
