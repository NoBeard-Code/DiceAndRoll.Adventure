using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface ILocationGrain : IGrainWithStringKey
{
    Task Configure(LocationInfo data);

    Task Enter(string playerName);
}
