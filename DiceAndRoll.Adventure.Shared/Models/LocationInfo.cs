namespace DiceAndRoll.Adventure.Shared.Models;

[GenerateSerializer, Immutable]
public record LocationInfo(
    string Name, string Title, string Description, string Type, string Terrain, short X, short Y, List<string> ConnectsTo,
    List<string>? Enemies, short? EnemyMana, Reward? Reward);

[GenerateSerializer, Immutable]
public record Reward(short Mana);
