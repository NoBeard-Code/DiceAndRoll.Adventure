namespace DiceAndRoll.Adventure.Shared.Models;

[GenerateSerializer, Immutable]
public record CharacterInfo(
    string Name, string Title, string Story, string Description, byte Health, byte Defense, byte Attack, string Type,
    List<string> Armors, List<string> Weapons, List<string> Specials, string Armor, string Weapon);
