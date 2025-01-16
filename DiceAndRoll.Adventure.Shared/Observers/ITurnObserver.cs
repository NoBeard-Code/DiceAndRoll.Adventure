namespace DiceAndRoll.Adventure.Shared.Observers;

public interface ITurnObserver : IGrainObserver
{
    Task TurnFinished(string output);

    Task YourTurn(Guid playerId); // TODO: retrieve available turn options

    Task AbandonTurn(Guid playerId);
}
