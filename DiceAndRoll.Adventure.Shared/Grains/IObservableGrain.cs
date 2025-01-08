namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IObservableGrain<T> where T : IGrainObserver
{
    Task Subscribe(T observer);

    Task Unsubscribe(T observer);
}
