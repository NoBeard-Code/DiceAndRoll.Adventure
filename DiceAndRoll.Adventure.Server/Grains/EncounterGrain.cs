using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
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

internal sealed class EncounterGrain(
    [PersistentState("encounter", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<EncounterState> state) : Grain, IEncounterGrain
{
    public async Task Start(List<MonsterInfo> monsters, IPlayerGrain initiator)
    {
        if (state.RecordExists)
            await state.ClearStateAsync();

        // create the list of belligerents, set the players and monsters, shuffle the initiative row

        CurrentList<Belligerent> beligerents = [ new Belligerent(initiator.GetPrimaryKey(), "Player 1", true)]; // TODO: later, do the composite key

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

        // start the battle with calling DisplayMoves method
    }

    public Task<string> Join(IPlayerGrain player)
    {
        // add player to list of players, add it to the initiative row at the end

        state.State.Players.Add(player);

        state.State.InitiativeRow.Add(new Belligerent(player.GetPrimaryKey(), $"Player {state.State.Players.Count}", true));

        return Task.FromResult("Player has joined the fight.");
    }

    public async Task<string> Leave(IPlayerGrain player)
    {
        // if there is no more players, end the battle
        if (state.State.Players.Count <= 1)
        {
            await End();
            return "[maroon bold]You left encounter. Battle is over.[/]"; 
        }

        // if there is still some players, remove player from list of players, remove it from the initiative row
        await RemovePlayer(player);

        return "[maroon bold]You left encounter. Battle continues.[/]"; 
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

        return "[red]The battle has ended.[/]";
    }

    private async Task RemovePlayer(IPlayerGrain player)
    {
        var target = state.State.InitiativeRow.First(x => x.Id == player.GetPrimaryKey());
        state.State.InitiativeRow.Remove(target);

        state.State.Players.Remove(player);

        await state.WriteStateAsync();
    }

    public Task<List<MonsterIdentification>> GetMonsters()
    {
        return Task.FromResult(state.State.Monsters
            .Select(x => new MonsterIdentification(x.GetPrimaryKey(), x.GetKeyExtension())).ToList());            
    }

    public async Task<string> DisplayMoves()
    {
        // display the moves of the players and monsters
        var sb = new StringBuilder();
        do
        {
            var move = await ExecuteNextMove();

            sb.Append(move.Output);

            if (move.Result == MoveResultType.MonstersMove)
            {
                continue;
            }

            break;

        } while (true);

        return sb.ToString();
    }

    private async Task<MoveResult> ExecuteNextMove()
    {
        if (state.State.Players.Count == 0 || state.State.Monsters.Count == 0)
        {   
            // no one is on the move - either all monsters are defeated or all players are dead!
            return new MoveResult { Output = await End(), Result = MoveResultType.BattleEnded };
        }

        // battle still continues
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

        if (initiativeRow.Current == null)
        {
            return new MoveResult { Output = sb.ToString(), Result = MoveResultType.BattleEnded };
        }

        // next turn
        state.State.Turn++;
        sb.AppendLine($"[fuchsia]Turn {state.State.Turn} started:[/]");

        if (initiativeRow.Current.IsPlayer)
        {
            sb.AppendLine($"[darkblue]Player[/] [blue]{initiativeRow.Current.Name}[/] [darkblue]is on the move.[/]");
            return new MoveResult { Output = sb.ToString(), Result = MoveResultType.PlayersMove };
        }

        // monster's turn:
        sb.AppendLine($"[darkgreen]Monster[/] [green]{initiativeRow.Current.Name}[/] [darkgreen]is on the move.[/]");

        var monster = state.State.Monsters.First(x => x.GetPrimaryKey() == initiativeRow.Current.Id);

        sb.AppendLine(await SimulateMonsterMove(monster));

        return new MoveResult { Output = sb.ToString(), Result = MoveResultType.MonstersMove };
    }

    public async Task<string> AttackMonster(IPlayerGrain player, Guid monsterId)
    {
        // get the attacker stats for the attack, get the target stats for the defense, calculate damage, apply it
        var sb = new StringBuilder();

        var attackerStats = await player.GetStats();
        sb.AppendLine($"[yellow]Attacker ({player.GetPrimaryKey()}): {attackerStats}[/]");

        var monster = state.State.Monsters.First(x => x.GetPrimaryKey() == monsterId);
        var defenderStats = await monster.GetStats();
        sb.AppendLine($"[yellow]Defender ({monster.GetKeyExtension()}): {defenderStats}[/]");

        var damage = attackerStats.Attack - defenderStats.Defense;

        if (damage > 0)
        {
            var isDead = await monster.TakeHit((byte)damage);
            sb.AppendLine($"[green4 bold]Monster {monster.GetKeyExtension()} has taken {damage} damage.[/]");

            if (isDead)
            {
                sb.AppendLine($"[green4 bold]Monster {monster.GetKeyExtension()} has been killed.[/]");
                await monster.Kill();

                state.State.Monsters.Remove(monster);
                await state.WriteStateAsync();
            }
        }

        return sb.ToString();
    }

    private async Task<string> SimulateMonsterMove(IMonsterGrain monster)
    {
        // get the monster stats, get the player stats, calculate damage, apply it
        var sb = new StringBuilder();

        var attackerStats = await monster.GetStats();
        sb.AppendLine($"[yellow]Attacker ({monster.GetKeyExtension()}): {attackerStats}[/]");

        var randomPlayer = state.State.Players[Random.Shared.Next(0, state.State.Players.Count - 1)];
        var defenderStats = await randomPlayer.GetStats();
        sb.AppendLine($"[yellow]Defender ({randomPlayer.GetPrimaryKey()}): {defenderStats}[/]");

        var isDefenderKilled = await ExecuteAttack(attackerStats, defenderStats, randomPlayer);
        if (isDefenderKilled)
        {
            await RemovePlayer(randomPlayer);
        }

        return sb.ToString();
    }

    private static async Task<bool> ExecuteAttack(BattleStats attackerStats, BattleStats defenderStats, IBelligerentGrain defender)
    {
        // get the attacker stats for the attack, get the target stats for the defense, calculate damage, apply it

        AnsiConsole.MarkupLine($"[yellow]Attacker: {attackerStats}[/]");
        AnsiConsole.MarkupLine($"[yellow]Defender: {defenderStats}[/]");

        var damage = attackerStats.Attack - defenderStats.Defense;

        if (damage > 0)
        {
            var isDead = await defender.TakeHit((byte)damage);
            if (isDead)
            {
                await defender.Kill();
                return true; // defender is dead
            }
        }

        return false; // defender is still alive
    }
}
