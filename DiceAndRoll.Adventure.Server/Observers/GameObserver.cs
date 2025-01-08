using DiceAndRoll.Adventure.Shared.Observers;
using Spectre.Console;

namespace DiceAndRoll.Adventure.Server.Observers;

internal class GameObserver : IGameObserver
{
    public bool IsOver { get; private set; }

    public Task GameOver()
    {
        AnsiConsole.MarkupLine($"[olive]Game ended.[/]");

        IsOver = true;
        return Task.CompletedTask;
    }

    public Task PlayerJoined(string player)
    {
        AnsiConsole.MarkupLine($"[olive]New player joined the game: {player}.[/]");
        return Task.CompletedTask;
    }

    public Task PlayerLeft(string player)
    {
        AnsiConsole.MarkupLine($"[olive]The player left the game: {player}.[/]");
        return Task.CompletedTask;
    }
}
