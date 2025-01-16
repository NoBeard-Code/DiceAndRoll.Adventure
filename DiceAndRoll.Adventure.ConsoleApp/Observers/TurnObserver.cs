using DiceAndRoll.Adventure.Shared.Observers;

namespace DiceAndRoll.Adventure.ConsoleApp.Observers;

internal class TurnObserver(Guid me, Action<string> turnFinished, Action yourTurnAction, Action abandonTurnAction) : ITurnObserver
{
    public Task TurnFinished(string output)
    {
        turnFinished(output);
        return Task.CompletedTask;
    }

    public Task YourTurn(Guid playerId)
    {
        if (me == playerId)
            yourTurnAction();

        return Task.CompletedTask;
    }

    public Task AbandonTurn(Guid playerId)
    {
        if (me == playerId)
            abandonTurnAction();

        return Task.CompletedTask;
    }
}
