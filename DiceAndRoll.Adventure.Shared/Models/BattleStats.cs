namespace DiceAndRoll.Adventure.Shared.Models;

[GenerateSerializer]
public class BattleStats
{
    public byte Health { get; set; }

    public byte Defense { get; set; }

    public byte Attack { get; set; }
}
