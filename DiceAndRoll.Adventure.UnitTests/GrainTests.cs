using DiceAndRoll.Adventure.Shared.Grains;
using DiceAndRoll.Adventure.UnitTests.Fixtures;

namespace DiceAndRoll.Adventure.UnitTests;

public class GrainTests(ClusterFixture cluster) : IClassFixture<ClusterFixture>
{
    [Fact]
    public async Task TestSingleGrain()
    {
        var grain = cluster.GrainFactory.GetGrain<IPlayerGrain>(Guid.NewGuid(), string.Empty);
        var result = await grain.Look();

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    [InlineData(1000000)]
    public async Task TestMultipleGrains(int numberOfGrains)
    {
        for (int i = 0; i < numberOfGrains; i++)
        {
            var grain = cluster.GrainFactory.GetGrain<IPlayerGrain>(Guid.NewGuid(), string.Empty);
            var result = await grain.Look();
            Assert.NotNull(result);
        }
    }
}
