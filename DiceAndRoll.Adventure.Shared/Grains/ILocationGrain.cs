using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface ILocationGrain : IGrainWithStringKey
{
    Task ConfigureAsync(LocationInfo data);
}
