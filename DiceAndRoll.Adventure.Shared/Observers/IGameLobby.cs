namespace DiceAndRoll.Adventure.Shared.Observers;

public interface IGameLobby : IGrainObserver
{
    Task GameRegistered(Guid gameId);

    Task GameUnregistered(Guid gameId);
}

