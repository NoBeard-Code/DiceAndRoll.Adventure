using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Services;

public interface IMonsterService
{
    public Task<List<MonsterInfo>> GetMonstersAsync();
}
