using DiceAndRoll.Adventure.ConsoleApp;
using DiceAndRoll.Adventure.ConsoleApp.Configuration;
using DiceAndRoll.Adventure.ConsoleApp.Observers;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

var builder = Host.CreateDefaultBuilder(args);

builder.Configure();

var host = builder.Build();

AnsiConsole.MarkupLineInterpolated($"[bold yellow slowblink]{Constants.LOGO_TEXT_ART}[/]");

AnsiConsole.MarkupLine("[bold yellow]Dice & Roll Adventure Game || Player Console[/]" + Environment.NewLine);
AnsiConsole.MarkupLine("[silver]Please await while the console configures...[/]");

var client = host.Services.GetRequiredService<IClusterClient>();

await host.StartAsync();

// GAME LOBBY

IDirectoryGrain? directory = null;
IGameGrain? game = null;

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .StartAsync("Searching for a game ...", async ctx =>
    {
        directory = client.GetGrain<IDirectoryGrain>(Guid.Empty);

        var gameLobby = new GameLobby();
        var observer = client.CreateObjectReference<IGameLobby>(gameLobby);

        //var activeGames = await directory.GetActiveGames();

        ctx.Status("Waiting for games to be initialized...");

        var activeGames = await directory.GetActiveGames();

        if (activeGames.Count == 0)
        {
            await directory.Subscribe(observer);

            while (gameLobby.Games.Count == 0)
            {
                await Task.Delay(1000);
            }

            await directory.Unsubscribe(observer);
            activeGames = gameLobby.Games;
        }

        ctx.Status("Fetching first game from list of active games...");

        game = client.GetGrain<IGameGrain>(activeGames[0]);

        AnsiConsole.MarkupLine($"[olive]Loading game completed for game: {game.GetPrimaryKey()}.[/]");
    });

// PLAYER PROMPT

AnsiConsole.MarkupLine($"[green]Game successfully loaded.[/]");

var name = AnsiConsole.Prompt(
    new TextPrompt<string>("What will be your name in this game?"));

var characters = await directory!.GetCharacters();

ConsoleOutput.DisplayCharacters(characters);

var player = client.GetGrain<IPlayerGrain>(Guid.NewGuid());

var prompt = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title($"Nice, [yellow]{name}[/]! What will be your [green]character[/]?")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
        .AddChoices(characters.Select(x => x.Title))
        );

AnsiConsole.MarkupLine($"Great [yellow]{name}[/], you will be playing [yellow]{prompt}[/] character." + Environment.NewLine);

var character = characters.Find(x => x.Title == prompt);
await player.SetCharacter(name, character!);

var playerObserver = new PlayerObserver();
var playerObserverRef = client.CreateObjectReference<IPlayerObserver>(playerObserver);
await player.Subscribe(playerObserverRef);

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .StartAsync("Entering the game...", async ctx =>
    {
        await game!.Join(player);
    });

var gameObserver = new GameObserver();
var gameObserverRef = client.CreateObjectReference<IGameObserver>(gameObserver);
await game!.Subscribe(gameObserverRef);

// MAP/GO PROMPT

AnsiConsole.MarkupLine("Let's begin with our story...");

var initialText = $", drenched and exhausted, washes up on the shore of an unfamiliar land. The sand beneath you is coarse, and the air carries a salty tang. As you regain your senses, you notice the landscape around:";
AnsiConsole.MarkupLine($"[yellow]{name}[/][purple]{initialText}[/]");
AnsiConsole.WriteLine();

await AnsiConsole.Status()
    .StartAsync("Entering the map...", async ctx =>
    {
        var result = await player.Look();
        AnsiConsole.MarkupLine(result);
    });

var mapActions = new List<string>(["look", "go", "back", "fight", "kill", "quit"]);

string response = "start";
try
{
    while (response != string.Empty && !gameObserver.IsOver)
    {
        var command = AnsiConsole.Prompt(new TextPrompt<string>("What will you do?")
            .AddChoices(mapActions)
            .DefaultValue("look"));

        response = await Run(command);
        AnsiConsole.MarkupLine($"{response}");
    }
}
finally
{
    AnsiConsole.MarkupLine("[red]Game over![/]");
    await game!.Unsubscribe(gameObserverRef);
    await host.StopAsync();
}

Console.ReadLine();

async Task<string> Run(string command)
{
    command = RemoveUnnecessaryWords(command);

    string[] words = command.Split(' ');
    string verb = words[0].ToLower();

    return verb switch
    {
        "look" =>
            await player.Look(),

        "go" =>
            await GoPrompt(),

        "back" =>
            await player.GoBack(),

        "fight" =>
            await InteractiveFightPrompt(),

        "kill" =>
            await KillPrompt(),

        "quit" =>
            await LeaveGame(),

        _ => "[fuchsia]I don't understand[/]"
    }; 
}

async Task<string> LeaveGame()
{
    await player.Unsubscribe(playerObserverRef);

    await game!.Leave(player);
    return string.Empty;
}

