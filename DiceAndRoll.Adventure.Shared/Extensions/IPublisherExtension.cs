using DiceAndRoll.Adventure.Shared.Grains;

namespace DiceAndRoll.Adventure.Shared.Extensions;

public interface IPublisherExtension<T> : IGrainExtension, IObservableGrain<T> where T : IGrainObserver
{
    internal Task Notify(Func<T, Task> notification);
}
