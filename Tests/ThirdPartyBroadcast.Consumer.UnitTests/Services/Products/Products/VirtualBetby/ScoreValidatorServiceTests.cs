using ThirdPartyBroadcast.Consumer.Extensions;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualBetby;

public class ScoreValidatorServiceTests
{
    private readonly FullTimeScoreService fullTimeScoreService;

    private readonly ScoreValidatorServiceImp service;

    public ScoreValidatorServiceTests()
    {
        fullTimeScoreService = Substitute.For<FullTimeScoreService>();
        service = new ScoreValidatorServiceImp(fullTimeScoreService);
    }

    [Fact]
    public void Validate_NotEnoughQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"11\" Away=\"1\" /></Resources>"
        };
        request.Resources = request.SelectedResources.ToResources().Items;

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "1",
                Away = "1"
            },
            new()
            {
                Id = "Q2",
                Home = "1",
                Away = "0"
            },
            new()
            {
                Id = "Q3",
                Home = "1",
                Away = "2"
            },
            new()
            {
                Id = "Q4",
                Home = "4",
                Away = "5"
            },
            new()
            {
                Id = "00",
                Home = "7",
                Away = "8"
            }
        };
        fullTimeScoreService.CheckForAllQuarters(Arg.Any<List<ResourceItemModel>>()).Returns(false);
        var result = service.Validate(request, resources);
        const string expectedMessage = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate.";
        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void GetInconsistentMessage_ResourceFullTime_ReturnRightMessage()
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

        var result = service.GetInconsistentMessage(fullTime);
        const string expectedMessage = "FT(4 - 5)";
        Assert.Equal(expectedMessage, result);
    }

    [Fact]
    public void GetInconsistentMessage_ResourceQuarter_ReturnRightMessage()
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
            }
        };

        var result = service.GetInconsistentMessage(quarters);
        const string expectedMessage = "Q1(3 - 4), Q2(1 - 2), Q3(2 - 3), Q4(4 - 5), FT(10 - 14)";
        Assert.Equal(expectedMessage, result);
    }
}