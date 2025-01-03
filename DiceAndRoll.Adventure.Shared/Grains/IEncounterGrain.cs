using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IEncounterGrain : IGrainWithGuidKey
{
    internal Task Start(List<MonsterInfo> monsters, IPlayerGrain initiator);

    internal Task<string> Join(IPlayerGrain player);

    internal Task<bool> Leave(IPlayerGrain player);

    internal Task End();

    Task<List<MonsterIdentification>> GetMonsters();

    Task AttackMonster(BattleStats attackerStats, Guid monsterId);
}
