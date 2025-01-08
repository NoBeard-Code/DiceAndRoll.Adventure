using DiceAndRoll.Adventure.Shared.Observers;
using Spectre.Console;

namespace DiceAndRoll.Adventure.ConsoleApp.Observers;

internal class PlayerObserver : IPlayerObserver
{
    public Task NotifyPlayer(string message)
    {
        AnsiConsole.MarkupLine(message);
        return Task.CompletedTask;
    }
}
