using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;

namespace DiceAndRoll.Adventure.Server.Grains;

internal abstract class BelligerentState
{
    protected BelligerentState()
    {
        
    }

    protected BelligerentState(BattleStats stats)
    {
        Stats = stats;
    }

    public BattleStats Stats { get; set; } = new();
}

internal abstract class BelligerentGrain<TState>(
    IPersistentState<TState> state) : Grain, IBelligerentGrain where TState : BelligerentState
{
    public Task<BattleStats> GetStats()
    {
        return Task.FromResult(state.State.Stats);
    }

    public async Task ResetStats(BattleStats stats)
    {
        state.State.Stats = stats;
        await state.WriteStateAsync();
    }

    public async virtual Task<bool> TakeHit(byte damage)
    {
        if (state.State.Stats.Health <= damage)
        {
            state.State.Stats.Health = 0;
            AnsiConsole.MarkupLine("Belligerent's health {0} has been reduced to zero.", this.GetPrimaryKey());
        }
        else
        {
            state.State.Stats.Health -= damage;
            AnsiConsole.MarkupLine("Belligerent {0} has taken {1} damage.", this.GetPrimaryKey(), damage);
        }

        await state.WriteStateAsync();

        // if belligerent is dead, return true
        return state.State.Stats.Health == 0;
    }

    public virtual Task Kill()
    {
        AnsiConsole.MarkupLine("[maroon]Belligerent {0} has been killed.[/]", this.GetPrimaryKey());

        DeactivateOnIdle();

        return Task.CompletedTask;
    }
}
