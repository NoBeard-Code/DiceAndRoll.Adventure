using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface ICharacterGrain : IGrainWithStringKey
{
    Task<List<CharacterInfo>> GetCharacters();
}
