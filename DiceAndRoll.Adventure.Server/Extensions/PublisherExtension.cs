using DiceAndRoll.Adventure.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace DiceAndRoll.Adventure.Server.Extensions;

internal class PublisherExtension<T> : IPublisherExtension<T> where T : IGrainObserver
{
    private readonly ObserverManager<T> _observerManager;

    public PublisherExtension(ILogger<T> logger, IGrainContext context)
    {
        _observerManager = new ObserverManager<T>(TimeSpan.FromMinutes(5), logger);
    }

    public Task Subscribe(T observer)
    {
        _observerManager.Subscribe(this, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(T observer)
    {
        _observerManager.Unsubscribe(this);
        return Task.CompletedTask;
    }

    public async Task Notify(Func<T, Task> notification)
    {
        await _observerManager.Notify(notification);
    }
}
