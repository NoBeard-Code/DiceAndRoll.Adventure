using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Orleans.Concurrency;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IEncounterGrain : IGrainWithGuidCompoundKey, IObservableGrain<ITurnObserver>
{
    internal Task Start(List<MonsterInfo> monsters, IPlayerGrain initiator);

    internal Task<string> Join(IPlayerGrain player);

    Task<string> Leave(IPlayerGrain player);

    [ReadOnly, AlwaysInterleave]
    Task<List<MonsterIdentification>> GetMonsters();

    [AlwaysInterleave]
    Task<string> AttackMonster(IPlayerGrain player, Guid monsterId);

    [AlwaysInterleave]
    Task SkipTurn(IPlayerGrain player);
}
