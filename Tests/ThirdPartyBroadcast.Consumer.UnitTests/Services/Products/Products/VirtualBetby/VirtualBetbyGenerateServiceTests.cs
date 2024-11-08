using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualBetby;

public class VirtualBetbyGenerateServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly GenerateMatchInfoService generateMatchInfoService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly ScoreService scoreService;

    private readonly VirtualBetbyGenerateServiceImp service;

    public VirtualBetbyGenerateServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        generateMatchInfoService = Substitute.For<GenerateMatchInfoService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        scoreService = Substitute.For<ScoreService>();
        service = new VirtualBetbyGenerateServiceImp(repository, generateMatchInfoService, logDirectlyService, scoreService);
    }

    [Fact]
    public void ValidateResources_EmptyQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            SelectedResources = "<Resources></Resources>"
        };

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        Assert.Equal(ErrorMessage.NotExistValue, result.ErrorMessage);
    }

    [Fact]
    public void ValidateResources_HalfTimeScoreValidatorAndActionIsCalcNotEnoughQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"12\" Away=\"2\" /><Resource Id=\"01\" Home=\"4\" Away=\"3\" /></Resources>"
        };

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        Assert.Equal("Cannot Calculate HT due to related quarters have not been calculated or selected to calculate.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateResources_HalfTimeScoreValidatorAndActionIsCalcHalftimeNotEqualQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"1\" Away=\"1\" /><Resource Id=\"Q2\" Home=\"2\" Away=\"2\" /><Resource Id=\"Q3\" Home=\"3\" Away=\"3\" /><Resource Id=\"01\" Home=\"4\" Away=\"3\" /></Resources>"
        };

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        Assert.Equal("Inconsistent value Q1(1 - 1), Q2(2 - 2).", result.ErrorMessage);
    }

    [Fact]
    public void ValidateResources_HalfTimeScoreValidatorAndActionIsCalcHalftimeEqualQuarter_ReturnTrue()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q2\" Home=\"2\" Away=\"2\" /><Resource Id=\"Q3\" Home=\"3\" Away=\"3\" /><Resource Id=\"01\" Home=\"3\" Away=\"3\" /></Resources>"
        };

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "1",
                Away = "1"
            }
        };

        var result = service.ValidateResources(request, resources);

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ValidateResources_HalfTimeScoreValidatorAndActionIsNotResourceHt_ReturnTwoResource()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q2\" Home=\"2\" Away=\"2\" /><Resource Id=\"Q3\" Home=\"3\" Away=\"3\" /></Resources>"
        };

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "1",
                Away = "1"
            }
        };

        var result = service.ValidateResources(request, resources);

        Assert.True(result.IsSuccessful);

        Assert.Equal(2, request.Resources.Count);
    }

    [Fact]
    public void ValidateResources_FullTimeScoreValidatorAndActionIsCalcNotEnoughQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"2\" Away=\"2\" /><Resource Id=\"00\" Home=\"3\" Away=\"3\" /></Resources>"
        };

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
                Id = "01",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        const string expectedMessage = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate.";

        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void ValidateResources_FullTimeScoreValidatorAndActionIsCalcFullTimeHomeEqualAway_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"2\" Away=\"2\" /><Resource Id=\"00\" Home=\"3\" Away=\"3\" /></Resources>"
        };

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
                Home = "1",
                Away = "2"
            },
            new()
            {
                Id = "01",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        const string expectedMessage = "Cannot calculate FT(3 - 3) due to FT should not be draw.";

        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void ValidateResources_FullTimeScoreValidatorAndActionIsCalcFullTimeNotEqualQuarter_ReturnErrorMessage()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"2\" Away=\"2\" /><Resource Id=\"00\" Home=\"3\" Away=\"5\" /></Resources>"
        };

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
                Home = "1",
                Away = "2"
            },
            new()
            {
                Id = "01",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        const string expectedMessage = "Inconsistent value Q1(2 - 2), Q2(1 - 0), Q3(1 - 2), Q4(1 - 2), FT(3 - 5).";

        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void ValidateResources_FullTimeScoreValidatorAndActionIsCalcFullTimeEqualQuarter_ReturnTrue()
    {
        var request = new VirtualBetbyRequest
        {
            MatchId = 1,
            Action = "Cal",
            SelectedResources = "<Resources><Resource Id=\"Q1\" Home=\"1\" Away=\"2\" /><Resource Id=\"Q4\" Home=\"4\" Away=\"5\" /><Resource Id=\"OT\" Home=\"\" Away=\"\" /><Resource Id=\"01\" Home=\"3\" Away=\"5\" /><Resource Id=\"00\" Home=\"10\" Away=\"14\" /></Resources>"
        };

        var resources = new List<ResourceItemModel>
        {
            new()
            {
                Id = "Q1",
                Home = "5",
                Away = "10"
            },
            new()
            {
                Id = "Q2",
                Home = "2",
                Away = "3"
            },
            new()
            {
                Id = "Q3",
                Home = "3",
                Away = "4"
            },
            new()
            {
                Id = "Q4",
                Home = "10",
                Away = "2"
            },
            new()
            {
                Id = "01",
                Home = "4",
                Away = "5"
            }
        };

        var result = service.ValidateResources(request, resources);

        Assert.True(result.IsSuccessful);
    }
}