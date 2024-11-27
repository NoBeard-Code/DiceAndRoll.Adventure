using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Services;

public interface ICharacterService
{
    public Task<List<CharacterInfo>> GetCharactersAsync();
}
