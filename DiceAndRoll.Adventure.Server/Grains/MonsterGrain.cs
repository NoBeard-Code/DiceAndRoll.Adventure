using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class MonsterState(MonsterInfo Info, BattleStats Stats) : BelligerentState(Stats)
{
    public MonsterInfo Info { get; set; } = Info;
}

internal sealed class MonsterGrain(
    [PersistentState("monster", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<MonsterState> state) : BelligerentGrain<MonsterState>(state), IMonsterGrain
{
    private readonly IPersistentState<MonsterState> _state = state;

    public async Task Configure(MonsterInfo info)
    {
        if (_state.RecordExists)
        {
            await _state.ClearStateAsync();
        }

        var stats = new BattleStats
        {
            Health = info.Health,
            Defense = info.Defense,
            Attack = info.Attack
        };

        _state.State = new MonsterState(info, stats);

        await _state.WriteStateAsync();

        AnsiConsole.MarkupLine("Monster {0} has been configured.", this.GetKeyExtension());
    }

    public Task<MonsterInfo> GetInfo()
    {
        return Task.FromResult(_state.State.Info);
    }
}