string RemoveUnnecessaryWords(string s)
{
    var words = new[] { " on ", " the ", " a ", " to ", " in " };

    StringBuilder builder = new(s);
    foreach (var word in words)
    {
        builder.Replace(word, " ");
    }

    return builder.ToString();
}

async Task<string> GoPrompt()
{
    var directions = await player.FindDirections();

    if (directions is null || directions.Count == 0)
    {
        return "[grey]No directions from here. You are in the void.[/]";
    }
    else
    {
        directions.Add(string.Empty, "cancel");

        var command = AnsiConsole.Prompt(new TextPrompt<string>("[fuchsia]Go where?[/]")
            .AddChoices(directions.Values)
            .DefaultValue(directions.First().Value));

        if (command != "cancel")
        {
            var choice = directions.First(x => x.Value == command).Key;
            return await player.GoTo(choice);
        }

        return "Okay. Let's try something else...";
    }
}

async Task<string> KillPrompt()
{
    var others = await player.FindOthers();

    if (others is null || others.Count == 0)
    {
        return "[grey]There's no one there.[/]";
    }
    else
    {
        others.Add(new PlayerInfo(Guid.Empty, "cancel"));

        var command = AnsiConsole.Prompt(new TextPrompt<string?>("[fuchsia]Kill whom?[/]")
            .AddChoices(others.Select(x => x.Name))
            .DefaultValue(others.First().Name));

        if (command != "cancel")
        {
            var choice = others.First(x => x.Name == command);
            return await player.Kill(choice);
        }

        return "It's good to see that there's still some humanity out there ...";
    }
}

[Obsolete("This method is deprecated. Use InteractiveFightPrompt() instead.")]
async Task<string> FightPrompt()
{
    var encounter = await player.Fight();
    if (encounter == null)
    {
        return "[fuchsia]You are not in a combat situation.[/]";
    }

    AnsiConsole.MarkupLine("[red bold rapidblink]You have been engaged in combat![/]");

    const string fleeCommand = "FLEE!";
    var command = string.Empty;

    do
    {
        AnsiConsole.MarkupLine(await encounter.DisplayMoves());

        if (await player.AmIDead())
        {
            return "[red]You have been killed![/]";
        }

        var monsters = await encounter.GetMonsters();

        if (monsters is null || monsters.Count == 0)
        {
            return "[green]All the monsters have been killed![/]";
        }

        var options = monsters.Select(x => "attack " + x.Title).ToList();
        options.Add(fleeCommand);

        command = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[fuchsia]What will you do?[/]")
            .AddChoices(options)
        );

        if (command == fleeCommand)
        {
            return await encounter.Leave(player);
        }

        var option = options.First(x => x == command)[7..];
        var monster = monsters.First(x => x.Title == option);

        AnsiConsole.MarkupLine(await encounter.AttackMonster(player, monster.Id));

    } while (true);
}

async Task<string> InteractiveFightPrompt()
{
    var encounter = await player.Fight();
    if (encounter == null)
    {
        return "[fuchsia]You are not in a combat situation.[/]";
    }

    AnsiConsole.MarkupLine("[red bold]You have been engaged in combat![/]");

    List<MonsterIdentification>? monsters = null;

    var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

    var turnObserver = new TurnObserver(
        (output) => { // turn finished
            AnsiConsole.MarkupLine(output);
        }, (List<MonsterIdentification>? output) => { // your turn
            monsters = output;
            waitHandle.Set(); // signal the main thread to continue
        });
    var turnObserverRef = client.CreateObjectReference<ITurnObserver>(turnObserver);
    await encounter.Subscribe(turnObserverRef);

    const string fleeCommand = "FLEE!";
    var command = string.Empty;

    do
    {
        AnsiConsole.MarkupLine("[bold rapidblink]Waiting for your turn...[/]");
        waitHandle.WaitOne(); // wait for the signal from the observer

        AnsiConsole.MarkupLine("It's my turn, I'm on the move.");

        if (await player.AmIDead())
        {
            return "[red]You have been killed![/]";
        }

        monsters = await encounter.GetMonsters();

        if (monsters is null || monsters.Count == 0)
        {
            return "[green]All the monsters have been killed![/]";
        }

        var options = monsters.Select(x => "attack " + x.Title).ToList();
        options.Add(fleeCommand);

        command = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[fuchsia]What will you do? (you have 5 seconds to respond)[/]")
            .AddChoices(options)
        );

        if (command == fleeCommand)
        {
            // clean up and leave the encounter
            waitHandle.Dispose(); 
            await encounter.Unsubscribe(turnObserverRef);
            return await encounter.Leave(player);
        }

        // TODO: implement other turn options; for now, attack monster is the only option

        var option = options.First(x => x == command)[7..];
        var monster = monsters.First(x => x.Title == option);

        var reference = encounter.AsReference<IEncounterGrain>();

        AnsiConsole.MarkupLine(await reference.AttackMonster(player, monster.Id));

    } while (true);
}