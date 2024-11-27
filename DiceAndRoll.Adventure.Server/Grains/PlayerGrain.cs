using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class PlayerGrain(
    ICharacterService characterService,
    [PersistentState("player", "game")] IPersistentState<CharacterInfo> state) : Grain, IPlayerGrain
{
    private string _name = string.Empty;

    public async Task<List<CharacterInfo>> GetCharactersAsync()
    {
        return await characterService.GetCharactersAsync();
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task JoinGameAsync(string name, CharacterInfo character)
    {
        _name = name;

        state.ClearStateAsync();

        state.State = character;
        state.WriteStateAsync();

        return Task.CompletedTask;
    }
}
