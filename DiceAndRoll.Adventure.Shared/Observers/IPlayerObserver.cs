namespace DiceAndRoll.Adventure.Shared.Observers;

public interface IPlayerObserver : IGrainObserver
{
    Task NotifyPlayer(string message);
}
