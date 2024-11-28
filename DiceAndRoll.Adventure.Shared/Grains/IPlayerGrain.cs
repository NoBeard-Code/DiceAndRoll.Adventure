using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey
{
    Task<List<CharacterInfo>> GetCharacters();

    Task JoinGame(string name, CharacterInfo character);

    Task<string?> Play(string command);

    Task Die();
}
