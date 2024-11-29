using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.UnitTests.Fakes;

internal class FakeCharacterService : ICharacterService
{
    public Task<List<CharacterInfo>> GetCharactersAsync()
    {
        var characters = new List<CharacterInfo>
        {
            new("hedge_knight", "Hedge Knight", "Some background here...",
            "Some description here...", 100, 10, 10, "melee", [], [], [], "weak_armor", "weak_weapon")
        };

        return Task.FromResult(characters);
    }
}
