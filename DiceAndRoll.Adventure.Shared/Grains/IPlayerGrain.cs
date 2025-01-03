using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey, IBelligerentGrain
{
    Task SetCharacter(string name, CharacterInfo character);

    internal Task Appear(string location);

    Task<string> Look();

    Task<string> GoTo(string location);

    Task<string> GoBack();

    Task<Dictionary<string, string>?> FindDirections();

    Task<List<PlayerInfo>?> FindOthers();

    internal Task<string> Die();

    Task<bool> AmIDead();

    [Obsolete]
    Task<string> Kill(PlayerInfo targetPlayer);

    Task<IEncounterGrain?> Fight();







    Task<string> Attack(Guid targetId);
}
