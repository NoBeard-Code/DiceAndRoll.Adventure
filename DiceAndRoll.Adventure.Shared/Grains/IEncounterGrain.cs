using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IEncounterGrain : IGrainWithGuidKey
{
    internal Task StartBattle(List<MonsterInfo> monsters, IPlayerGrain initiator);

    internal Task EndBattle();

    internal Task<string> AddPlayer(IPlayerGrain player);

    internal Task<bool> RemovePlayer(IPlayerGrain player);

    Task<List<MonsterIdentification>> GetMonsters();

    Task AttackMonster(BattleStats attackerStats, Guid monsterId);
}
