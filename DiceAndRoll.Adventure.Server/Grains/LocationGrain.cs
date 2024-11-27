using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.Shared.Models;

namespace DiceAndRoll.Adventure.Server.Grains;

internal sealed class LocationGrain(
    [PersistentState("location", "game")] IPersistentState<LocationInfo> state) 
    : Grain, ILocationGrain
{
    private readonly Dictionary<string, ILocationGrain> _connections = [];

    public Task ConfigureAsync(LocationInfo info)
    {
        if (state.RecordExists) 
            return Task.CompletedTask; 

        state.State = info;
        state.WriteStateAsync();

        foreach (var connection in info.ConnectsTo)
        {
            var grain = GrainFactory.GetGrain<ILocationGrain>(connection);
            if (grain != null)
            {
                _connections.Add(connection, grain);
            }
        }

        return Task.CompletedTask;
    }
}
