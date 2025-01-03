using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;

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

        // TODO: continue with logic
        await NextTurn(state.State.InitiativeRow);
    }

    public Task<string> Join(IPlayerGrain player)
    {
        // add player to list of players, add it to the initiative row at the end

        state.State.Players.Add(player);

        state.State.InitiativeRow.Add(new Belligerent(player.GetPrimaryKey(), $"Player {state.State.Players.Count}", true));

        return Task.FromResult("Player has joined the fight.");
    }

    public async Task<bool> Leave(IPlayerGrain player)
    {
        // if there is no more players, end the battle
        if (state.State.Players.Count <= 1)
        {
            AnsiConsole.MarkupLine("[maroon bold]Encounter has been cancelled, the monsters will reset their state.[/]");

            await End();
            return true; // battle ended
        }

        // if there is still some players, remove player from list of players, remove it from the initiative row
        await RemovePlayer(player);

        return false; // battle continues
    }

    public Task End()
    {
        AnsiConsole.MarkupLine("[red]Battle has ended.[/]");

        // Orleans does not automatically remove or deactivate referenced grains when you deactivate a base grain.
        // Each grain's lifecycle is managed independently, so all monster grain should be manually deactivated here.

        foreach (var monster in state.State.Monsters)
        {
            monster.Kill();
        }

        state.ClearStateAsync(); // clear the state

        DeactivateOnIdle(); // deactivate the grain when it is not used

        return Task.CompletedTask;
    }

    private async Task StartBattle()
    {
        ResetBattle();

        do
        {
            await StartRound();
        }
        while (state.State.Players.Count > 1 && state.State.Monsters.Count > 0);
        
        await End();
    }

    private void ResetBattle()
    {
        //state.State.Players.Clear();
        //state.State.Monsters.Clear();
        state.State.Round = 0;
        //state.State.Turn = 0;
        //state.State.InitiativeRow.Clear();
    }

    private async Task<string> StartRound()
    {
        state.State.Round++;
        state.State.Turn = 0;

        AnsiConsole.MarkupLine($"[fuchsia bold]ROUND {state.State.Round} started:[/]");

        state.State.InitiativeRow.Reset();

        do
        {
            await NextTurn(state.State.InitiativeRow);

            state.State.InitiativeRow.MoveNext();

        } while (!state.State.InitiativeRow.IsLast);

        await state.WriteStateAsync();

        return "Round finished.";
    }

    private async Task NextTurn(CurrentList<Belligerent> initiativeRow)
    {
        if (initiativeRow.Current == null)
        {
            return; // no one is on the move - everyone is dead!
        }

        state.State.Turn++;
        AnsiConsole.MarkupLine($"[fuchsia]Turn {state.State.Turn} started:[/]");

        if (initiativeRow.Current.IsPlayer)
        {
            return;
        }

        // monster's turn:
        var monster = state.State.Monsters.First(x => x.GetPrimaryKey() == initiativeRow.Current.Id);
        await SimulateMonsterMove(monster);
    }

    private async Task SimulateMonsterMove(IMonsterGrain monster)
    {
        Thread.Sleep(1000); // simulate monster's thinking

        var monsterStats = await monster.GetStats();

        var randomPlayer = state.State.Players[Random.Shared.Next(0, state.State.Players.Count - 1)];
        
        var playerStats = new BattleStats
        {
            Health = 100
        }; //  await randomPlayer.GetStats(); // circular reference to the player grain!!

        Thread.Sleep(2000); // simulate monster's attack

        var isDefenderKilled = await ExecuteAttack(monsterStats, playerStats, randomPlayer);
        if (isDefenderKilled)
        {
            await RemovePlayer(randomPlayer);
        }

        Thread.Sleep(500); // simulate monster's thinking
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

    public async Task AttackMonster(BattleStats attackerStats, Guid monsterId)
    {
        // get the attacker stats for the attack, get the target stats for the defense, calculate damage, apply it

        AnsiConsole.MarkupLine($"[yellow]Attacker: {attackerStats}[/]");

        var monster = state.State.Monsters.First(x => x.GetPrimaryKey() == monsterId);
        AnsiConsole.MarkupLine("Monster Id: {0}, Title: {1}.", monster.GetPrimaryKey(), monster.GetKeyExtension());

        var defenderStats = await monster.GetStats();
        AnsiConsole.MarkupLine($"[yellow]Defender: {defenderStats}[/]");

        var damage = attackerStats.Attack - defenderStats.Defense;

        if (damage > 0)
        {
            var isDead = await monster.TakeHit((byte)damage);
            if (isDead)
            {
                await monster.Kill();

                state.State.Monsters.Remove(monster);
                await state.WriteStateAsync();
            }
        }
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
