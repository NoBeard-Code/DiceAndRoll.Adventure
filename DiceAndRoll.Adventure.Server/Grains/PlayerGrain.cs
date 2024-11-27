using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class PlayerGrain(
    ICharacterService characterService,
    [PersistentState("player", "game")] IPersistentState<CharacterInfo> state) : Grain, IPlayerGrain
{
    private const string FIRST_LOCATION = "lighthouse";

    private string _name = string.Empty;
    private ILocationGrain? _currentLocation; 

    public async Task<List<CharacterInfo>> GetCharacters()
    {
        return await characterService.GetCharactersAsync();
    }

    //public override Task OnActivateAsync(CancellationToken cancellationToken)
    //{
    //    return base.OnActivateAsync(cancellationToken);
    //}

    //public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    //{
    //    return base.OnDeactivateAsync(reason, cancellationToken);
    //}

    public Task JoinGame(string name, CharacterInfo character)
    {
        _name = name;
        var firstLocation = GrainFactory.GetGrain<ILocationGrain>(FIRST_LOCATION);
        _currentLocation = firstLocation;
        firstLocation.Enter(name);

        state.ClearStateAsync();

        state.State = character;
        state.WriteStateAsync();

        return Task.CompletedTask;
    }
}
