using DiceAndRoll.Adventure.Server.Models;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;
using Newtonsoft.Json;

namespace DiceAndRoll.Adventure.Server;

internal sealed class Game(IGrainFactory grainFactory)
{
    internal async Task StartAsync(string campaignFile)
    {
        // Read the contents of the game file and deserialize it
        var jsonData = await File.ReadAllTextAsync(campaignFile);
        var data = JsonConvert.DeserializeObject<Campaign>(jsonData)!;

        Console.WriteLine("Campaign selected: {0}", data.Name);

        // Initialize the game world using the game data
        foreach (var location in data.Locations)
        {
            await AddLocationAsync(location);
        }
    }

    private async Task<ILocationGrain> AddLocationAsync(LocationInfo location)
    {
        Console.WriteLine("Adding location: {0}", location.Name);

        var grain = grainFactory.GetGrain<ILocationGrain>(location.Name);
        await grain.Configure(location);
        return grain;
    }
}
