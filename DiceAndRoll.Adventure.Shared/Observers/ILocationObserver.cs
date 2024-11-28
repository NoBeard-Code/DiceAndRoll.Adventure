namespace DiceAndRoll.Adventure.Shared.Observers;

public interface ILocationObserver : IGrainObserver
{
    Task PlayerEnteredLocation(string playerName);
}
