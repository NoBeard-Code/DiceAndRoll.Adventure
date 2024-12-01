using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class CharacterGrain(ICharacterService characterService) : Grain, ICharacterGrain
{
    public Task<List<CharacterInfo>> GetCharacters()
    {
        return characterService.GetCharactersAsync();
    }
}
