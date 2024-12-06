using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.Reflection;

AnsiConsole.MarkupLineInterpolated($"[bold yellow]{Constants.LOGO_TEXT_ART}[/]");

AnsiConsole.MarkupLine("[bold yellow]Dice & Roll Adventure Game || Game Server[/]" + Environment.NewLine);
AnsiConsole.MarkupLine("[silver]Please await while the game server configures...[/]" + Environment.NewLine);

IHost? host = null;

await AnsiConsole.Status()
    .StartAsync("Configuring Orleans...", async ctx => {

        var builder = Host.CreateDefaultBuilder(args);
        builder.Configure();
        host = builder.Build();

        await host.StartAsync();
    });

AnsiConsole.MarkupLine("[green]Microsoft Orleans successfully configured.[/]");

IGameGrain? game = null;
IGrainFactory? grainFactory = null;

await AnsiConsole.Status()
    .StartAsync("Setting up the game server...", async ctx =>
    {
        ctx.Status("Loading campaigns...");

        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var campaignFile = Path.Combine(path, "Data/campaign.json");

        if (!File.Exists(campaignFile))
        {
            AnsiConsole.MarkupLine("[red]Campaign file not found: {0}[/]", campaignFile);
            throw new ApplicationException();
        }

        AnsiConsole.MarkupLine("Setting up Dice & Roll Adventure game, please wait ...");

        // Initialize the game world
        grainFactory = host!.Services.GetRequiredService<IGrainFactory>();

        game = grainFactory.GetGrain<IGameGrain>(Guid.NewGuid());
        await game.ConfigureAndStart(campaignFile);
});

AnsiConsole.MarkupLine("[green]Game server successfully configured.[/]");

AnsiConsole.MarkupLine($"[olive]Loading game completed for game: {game.GetPrimaryKey()}.[/]");
AnsiConsole.MarkupLine("[bold yellow]Now you can invite the players to play the game :)...[/]" + Environment.NewLine);

try
{
    var exit = false;
    while (exit is false)
    {
        var command = AnsiConsole.Prompt(new TextPrompt<string>("Command:")
            .AddChoices(["stats", "context", "quit"])
            .DefaultValue("stats"));

        exit = await Run(command);
    }
}
finally
{
    await game!.End();
    AnsiConsole.MarkupLine("[red]Game server shut down![/]");
    await host!.StopAsync();
    AnsiConsole.MarkupLine("You can close this window now.");
}

return 0;

async Task<bool> Run(string command)
{
    command = command.ToLower();

    switch (command)
    {
        case "stats":
            return await DisplayGameStats(grainFactory!);
            
        case "context":
            break;

        case "quit":
            return true;

        default:
            AnsiConsole.MarkupLine("[fuchsia]I don't understand[/]");
            break;
    };

    return false;
}

async Task<bool> DisplayGameStats(IGrainFactory grainFactory)
{
    AnsiConsole.WriteLine();

    var directory = grainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);
    var games = await directory.GetActiveGames();
    var items = games.Select(game => new { Label = game.ToString(), Value = 24 });

    AnsiConsole.Write(new BarChart()
        .Width(60)
        .Label("[green bold underline]Active games/players[/]")
        .AddItems(items, (item) => new BarChartItem(
        item.Label, item.Value, Color.Yellow)));

    AnsiConsole.WriteLine("Characters breakdown:");

    AnsiConsole.Write(new BreakdownChart()
        .Width(60)
        // Add item is in the order of label, value, then color.
        .AddItem("Gunslinger", 1, Color.Red)
        .AddItem("Automaton", 0, Color.Blue)
        .AddItem("Musketeer", 3, Color.Green)
        .AddItem("Alchemist", 6, Color.Yellow)
        .AddItem("Assasin", 4, Color.LightGreen)
        .AddItem("Illusionist", 2, Color.Aqua));

    AnsiConsole.WriteLine();

    return false;
}