using DiceAndRoll.Adventure.Shared.Observers;

namespace DiceAndRoll.Adventure.ConsoleApp.Observers;

public class GameLobby : IGameLobby
{
    public List<Guid> Games { get; set; } = [];

    public Task GameRegistered(Guid gameId)
    {
        Games.Add(gameId);
        return Task.CompletedTask;
    }

    public Task GameUnregistered(Guid gameId)
    {
        Games.Remove(gameId);
        return Task.CompletedTask;
    }
}