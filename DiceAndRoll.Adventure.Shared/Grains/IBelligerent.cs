using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IBelligerent
{
    Task<BattleStats> GetStats();

    Task<bool> TakeHit(byte damage);

    Task Kill();
}
