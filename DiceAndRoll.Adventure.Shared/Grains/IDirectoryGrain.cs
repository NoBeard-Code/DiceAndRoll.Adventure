using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IDirectoryGrain : IGrainWithGuidKey
{
    internal Task RegisterGame(Guid gameId);

    internal Task UnregisterGame(Guid gameId);

    Task<List<Guid>> GetActiveGames();

    Task<List<CharacterInfo>> GetCharacters();
}
