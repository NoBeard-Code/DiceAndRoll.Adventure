using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class EncounterState
{
    public List<IPlayerGrain> Players { get; set; } = [];

    public List<IMonsterGrain> Monsters { get; set; } = [];

    public byte Round { get; set; }

    public byte Turn { get; set; }

    public List<Guid> InitiativeRow { get; set; } = [];
}

internal sealed class EncounterGrain(
    [PersistentState("encounter", Constants.ORLEANS_GRAIN_STORAGE)]
    IPersistentState<EncounterState> state) : Grain, IEncounterGrain
{
    public async Task StartBattle(List<MonsterInfo> monsters, IPlayerGrain initiator)
    {
        // store beligerents into state, construct initiative row, reset round and turn

        List<Guid> beligerents = [initiator.GetPrimaryKey()];

        List<string> monsterNames = [];
        foreach (var monster in monsters)
        {
            var names = monsterNames.Count(x => x == monster.Title); // ensure unique names
            monsterNames.Add(monster.Title);    

            var grain = GrainFactory.GetGrain<IMonsterGrain>(Guid.NewGuid(), $"{monster.Title} {names + 1}");
            await grain.Configure(monster);
            state.State.Monsters.Add(grain);
            beligerents.Add(grain.GetPrimaryKey());
        }

        state.State.Players = [initiator];
        state.State.Round = 1;
        state.State.InitiativeRow = beligerents.Shuffle();

        AnsiConsole.MarkupLine($"[fuchsia]Battle has begun. Round {state.State.Round}:[/]");

        NextTurn();

        await state.WriteStateAsync();
    }

    private async void NextTurn()
    {
        state.State.Turn++;

        if (state.State.Turn > state.State.InitiativeRow.Count)
        {
            state.State.Turn = 1;
            state.State.Round++;
            AnsiConsole.MarkupLine($"[fuchsia]Round {state.State.Round} started:[/]");
        }

        AnsiConsole.MarkupLine($"[purple]Turn {state.State.Turn}:[/]");

        // find the beligerent and execute attack if it's monster's turn

        var candidate = state.State.InitiativeRow[state.State.Turn - 1];

        var foundPlayer = state.State.Players.Find(x => x.GetPrimaryKey() == candidate);
        if (foundPlayer != null)
        {
            return;
        }
        else
        {
            var foundMonster = state.State.Monsters.Find(x => x.GetPrimaryKey() == candidate);
            if (foundMonster != null)
            {
                var randomPlayer = state.State.Players[Random.Shared.Next(0, state.State.Players.Count - 1)];

                var isDefenderKilled = await ExecuteAttack(
                    await foundMonster.GetStats(),
                    await randomPlayer.GetStats(), randomPlayer);

                if (isDefenderKilled)
                {
                    if (randomPlayer is IMonsterGrain)
                    {
                        // state.State.Monsters.Remove(randomPlayer);
                    }
                    else if (randomPlayer is IPlayerGrain)
                    {
                        state.State.Players.Remove(randomPlayer);
                    }
                    state.State.InitiativeRow.Remove(randomPlayer.GetPrimaryKey());
                }
            }
        }
    }

    public Task EndBattle()
    {
        AnsiConsole.MarkupLine("[red]Battle has ended.[/]");

        // Orleans does not automatically remove or deactivate referenced grains when you deactivate a base grain.
        // Each grain's lifecycle is managed independently, so all monster grain should be manually deactivated here.

        foreach (var monster in state.State.Monsters)
        {
            monster.Kill();
        }

        DeactivateOnIdle(); // deactivate the grain when it is not used

        return Task.CompletedTask;
    }

    public Task<string> AddPlayer(IPlayerGrain player)
    {
        // add player to list of players, add it to the initiative row at the end

        state.State.Players.Add(player);

        state.State.InitiativeRow.Add(player.GetPrimaryKey());

        return Task.FromResult("Player has joined the fight.");
    }

    public async Task<bool> RemovePlayer(IPlayerGrain player)
    {
        // if there is no more players, end the battle
        if (state.State.Players.Count <= 1)
        {
            AnsiConsole.MarkupLine("[maroon bold]Encounter has been cancelled, the monsters will reset their state.[/]");

            await EndBattle();
            return true; // battle ended
        }

        // if there is still some players, remove player from list of players, remove it from the initiative row
        state.State.Players.Remove(player);
        state.State.InitiativeRow.Remove(player.GetPrimaryKey());

        return false; // battle continues
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
            }
        }
    }

    private static async Task<bool> ExecuteAttack(BattleStats attackerStats, BattleStats defenderStats, IBelligerent defender)
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
