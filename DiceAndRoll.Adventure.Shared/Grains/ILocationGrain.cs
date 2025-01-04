using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

internal interface ILocationGrain : IGrainWithStringKey
{
    internal Task<LocationInfo?> GetInfo();

    internal Task Configure(LocationInfo data, List<MonsterInfo> monsters);

    internal Task Enter(PlayerInfo player);

    internal Task Exit(PlayerInfo player);

    internal Task<string> Describe(PlayerInfo toMe);

    internal Task<ILocationGrain?> ExitTo(string destination);

    internal Task<Dictionary<string, ILocationGrain>> GetConnections();

    internal Task<List<PlayerInfo>> GetPlayers();

    internal Task<IEncounterGrain> EngageInEncounter(IPlayerGrain player);

    internal Task EndEncounter();
}
