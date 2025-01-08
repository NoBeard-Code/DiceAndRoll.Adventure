namespace DiceAndRoll.Adventure.Shared.Observers;

public interface IGameObserver : IGrainObserver
{
    Task PlayerJoined(string player);

    Task PlayerLeft(string player);

    Task GameOver();
}
