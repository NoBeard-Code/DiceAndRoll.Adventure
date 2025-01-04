using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

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
        }
        else
        {
            state.State.Stats.Health -= damage;
        }

        await state.WriteStateAsync();

        // if belligerent is dead, return true
        return state.State.Stats.Health == 0;
    }

    public virtual Task Kill()
    {
        state.ClearStateAsync(); // clear the state

        DeactivateOnIdle();

        return Task.CompletedTask;
    }
}
