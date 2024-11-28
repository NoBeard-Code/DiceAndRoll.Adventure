using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface ILocationGrain : IGrainWithStringKey
{
    Task Configure(LocationInfo data);

    Task Enter(string playerName);

    Task Exit(string playerName);

    Task<string> Describe();

    Task<ILocationGrain?> ExitTo(string destination);
}
