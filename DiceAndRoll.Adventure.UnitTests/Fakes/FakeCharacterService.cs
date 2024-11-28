using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.UnitTests.Fakes;

internal class FakeCharacterService : ICharacterService
{
    public Task<List<CharacterInfo>> GetCharactersAsync()
    {
        throw new NotImplementedException();
    }
}
