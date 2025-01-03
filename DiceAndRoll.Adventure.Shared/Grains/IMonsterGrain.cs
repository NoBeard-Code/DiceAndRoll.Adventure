using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

internal interface IMonsterGrain : IGrainWithGuidCompoundKey, IBelligerent
{
    Task Configure(MonsterInfo info);

    Task<MonsterInfo> GetInfo();
}
