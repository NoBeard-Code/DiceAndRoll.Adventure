using DiceAndRoll.Adventure.Server.Services;
using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class GameGrain(ICampaignService campaignService) : Grain, IGameGrain
{
    private readonly List<IPlayerGrain> _players = [];
    private readonly List<ILocationGrain> locations = [];

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var directory = GrainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);
        await directory.RegisterGame(this.GetPrimaryKey());
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var directory = GrainFactory.GetGrain<IDirectoryGrain>(Guid.Empty);
        await directory.UnregisterGame(this.GetPrimaryKey());
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task ConfigureAndStart(string campaignFile)
    {
        var campaign = await campaignService.LoadAsync(campaignFile);

        var locations = await campaignService.GetLocationsAsync();

        // Initialize the game world using the game data
        foreach (var location in locations)
        {
            await AddLocationAsync(location);
        }
    }

    private async Task<ILocationGrain> AddLocationAsync(LocationInfo location)
    {
        var grain = GrainFactory.GetGrain<ILocationGrain>(location.Name);
        await grain.Configure(location);
        return grain;
    }

    public Task End()
    {
        return Task.CompletedTask;
    }

    public Task Join(IPlayerGrain player)
    {
        throw new NotImplementedException();
    }

    public Task Leave(IPlayerGrain player)
    {
        throw new NotImplementedException();
    }
}
