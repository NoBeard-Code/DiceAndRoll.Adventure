using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;

namespace DiceAndRoll.Adventure.ConsoleApp.Observers;

internal class TurnObserver(Action<string> turnFinished, Action<List<MonsterIdentification>?> yourTurnAction) : ITurnObserver
{
    public Task TurnFinished(string output)
    {
        turnFinished(output);
        return Task.CompletedTask;
    }

    //public Task YourTurn()
    //{
    //    yourTurnAction();
    //    return Task.CompletedTask;
    //}

    public Task YourTurn(List<MonsterIdentification>? monsters)
    {
        yourTurnAction(monsters);
        return Task.CompletedTask;
    }
}
