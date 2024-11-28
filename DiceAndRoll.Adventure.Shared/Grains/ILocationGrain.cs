using DiceAndRoll.Adventure.Shared.Models;
using DiceAndRoll.Adventure.Shared.Observers;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface ILocationGrain : IGrainWithStringKey
{
    Task<LocationInfo?> GetInfo();

    Task Configure(LocationInfo data);

    Task Enter(string playerName);

    Task Exit(string playerName);

    Task<string> Describe(string playerName);

    Task<ILocationGrain?> ExitTo(string destination);

    Task<Dictionary<string, ILocationGrain>> GetConnections();

    Task Subscribe(ILocationObserver observer);
    Task Unsubscribe(ILocationObserver observer);
}
