using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

internal interface ILocationGrain : IGrainWithStringKey
{
    Task<LocationInfo?> GetInfo();

    Task Configure(LocationInfo data, List<MonsterInfo> monsters);

    Task Enter(PlayerInfo player);

    Task Exit(PlayerInfo player);

    Task<string> Describe(PlayerInfo toMe);

    Task<ILocationGrain?> ExitTo(string destination);

    Task<Dictionary<string, ILocationGrain>> GetConnections();

    Task<List<PlayerInfo>> GetPlayers();

    Task<IEncounterGrain> EngageInEncounter(IPlayerGrain player);







    Task LeaveEncounter(IPlayerGrain player);

    Task<IEncounterGrain?> GetEncounterReference();
}
