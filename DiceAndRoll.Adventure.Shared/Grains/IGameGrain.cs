using DiceAndRoll.Adventure.Shared.Observers;

namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IGameGrain : IGrainWithGuidKey, IObservableGrain<IGameObserver>
{
    Task ConfigureAndStart(string campaignFile);

    internal Task End();

    Task Join(IPlayerGrain player, bool randomLocation = false);

    Task Leave(IPlayerGrain player);
}
