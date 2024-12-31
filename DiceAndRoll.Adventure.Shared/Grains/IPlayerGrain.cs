using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey
{
    Task SetCharacter(string name, CharacterInfo character);

    internal Task Appear(string location);

    Task<string> Look();

    Task<string> GoTo(string location);

    Task<string> GoBack();

    Task<string> Fight();

    Task<string> Attack(Guid targetId);

    Task<string> Flee();

    Task<List<MonsterIdentification>?> GetMonstersInEncounter();

    internal Task<BattleStats> GetStats();

    Task<string> Kill(PlayerInfo targetPlayer);

    internal Task<string> Die();

    Task<Dictionary<string, string>?> FindDirections();

    Task<List<PlayerInfo>?> FindOthers();

    Task<bool> AmIDead();
}
