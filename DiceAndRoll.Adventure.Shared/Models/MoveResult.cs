namespace DiceAndRoll.Adventure.Shared.Models;

public enum MoveResultType
{
    Undetermined = 0,
    MonstersMove,
    PlayersMove,
    Played,
    Skipped,
    BattleEnded
}

[GenerateSerializer]
public record MoveResult
{
    public Guid? BelligerentId { get; set; } 

    public string? Output { get; set; }

    public required MoveResultType Result { get; set; }
}
