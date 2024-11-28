using DiceAndRoll.Adventure.Shared.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

var builder = Host.CreateDefaultBuilder(args);

builder
    .UseOrleansClient(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddFilter("Orleans", LogLevel.None);
    });

var host = builder.Build();

// https://www.asciiart.eu/text-to-ascii-art

AnsiConsole.WriteLine("""
     ____  _             ___     ____       _ _                                   
    |  _ \(_) ___ ___   ( _ )   |  _ \ ___ | | |                                  
    | | | | |/ __/ _ \  / _ \/\ | |_) / _ \| | |                                  
    | |_| | | (_|  __/ | (_>  < |  _ < (_) | | |                                  
    |____/|_|\___\___|  \___/\/ |_| \_\___/|_|_|        ____                      
       / \   __| |_   _____ _ __ | |_ _   _ _ __ ___   / ___| __ _ _ __ ___   ___ 
      / _ \ / _` \ \ / / _ \ '_ \| __| | | | '__/ _ \ | |  _ / _` | '_ ` _ \ / _ \
     / ___ \ (_| |\ V /  __/ | | | |_| |_| | | |  __/ | |_| | (_| | | | | | |  __/
    /_/   \_\__,_| \_/ \___|_| |_|\__|\__,_|_|  \___|  \____|\__,_|_| |_| |_|\___|
    """);

AnsiConsole.WriteLine();
bool error = false;

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .StartAsync("Searching for a game ...", async ctx =>
    {
        try
        {
            await host.StartAsync();
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

AnsiConsole.MarkupLine($"[green]Game successfully loaded.[/]");

var name = AnsiConsole.Prompt(
    new TextPrompt<string>("What will be your name in this game?"));

var client = host.Services.GetRequiredService<IClusterClient>();
var player = client.GetGrain<IPlayerGrain>(Guid.NewGuid());

var characters = await player.GetCharacters();

var prompt = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title($"Nice, [yellow]{name}[/]! What will be your [green]character[/]?")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
        .AddChoices(characters.Select(x => x.Title))
        );

AnsiConsole.MarkupLine($"Great [yellow]{name}[/], you will be playing [yellow]{prompt}[/] character.");

var character = characters.Find(x => x.Title == prompt);

AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .Start("Entering the game...", ctx =>
    {
        player.JoinGame(name, character!);
        Thread.Sleep(1000);
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

string response = "start";
try
{
    while (response is not "")
    {
        var command = AnsiConsole.Prompt(new TextPrompt<string>("What will you do?")
            .AddChoices(["look", "go", "quit"])
            .DefaultValue("look"));

        response = await Run(command);
        AnsiConsole.MarkupLine($"{response}");
    }
}
finally
{
    await player.Die();
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

        "quit" =>
            await player.Die(),

        _ => "[fuchsia]I don't understand[/]"
    }; 
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