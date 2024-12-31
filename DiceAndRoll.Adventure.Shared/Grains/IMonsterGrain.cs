using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

internal interface IMonsterGrain : IGrainWithGuidCompoundKey
{
    Task Configure(MonsterInfo info);

    Task<MonsterInfo> GetInfo();

    Task<BattleStats> GetStats();

    Task<bool> TakeHit(byte damage);

    Task AttackPlayer(byte damage);

    Task Kill();
}
