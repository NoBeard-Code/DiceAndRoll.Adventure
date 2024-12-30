using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal class EncounterState
{
    public List<MonsterInfo> Monsters { get; set; } = [];
}

internal sealed class EncounterGrain : IEncounterGrain
{
}
