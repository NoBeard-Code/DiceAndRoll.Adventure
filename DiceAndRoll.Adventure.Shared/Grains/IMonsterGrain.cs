using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

internal interface IMonsterGrain : IBelligerentGrain
{
    Task Configure(MonsterInfo info);

    Task<MonsterInfo> GetInfo();
}
