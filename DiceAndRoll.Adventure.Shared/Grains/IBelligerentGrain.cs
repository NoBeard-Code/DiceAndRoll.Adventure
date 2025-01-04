using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IBelligerentGrain
{
    internal Task<BattleStats> GetStats();

    internal Task ResetStats(BattleStats stats);

    internal Task<bool> TakeHit(byte damage);

    internal Task Kill();
}
