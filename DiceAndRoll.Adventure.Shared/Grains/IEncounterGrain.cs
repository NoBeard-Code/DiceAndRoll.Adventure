using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IEncounterGrain : IGrainWithGuidCompoundKey
{
    internal Task Start(List<MonsterInfo> monsters, IPlayerGrain initiator);

    internal Task<string> Join(IPlayerGrain player);

    Task<string> Leave(IPlayerGrain player);

    internal Task End();

    Task<List<MonsterIdentification>> GetMonsters();

    Task<string> AttackMonster(IPlayerGrain player, Guid monsterId);
}
