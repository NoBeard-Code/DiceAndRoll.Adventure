using Orleans.Concurrency;

namespace DiceAndRoll.Adventure.Shared.Observers;

public interface ILocationObserver : IGrainObserver
{
    [OneWay]
    Task PlayerEnteredLocation(string playerName);

    [OneWay]
    Task PlayerExitedLocation(string playerName);
}
