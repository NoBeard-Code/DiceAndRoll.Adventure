namespace DiceAndRoll.Adventure.Shared.Models;

[GenerateSerializer, Immutable]
public record BattleStats
{
    public byte Health { get; set; }

    public byte Defense { get; set; }

    public byte Attack { get; set; }
}
