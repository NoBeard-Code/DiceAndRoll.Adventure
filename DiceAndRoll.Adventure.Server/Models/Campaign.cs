using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Models;

internal record Campaign(string Name, List<LocationInfo> Locations);

