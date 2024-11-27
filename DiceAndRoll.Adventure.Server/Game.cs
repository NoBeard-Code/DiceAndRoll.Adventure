using DiceAndRoll.Adventure.Server.Models;
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
            if (location == null) continue;
            await AddLocationAsync(location);
        }
    }

    private async Task AddLocationAsync(Location location)
    {
        Console.WriteLine("Adding location: {0}", location.Name);

        //var grain = grainFactory.GetGrain<ILocationGrain>();
    }
}
