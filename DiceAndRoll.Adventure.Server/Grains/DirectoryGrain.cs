using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Extensions;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.Logging;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class DirectoryGrain : Grain, IDirectoryGrain, ISingletonGrain
{
    private readonly ICharacterService _characterService;
    private readonly HashSet<Guid> _activeGames = [];
    
    private IPublisherExtension<IGameLobby> _publisher;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _publisher = GrainContextComponentExtensions.GetGrainExtension<IPublisherExtension<IGameLobby>>(this.GrainContext);

        return base.OnActivateAsync(cancellationToken);
    }

    public DirectoryGrain(ILogger<DirectoryGrain> logger, ICharacterService characterService)
    {
        _characterService = characterService;
    }

    public async Task RegisterGame(Guid grainId)
    {
        _activeGames.Add(grainId);
        await _publisher.Notify(o => o.GameRegistered(grainId));
    }

    public async Task UnregisterGame(Guid grainId)
    {
        _activeGames.Remove(grainId);
        await _publisher.Notify(o => o.GameUnregistered(grainId));
    }

    public Task<List<Guid>> GetActiveGames()
    {
        return Task.FromResult(_activeGames.ToList());
    }

    public Task<List<CharacterInfo>> GetCharacters()
    {
        return _characterService.GetCharactersAsync();
    }
}
