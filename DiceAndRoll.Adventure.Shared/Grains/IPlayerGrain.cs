using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey
{
    Task<List<CharacterInfo>> GetCharactersAsync();

    Task JoinGameAsync(string name, CharacterInfo character);
}
