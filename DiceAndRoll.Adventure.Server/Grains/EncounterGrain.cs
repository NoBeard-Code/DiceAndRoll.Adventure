using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Timers;
using Orleans.Utilities;
using Spectre.Console;
using System.Text;

namespace DiceAndRoll.Adventure.Server.Grains;

internal record Belligerent(Guid Id, string Name, bool IsPlayer);

internal class EncounterState
{
    public List<IPlayerGrain> Players { get; set; } = [];

    public List<IMonsterGrain> Monsters { get; set; } = [];

    public byte Round { get; set; }

    public byte Turn { get; set; }

    public CurrentList<Belligerent> InitiativeRow { get; set; } = [];
}

[Reentrant]
internal sealed class EncounterGrain(
    ILogger<EncounterGrain> logger,
    ITimerRegistry timerRegistry,
    [PersistentState("encounter", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<EncounterState> state) : Grain, IEncounterGrain
{
    private readonly ObserverManager<ITurnObserver> _observerManager = new(TimeSpan.FromMinutes(5), logger);
    private IGrainTimer? _battleTimer;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var options = new GrainTimerCreationOptions        
        {
            DueTime = Timeout.InfiniteTimeSpan, // prevent timer from starting automatically
            Period = Timeout.InfiniteTimeSpan, 
            Interleave = true // ensure that the timer is interleaved with other work
        };

        _battleTimer = timerRegistry.RegisterGrainTimer(GrainContext, ExecuteNextTurn, this, options);

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _battleTimer?.Dispose();

        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task Start(List<MonsterInfo> monsters, IPlayerGrain initiator)
    {
        if (state.RecordExists)
            await state.ClearStateAsync();

        // create the list of belligerents, set the players and monsters, shuffle the initiative row

        CurrentList<Belligerent> beligerents = [ new Belligerent(initiator.GetPrimaryKey(), initiator.GetKeyExtension(), true)];

        List<string> monsterNames = [];
        foreach (var monster in monsters)
        {
            var names = monsterNames.Count(x => x == monster.Title); // ensure unique names
            monsterNames.Add(monster.Title);

            var name = $"{monster.Title} {names + 1}";
            var grain = GrainFactory.GetGrain<IMonsterGrain>(Guid.NewGuid(), name);
            await grain.Configure(monster);

            state.State.Monsters.Add(grain);

            beligerents.Add(new Belligerent(grain.GetPrimaryKey(), name, false));
        }

        state.State.Players = [initiator];
        state.State.InitiativeRow = [.. beligerents.Shuffle()];

        await state.WriteStateAsync();

        // start grain timer for the initial move (and therefore, starting the battle)
        NextPing(2000);
    }

    private void NextPing(long inMiliseconds)
    {
        _battleTimer?.Change(TimeSpan.FromMilliseconds(inMiliseconds), Timeout.InfiniteTimeSpan);
    }

    public Task<string> Join(IPlayerGrain player)
    {
        // add player to list of players, add it to the initiative row at the end

        state.State.Players.Add(player);

        state.State.InitiativeRow.Add(new Belligerent(player.GetPrimaryKey(), $"Player {player.GetKeyExtension()}", true));

        return Task.FromResult("Player has joined the fight.");
    }

    public async Task<string> Leave(IPlayerGrain player)
    {
        // if there is no more players, end the battle
        if (state.State.Players.Count <= 1)
        {
            await End();
            return "[maroon bold]You left the encounter. Battle is over.[/]"; 
        }

        // if there is still some players, remove player from list of players, remove it from the initiative row
        await RemovePlayer(player);

        return "[maroon bold]You left the encounter. Battle continues.[/]"; 
    }

    private async Task<string> End()
    {
        // Orleans does not automatically remove or deactivate referenced grains when you deactivate a base grain.
        // Each grain's lifecycle is managed independently, so all monster grain should be manually deactivated here.

        foreach (var monster in state.State.Monsters)
        {
            await monster.Kill();
        }

        await state.ClearStateAsync(); // clear the state

        // remove reference to the encounter from the location grain
        var location = GrainFactory.GetGrain<ILocationGrain>(this.GetKeyExtension());
        location?.EndEncounter();

        DeactivateOnIdle(); // deactivate the grain when it is not used

        return "[silver]The battle has ended.[/]";
    }

    private async Task RemovePlayer(IPlayerGrain player)
    {
        var target = state.State.InitiativeRow.First(x => x.Id == player.GetPrimaryKey());
        state.State.InitiativeRow.Remove(target);

        state.State.Players.Remove(player);

        await state.WriteStateAsync();

        await _observerManager.Notify(o => o.YourTurn(player.GetPrimaryKey()));
    }

    private async Task RemoveMonster(IMonsterGrain monster)
    {
        var target = state.State.InitiativeRow.First(x => x.Id == monster.GetPrimaryKey());
        state.State.InitiativeRow.Remove(target);

        state.State.Monsters.Remove(monster);

        await state.WriteStateAsync();
    }

    public Task<List<MonsterIdentification>> GetMonsters()
    {
        return Task.FromResult(state.State.Monsters
            .Select(x => new MonsterIdentification(x.GetPrimaryKey(), x.GetKeyExtension())).ToList());            
    }

    private MoveResult? _lastMove;

    private async Task ExecuteNextTurn(EncounterGrain thisGrain, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || _lastMove?.Result == MoveResultType.BattleEnded)
            return;

        var move = new MoveResult { Result = MoveResultType.Undetermined };

        if (state.State.Players.Count == 0 || state.State.Monsters.Count == 0) // either all monsters are defeated or all players are dead!
        {
            await End();
            return; // this is the end of the battle loop
        }

        // battle still continues

        if (_lastMove?.Result == MoveResultType.PlayersMove)
        {
            move = new MoveResult
            {
                Output = "Player did not respond, so the turn was skipped.",
                Result = MoveResultType.Skipped,
                BelligerentId = _lastMove.BelligerentId
            };

            await NotifyEndOfTurn(move);

            await _observerManager.Notify(o => o.AbandonTurn(move.BelligerentId.GetValueOrDefault()));

            // immediately start the next turn
        }

        // set round, turn and initiative row
        var sb = new StringBuilder();
        var initiativeRow = state.State.InitiativeRow;

        if (initiativeRow.IsLast || state.State.Round == 0)
        {
            // round is over, start a new one
            state.State.Round++;
            state.State.Turn = 0;
            initiativeRow.Reset();

            sb.AppendLine($"[fuchsia bold]ROUND {state.State.Round} started:[/]");
        }
        else
        {
            initiativeRow.MoveNext();
        }

        if (initiativeRow.Current == null) // also an indication of the end as there are no belligerents left in the initiative row
            return;

        // next turn
        state.State.Turn++;
        sb.AppendLine($"[fuchsia]Turn {state.State.Turn} started:[/]");

        if (initiativeRow.Current.IsPlayer)
        {
            if (state.State.Round == 1)
                await Task.Delay(1000, cancellationToken); // for better synchronization

            sb.AppendLine($"[blue]Player[/] [aqua]{initiativeRow.Current.Name}[/] [blue]is on the move.[/]");

            move = new MoveResult { Output = sb.ToString(), Result = MoveResultType.PlayersMove, BelligerentId = initiativeRow.Current.Id };

            AnsiConsole.MarkupLine($"Notifying player to perform the next turn (wait for 5000 ms).");

            await _observerManager.Notify(o => o.YourTurn(move.BelligerentId.GetValueOrDefault()));

            NextPing(5000);
        }
        else
        {
            // monster's turn:
            sb.AppendLine($"[darkgreen]Monster[/] [green]{initiativeRow.Current.Name}[/] [darkgreen]is on the move.[/]");

            var monster = state.State.Monsters.First(x => x.GetPrimaryKey() == initiativeRow.Current.Id);
            var randomPlayer = state.State.Players[Random.Shared.Next(0, state.State.Players.Count - 1)];

            sb.AppendLine(await ExecuteAttack(monster, randomPlayer));

            move = new MoveResult { Output = sb.ToString(), Result = MoveResultType.MonstersMove, BelligerentId = initiativeRow.Current.Id };

            NextPing(1500);
        }

        await NotifyEndOfTurn(move);
    }

    public async Task<string> AttackMonster(IPlayerGrain player, Guid monsterId)
    {
        _lastMove!.Result = MoveResultType.Played; // signal that the turn is being executed

        var defender = state.State.Monsters.First(x => x.GetPrimaryKey() == monsterId);

        return await ExecuteAttack(player, defender);
    }

    private async Task<string> ExecuteAttack(IBelligerentGrain attacker, IBelligerentGrain defender)
    {
        var sb = new StringBuilder();

        var attackerStats = await attacker.GetStats();
        sb.AppendLine($"[yellow]Attacker ({attacker.GetKeyExtension()}): {attackerStats}[/]");

        var defenderStats = await defender.GetStats();
        sb.AppendLine($"[yellow]Defender ({defender.GetKeyExtension()}): {defenderStats}[/]");

        var damage = attackerStats.Attack - defenderStats.Defense;

        if (damage > 0)
        {
            var isDead = await defender.TakeHit((byte)damage);
            sb.AppendLine($"[maroon]Belligerent {defender.GetPrimaryKey()} has taken {damage} damage.[/]");

            if (isDead)
            {
                sb.AppendLine($"[maroon bold]Belligerent {defender.GetPrimaryKey()} has been killed.[/]");
                await defender.Kill();

                if (defender is IPlayerGrain player)
                {
                    await RemovePlayer(player);
                }
                else if (defender is IMonsterGrain monster)
                {
                    await RemoveMonster(monster);
                }
            }
        }

        return sb.ToString();
    }

    public async Task SkipTurn(IPlayerGrain player)
    {
        AnsiConsole.MarkupLine($"Player skipped the turn.");

        var moveResult = new MoveResult
        {
            Output = "Player intentionally skipped the turn.",
            Result = MoveResultType.Skipped,
            BelligerentId = player.GetPrimaryKey()
        };

        await NotifyEndOfTurn(moveResult);

        // signal that the player has made a move
        NextPing(500);
    }

    private async Task NotifyEndOfTurn(MoveResult moveResult)
    {
        AnsiConsole.MarkupLine($"Turn finished ({moveResult.Result}): {moveResult.Output}");

        await _observerManager.Notify(o => o.TurnFinished(moveResult.Output));

        _lastMove = moveResult;
    }

    public Task Subscribe(ITurnObserver observer)
    {
        _observerManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(ITurnObserver observer)
    {
        _observerManager.Unsubscribe(observer);
        return Task.CompletedTask;
    }
}
