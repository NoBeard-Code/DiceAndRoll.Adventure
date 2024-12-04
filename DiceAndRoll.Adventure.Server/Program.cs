using DiceAndRoll.Adventure.Server.Configuration;
using DiceAndRoll.Adventure.Shared.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.Reflection;

AnsiConsole.MarkupLine("[bold yellow]Dice & Roll Adventure Game[/]");
AnsiConsole.MarkupLine("[silver]Please await while the game server configures...[/]");

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
        var grainFactory = host!.Services.GetRequiredService<IGrainFactory>();

        game = grainFactory.GetGrain<IGameGrain>(Guid.NewGuid());
        await game.ConfigureAndStart(campaignFile);
});

AnsiConsole.MarkupLine("[green]Game server successfully configured.[/]");

AnsiConsole.MarkupLine($"[olive]Loading game completed for game: {game.GetPrimaryKey()}.[/]");
AnsiConsole.MarkupLine("[bold yellow]Now you can invite the players to play the game :)...[/]");

AnsiConsole.MarkupLine(string.Empty);
AnsiConsole.MarkupLine("[grey]Press any key to exit.[/]");
Console.ReadKey();

await game!.End();

await host!.StopAsync();

return 0;
