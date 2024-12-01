namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IDirectoryGrain : IGrainWithGuidKey
{
    Task RegisterGame(Guid gameId);

    Task UnregisterGame(Guid gameId);

    Task<List<Guid>> GetActiveGames();
}
