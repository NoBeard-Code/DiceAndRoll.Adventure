using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Observers;

public interface ITurnObserver : IGrainObserver
{
    Task TurnFinished(string output);

    Task YourTurn(List<MonsterIdentification>? monsters); // TODO: retrieve available turn options
}
