using DiceAndRoll.Adventure.Shared.Grains;

namespace DiceAndRoll.Adventure.Server.Grains;

/// <summary>
/// This grain acts like a singleton, i.e. a directory for fetching available grains, without knowing their keys upfront.
/// It's called with providing a Guid.Empty as its primary key.
/// </summary>
internal sealed class DirectoryGrain : Grain, IDirectoryGrain
{
    private readonly HashSet<Guid> _activeGames = new();

    public Task RegisterGame(Guid grainId)
    {
        _activeGames.Add(grainId);
        return Task.CompletedTask;
    }

    public Task UnregisterGame(Guid grainId)
    {
        _activeGames.Remove(grainId);
        return Task.CompletedTask;
    }

    public Task<List<Guid>> GetActiveGames()
    {
        return Task.FromResult(_activeGames.ToList());
    }
}
