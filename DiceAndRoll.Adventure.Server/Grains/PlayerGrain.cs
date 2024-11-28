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

    //public override Task OnActivateAsync(CancellationToken cancellationToken)
    //{
    //    return base.OnActivateAsync(cancellationToken);
    //}

    //public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    //{
    //    return base.OnDeactivateAsync(reason, cancellationToken);
    //}

    public Task JoinGame(string name, CharacterInfo character)
    {
        _name = name;
        var firstLocation = GrainFactory.GetGrain<ILocationGrain>(FIRST_LOCATION);
        _currentLocation = firstLocation;
        firstLocation.Enter(name);

        state.ClearStateAsync();

        state.State = character;
        state.WriteStateAsync();

        return Task.CompletedTask;
    }

    public async Task<string?> Play(string command)
    {
        command = RemoveUnnecessaryWords(command);

        string[] words = command.Split(' ');
        string verb = words[0].ToLower();

        if (_currentLocation is null)
        {
            return "You are in the void. Nobody returns from here, but ghosts!";
        }

        return verb switch
        {
            "look" =>
                await _currentLocation.Describe(_name),

            "go" => words.Length == 1
                ? "[fuchsia]Go where?[/]"
                : await Go(words[1]),

            //"north" or "south" or "east" or "west" => await Go(verb),

            //"kill" => words.Length == 1
            //    ? "Kill what?"
            //    : await Kill(command[(verb.Length + 1)..]),

            //"drop" => await Drop(FindMyThing(Rest(words))),

            //"take" => await Take(await _roomGrain.FindThing(Rest(words))),

            //"inv" or "inventory" =>
            //"You are carrying: " +
            //    $"{string.Join(" ", _things.Select(x => x.Name))}",

            "end" => "",

            _ => "[fuchsia]I don't understand[/]"
        };
    }

    private string RemoveUnnecessaryWords(string s)
    {
        var words = new[] { " on ", " the ", " a ", " to ", " in " };

        StringBuilder builder = new(s);
        foreach (var word in words)
        {
            builder.Replace(word, " ");
        }

        return builder.ToString();
    }

    private async Task<string> Go(string direction)
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
            await destination.Enter(_name);

            _currentLocation = destination;

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

    public async Task Die()
    {
        // exit the room and the game
        if (_currentLocation != null)
        {
            await _currentLocation.Exit(_name);
            _currentLocation = null;
        }
    }
}
