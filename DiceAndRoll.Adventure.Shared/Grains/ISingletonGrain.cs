namespace DiceAndRoll.Adventure.Shared.Grains;

/// <summary>
/// This is a marker interface for grains that act like a singleton, 
/// i.e. as a directory for fetching available grains, without knowing their keys upfront.
/// It's called with providing a Guid.Empty as its primary key.
/// </summary>
public interface ISingletonGrain
{
}
