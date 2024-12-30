namespace DiceAndRoll.Adventure.Shared.Models;

[GenerateSerializer, Immutable]
public record MonsterInfo(
    string Name, string Title, string Story, string Description, byte Health, byte Defense, byte Attack, string Type,
    string Weapon, List<string> Specials);
