using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualBetby;

public class FullTimeScoreServiceTests
{
    private readonly FullTimeScoreServiceImp service = new();

    [Fact]
    public void CheckForAllQuarters_Always_ReturnTrue()
    {
        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = ResourceId.Quarter1
            },
            new()
            {
                Id = ResourceId.Quarter2
            },
            new()
            {
                Id = ResourceId.Quarter3
            },
            new()
            {
                Id = ResourceId.Quarter4
            }
        };
        var result = service.CheckForAllQuarters(resources);

        Assert.True(result);
    }

    [Fact]
    public void IsFullTimeEqualToQuarterScores_Always_ReturnTrue()
    {
        var quarters = new List<ResourceItemModel>
        {
            new()
            {
                Id = ResourceId.Quarter1,
                Home = "1",
                Away = "2"
            },
            new()
            {
                Id = ResourceId.Quarter2,
                Home = "2",
                Away = "3"
            },
            new()
            {
                Id = ResourceId.Quarter3,
                Home = "3",
                Away = "4"
            },
            new()
            {
                Id = ResourceId.Quarter4,
                Home = "4",
                Away = "5"
            }
        };

        var fullTime = new ResourceItemModel
        {
            Id = ResourceId.FullTimeResourceId,
            Home = "10",
            Away = "14"
        };
        var result = service.IsFullTimeEqualToQuarterScores(quarters, fullTime);

        Assert.True(result);
    }
}