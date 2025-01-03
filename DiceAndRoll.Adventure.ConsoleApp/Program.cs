using DiceAndRoll.Adventure.ConsoleApp;
using DiceAndRoll.Adventure.ConsoleApp.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
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

bool error = false;

var client = host.Services.GetRequiredService<IClusterClient>();
IDirectoryGrain? directory = null;
IGameGrain? game = null;

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .StartAsync("Searching for a game ...", async ctx =>
    {
        try
        {
            await host.StartAsync();

            directory = client.GetGrain<IDirectoryGrain>(Guid.Empty);

            var activeGames = await directory.GetActiveGames();
            if (activeGames == null || activeGames.Count == 0) throw new Exception();

            ctx.Status("Fetching first game from list of active games...");
            game = client.GetGrain<IGameGrain>(activeGames[0]);
        }
        catch (Exception)
        {
            error = true;
        }
});

if (error)
{
    AnsiConsole.Markup("[red]No game is found.[/]");
    return;
}

AnsiConsole.MarkupLine($"[olive]Loading game completed for game: {game.GetPrimaryKey()}.[/]");
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

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .StartAsync("Entering the game...", async ctx =>
    {
        await game!.Join(player);
    });

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


var actions = new List<string>(["look", "go", "back", "fight", "kill", "quit"]);

string response = "start";
try
{
    while (response is not "")
    {
        var command = AnsiConsole.Prompt(new TextPrompt<string>("What will you do?")
            .AddChoices(actions)
            .DefaultValue("look"));

        response = await Run(command);
        AnsiConsole.MarkupLine($"{response}");
    }
}
finally
{
    AnsiConsole.MarkupLine("[red]Game over![/]");
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
            await FightPrompt(),

        "kill" =>
            await KillPrompt(),

        "quit" =>
            await LeaveGame(),

        _ => "[fuchsia]I don't understand[/]"
    }; 
}

async Task<string> LeaveGame()
{
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

async Task<string> FightPrompt()
{
    AnsiConsole.MarkupLine(await player.Fight());

    const string fleeCommand = "FLEE!";
    var command = string.Empty;

    do
    {
        var monsters = await player.GetMonstersInEncounter();

        if (monsters is null || monsters.Count == 0)
        {
            return "[fuchsia]There are no monsters to fight.[/]";
        }
        else
        {
            var options = monsters.Select(x => "attack " + x.Title).ToList();
            options.Add(fleeCommand);

            command = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[fuchsia]What will you do?[/]")
                .AddChoices(options)
            );

            if (command == fleeCommand)
            {
                return await player.Flee();
            }
            else
            {
                var option = options.First(x => x == command)[7..];
                var monster = monsters.First(x => x.Title == option);

                await player.Attack(monster.Id);
            }
        }
    } while (true);
}