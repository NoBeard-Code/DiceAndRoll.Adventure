namespace DiceAndRoll.Adventure.Server.Models;

internal record Campaign(string Name, List<Location> Locations);

internal record Location(string Name, string Title, string Description, string Type, string Terrain, short X, short Y, List<string> ConnectsTo);
