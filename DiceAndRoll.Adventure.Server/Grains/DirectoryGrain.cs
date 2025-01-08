using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class DirectoryGrain : Grain, IDirectoryGrain, ISingletonGrain
{
    private readonly ICharacterService _characterService;
    private readonly HashSet<Guid> _activeGames = [];
    private readonly ObserverManager<IGameLobby> _observerManager;

    public DirectoryGrain(ILogger<DirectoryGrain> logger, ICharacterService characterService)
    {
        _characterService = characterService;
        _observerManager = new ObserverManager<IGameLobby>(TimeSpan.FromMinutes(5), logger);
    }

    public Task RegisterGame(Guid grainId)
    {
        _activeGames.Add(grainId);
        _observerManager.Notify(o => o.GameRegistered(grainId));
        return Task.CompletedTask;
    }

    public Task UnregisterGame(Guid grainId)
    {
        _activeGames.Remove(grainId);
        _observerManager.Notify(o => o.GameRegistered(grainId));
        return Task.CompletedTask;
    }

    public Task<List<Guid>> GetActiveGames()
    {
        return Task.FromResult(_activeGames.ToList());
    }

    public Task<List<CharacterInfo>> GetCharacters()
    {
        return _characterService.GetCharactersAsync();
    }

    public Task Subscribe(IGameLobby observer)
    {
        _observerManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IGameLobby observer)
    {
        _observerManager.Unsubscribe(observer);
        return Task.CompletedTask;
    }
}
