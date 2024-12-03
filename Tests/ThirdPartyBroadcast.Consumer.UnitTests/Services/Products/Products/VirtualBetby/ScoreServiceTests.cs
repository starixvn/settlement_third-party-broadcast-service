using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualBetby;

public class ScoreServiceTests
{
    private readonly ScoreServiceImp service = new();

    [Fact]
    public void GetScoreMessage_ResourceFullTime_ReturnRightMessage()
    {
        var fullTime = new List<ResourceItemModel>
        {
            new()
            {
                Id = ResourceId.FullTimeResourceId,
                Home = "4",
                Away = "5"
            }
        };

        var result = service.GetScoreMessage(fullTime, string.Empty);
        const string expectedMessage = "FT(4 - 5)";
        Assert.Equal(expectedMessage, result);
    }

    [Fact]
    public void GetScoreMessage_ResourceQuarter_ReturnRightMessage()
    {
        var quarters = new List<ResourceItemModel>
        {
            new()
            {
                Id = ResourceId.Quarter2,
                Home = "1",
                Away = "2"
            },
            new()
            {
                Id = ResourceId.Quarter3,
                Home = "2",
                Away = "3"
            },
            new()
            {
                Id = ResourceId.Quarter1,
                Home = "3",
                Away = "4"
            },
            new()
            {
                Id = ResourceId.Quarter4,
                Home = "4",
                Away = "5"
            },
            new()
            {
                Id = ResourceId.FullTimeResourceId,
                Home = "10",
                Away = "14"
            },
            new()
            {
                Id = ResourceId.HalfTimeResourceId,
                Home = "4",
                Away = "6"
            }
        };

        var result = service.GetScoreMessage(quarters, string.Empty);
        const string expectedMessage = "Q1(3 - 4), Q2(1 - 2), HT(4 - 6), Q3(2 - 3), Q4(4 - 5), FT(10 - 14)";
        Assert.Equal(expectedMessage, result);
    }

    [Fact]
    public void GetScoreMessage_ResourceQuarterAndRefund_ReturnRightMessage()
    {
        var quarters = new List<ResourceItemModel>
        {
            new()
            {
                Id = ResourceId.Quarter2,
                Home = "1",
                Away = "2"
            },
            new()
            {
                Id = ResourceId.Quarter3,
                Home = "2",
                Away = "3"
            },
            new()
            {
                Id = ResourceId.Quarter1,
                Home = "3",
                Away = "4"
            },
            new()
            {
                Id = ResourceId.Quarter4,
                Home = "4",
                Away = "5"
            },
            new()
            {
                Id = ResourceId.FullTimeResourceId,
                Home = "10",
                Away = "14"
            },
            new()
            {
                Id = ResourceId.HalfTimeResourceId,
                Home = "4",
                Away = "6"
            }
        };

        var result = service.GetScoreMessage(quarters, ActionName.Refund);
        const string expectedMessage = "Q1, Q2, HT, Q3, Q4, FT";
        Assert.Equal(expectedMessage, result);
    }
}