using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IPlayerGrain : IGrainWithGuidKey
{
    Task SetCharacter(string name, CharacterInfo character);

    Task Appear(string location);

    Task<string> Look();

    Task<string> GoTo(string location);

    Task<string> GoBack();

    Task<string> Kill(PlayerInfo targetPlayer);

    Task<string> Die();

    Task<Dictionary<string, string>> FindDirections();

    Task<List<PlayerInfo>> FindOthers();

    Task<bool> AmIDead();
}
