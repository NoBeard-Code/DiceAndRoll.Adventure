using DiceAndRoll.Adventure.Server.Models;
using DiceAndRoll.Adventure.Shared.Models;
using Newtonsoft.Json;

namespace DiceAndRoll.Adventure.Server.Services;

public class CampaignService : ICampaignService
{
    private Campaign? _campaign;

    public async Task<string> LoadAsync(string campaignFile)
    {
        // Read the contents of the game file and deserialize it
        var jsonData = await File.ReadAllTextAsync(campaignFile);
        var data = JsonConvert.DeserializeObject<Campaign>(jsonData)!;

        _campaign = data;
        return data.Name;
    }

    public Task<List<LocationInfo>> GetLocationsAsync()
    {
        return Task.FromResult(_campaign!.Locations);
    }
}
