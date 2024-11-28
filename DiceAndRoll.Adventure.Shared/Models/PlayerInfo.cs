namespace DiceAndRoll.Adventure.Shared.Models;

[GenerateSerializer, Immutable]
public record class PlayerInfo(Guid Key, string? Name);
