using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Services;

public interface ICampaignService
{
    internal Task<string> LoadAsync(string campaignFile);

    internal Task<List<LocationInfo>> GetLocationsAsync();
}
