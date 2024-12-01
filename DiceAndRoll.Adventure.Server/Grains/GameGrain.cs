using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class GameGrain(ICampaignService campaignService) : Grain, IGameGrain
{
    private readonly List<IPlayerGrain> _players = [];
    private readonly List<ILocationGrain> locations = [];

    public async Task ConfigureAndStart(string campaignFile)
    {
        var campaign = await campaignService.LoadAsync(campaignFile);

        Console.WriteLine("Campaign selected: {0}", campaign);

        var locations = await campaignService.GetLocationsAsync();

        // Initialize the game world using the game data
        foreach (var location in locations)
        {
            await AddLocationAsync(location);
        }
    }

    private async Task<ILocationGrain> AddLocationAsync(LocationInfo location)
    {
        Console.WriteLine("Adding location: {0}", location.Name);

        var grain = this.GrainFactory.GetGrain<ILocationGrain>(location.Name);
        await grain.Configure(location);
        return grain;
    }

    public Task End()
    {
        // TODO: ...

        return Task.CompletedTask;
    }

    public Task Join()
    {
        throw new NotImplementedException();
    }

    public Task Leave()
    {
        throw new NotImplementedException();
    }
}
