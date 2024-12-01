using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Models;

public record Campaign(string Name, List<LocationInfo> Locations);

