using ThirdPartyBroadcast.Consumer.Extensions;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualBetby;

public class FullTimeScoreValidatorTests
{
    private readonly ScoreService scoreService;

    private readonly IValidator validator;

    private readonly FullTimeScoreValidator service;

    public FullTimeScoreValidatorTests()
    {
        scoreService = Substitute.For<ScoreService>();
        validator = Substitute.For<IValidator>();
        service = new FullTimeScoreValidator(scoreService);
    }

    [Fact]
    public void Validate_NotEnoughQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            SelectedResources = "<Resources><Resource Id=\"00\" Home=\"12\" Away=\"2\" /><Resource Id=\"Q1\" Home=\"12\" Away=\"2\" /><Resource Id=\"Q4\" Home=\"4\" Away=\"3\" /></Resources>"
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
            }
        };

        scoreService.CheckForAllQuarters(Arg.Any<List<ResourceItemModel>>()).Returns(false);
        var result = service.Validate(request, resources);
        const string expectedMessage = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate.";

        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void Validate_FullTimeNotEqualQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            SelectedResources = "<Resources><Resource Id=\"00\" Home=\"12\" Away=\"2\" /> Id=\"Q1\" Home=\"12\" Away=\"2\" /><Resource Id=\"Q4\" Home=\"4\" Away=\"3\" /></Resources>"
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
                Id = "00",
                Home = "14",
                Away = "15"
            }
        };
        scoreService.GetMarkedList(Arg.Any<List<ResourceItemModel>>(), Arg.Any<List<ResourceItemModel>>())
            .Returns(resources);
        scoreService.CheckForAllQuarters(Arg.Any<List<ResourceItemModel>>()).Returns(true);
        scoreService.IsScoreEqualToQuarterScores(Arg.Any<List<ResourceItemModel>>(), Arg.Any<ResourceItemModel>()).Returns(false);
        scoreService.GetScoreMessage(Arg.Any<List<ResourceItemModel>>(), string.Empty).Returns("Q1(12 - 2), Q2(1 - 0), Q3(1 - 2), Q4(4 - 3), FT(14 - 15)");
        var result = service.Validate(request, resources);
        const string expectedMessage = "Inconsistent value Q1(12 - 2), Q2(1 - 0), Q3(1 - 2), Q4(4 - 3), FT(14 - 15).";

        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void Validate_FullTimeHomeEqualAway_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            SelectedResources = "<Resources><Resource Id=\"00\" Home=\"15\" Away=\"15\" /></Resources>"
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
                Id = "00",
                Home = "15",
                Away = "15"
            }
        };
        scoreService.GetMarkedList(Arg.Any<List<ResourceItemModel>>(), Arg.Any<List<ResourceItemModel>>())
            .Returns(resources);
        scoreService.CheckForAllQuarters(Arg.Any<List<ResourceItemModel>>()).Returns(true);

        var result = service.Validate(request, resources);
        const string expectedMessage = "Cannot calculate FT(15 - 15) due to FT should not be draw.";

        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void Validate_PassedAllValidate_ReturnTrue()
    {
        var request = new VirtualBetbyRequest
        {
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"12\" Away=\"2\" /><Resource Id=\"01\" Home=\"4\" Away=\"3\" /></Resources>"
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
                Id = "01",
                Home = "1",
                Away = "0"
            }
        };
        var newResources = new List<ResourceItemModel>
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
            }
        };
        scoreService.GetMarkedList(request.Resources, Arg.Any<IEnumerable<ResourceItemModel>>()).Returns(resources);
        scoreService.CheckForAllQuarters(Arg.Any<List<ResourceItemModel>>()).Returns(true);
        scoreService.IsScoreEqualToQuarterScores(Arg.Any<IEnumerable<ResourceItemModel>>(), Arg.Any<ResourceItemModel>()).Returns(true);

        validator.Validate(request, newResources).Returns(new Result());
        service.SetNext(validator);
        var result = service.Validate(request, newResources);

        Assert.True(result.IsSuccessful);
    }
}