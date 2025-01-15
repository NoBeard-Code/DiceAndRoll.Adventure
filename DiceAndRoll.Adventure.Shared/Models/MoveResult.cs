namespace DiceAndRoll.Adventure.Shared.Models;

public enum MoveResultType
{
    MonstersMove,
    PlayersMove,
    Skipped,
    BattleEnded
}

[GenerateSerializer]
public record MoveResult
{
    public required string Output { get; set; }

    public MoveResultType Result { get; set; }
}
