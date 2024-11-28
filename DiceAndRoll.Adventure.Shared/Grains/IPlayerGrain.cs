using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey
{
    Task<List<CharacterInfo>> GetCharacters();

    Task JoinGame(string name, CharacterInfo character);

    Task<string> Look();

    Task<string> GoTo(string location);

    Task<string> GoBack();

    Task<string> Die();

    Task<Dictionary<string, string>> FindDirections();
}
