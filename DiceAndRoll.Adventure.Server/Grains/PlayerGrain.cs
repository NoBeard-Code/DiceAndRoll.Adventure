using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class PlayerGrain(
    ICharacterService characterService,
    [PersistentState("player", "game")] IPersistentState<CharacterInfo> state) : Grain, IPlayerGrain
{
    private const string FIRST_LOCATION = "lighthouse";

    private string _name = string.Empty;
    private ILocationGrain? _currentLocation; 

    public async Task<List<CharacterInfo>> GetCharacters()
    {
        return await characterService.GetCharactersAsync();
    }

    public async Task JoinGame(string name, CharacterInfo character)
    {
        _name = name;
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
            return await _currentLocation.Describe(_name);
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

            await _currentLocation.Exit(_name);

            await SetCurrentLocation(destination);

            var desc = await destination.Describe(_name);
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
        await location.Enter(_name);
        _currentLocation = location;
    }

    public Task<string> GoBack()
    {
        throw new NotImplementedException();
    }

    public async Task<string> Die()
    {
        // TODO: drop all belongings

        // exit the room and the game
        if (_currentLocation != null)
        {
            await _currentLocation.Exit(_name);
            _currentLocation = null;
        }

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
}
