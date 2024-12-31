using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class MonsterState(MonsterInfo Info, BattleStats Stats)
{
    public MonsterInfo Info { get; set; } = Info;

    public BattleStats Stats { get; set; } = Stats;
}

internal sealed class MonsterGrain(
    [PersistentState("monster", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<MonsterState> state) : Grain, IMonsterGrain
{
    public async Task Configure(MonsterInfo info)
    {
        if (state.RecordExists)
        {
            await state.ClearStateAsync();
        }

        var stats = new BattleStats
        {
            Health = info.Health,
            Defense = info.Defense,
            Attack = info.Attack
        };

        state.State = new MonsterState(info, stats);

        await state.WriteStateAsync();

        AnsiConsole.MarkupLine("Monster {0} has been configured.", this.GetKeyExtension());
    }

    public Task<MonsterInfo> GetInfo()
    {
        return Task.FromResult(state.State.Info);
    }

    #region Beligerent members 

    public Task<BattleStats> GetStats()
    {
        return Task.FromResult(state.State.Stats);
    }

    public async Task<bool> TakeHit(byte damage)
    {
        if (state.State.Stats.Health <= damage)
        {
            state.State.Stats.Health = 0;
            AnsiConsole.MarkupLine("Monster's health {0} has been reduced to zero.", this.GetKeyExtension());
        }
        else
        {
            state.State.Stats.Health -= damage;
            AnsiConsole.MarkupLine("Monster {0} has taken {1} damage.", this.GetKeyExtension(), damage);
        }

        await state.WriteStateAsync();

        // if monster is dead, return true
        return state.State.Stats.Health == 0;
    }

    public Task Kill()
    {
        AnsiConsole.MarkupLine("[maroon]Monster {0} has been killed.[/]", this.GetKeyExtension());

        DeactivateOnIdle();

        return Task.CompletedTask;
    }

    #endregion
}
