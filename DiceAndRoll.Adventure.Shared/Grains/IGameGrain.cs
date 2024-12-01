namespace DiceAndRoll.Adventure.Shared.Grains;

public interface IGameGrain : IGrainWithGuidCompoundKey
{
    Task ConfigureAndStart(string campaignFile);

    Task End();

    Task Join();

    Task Leave();
}
