using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey, IBelligerentGrain, IObservableGrain<IPlayerObserver>, ILocationObserver
{
    Task SetCharacter(string name, CharacterInfo character);

    internal Task Appear(string location);

    Task<string> Look();

    Task<string> GoTo(string location);

    Task<string> GoBack();

    Task<Dictionary<string, string>?> FindDirections();

    internal Task<string> Die();

    Task<bool> AmIDead();

    [Obsolete]
    Task<List<PlayerInfo>?> FindOthers();

    [Obsolete]
    Task<string> Kill(PlayerInfo targetPlayer);

    Task<IEncounterGrain?> Fight();
}
