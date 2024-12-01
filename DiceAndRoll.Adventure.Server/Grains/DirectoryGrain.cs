using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class DirectoryGrain(ICharacterService characterService) : Grain, IDirectoryGrain, ISingletonGrain
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

    public Task<List<CharacterInfo>> GetCharacters()
    {
        return characterService.GetCharactersAsync();
    }
}
