using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IBelligerentGrain
{
    Task<BattleStats> GetStats();

    Task ResetStats(BattleStats stats);

    Task<bool> TakeHit(byte damage);

    Task Kill();
}
