using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualSportFive;

public class MatchScoreValidatorTests
{
    private readonly IValidator nextValidator;
    private readonly MatchScoreValidator validator;

    public MatchScoreValidatorTests()
    {
        nextValidator = Substitute.For<IValidator>();
        validator = new MatchScoreValidator();
        validator.SetNext(nextValidator);
    }

    [Fact]
    public void Validate_HtAndFtResults_ConsistentScores_ShouldReturnSuccess()
    {
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo();

        var result = validator.Validate(htResult, ftResult, matchInfo);

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Validate_HtAndFtResults_InconsistentScores_ShouldReturnError()
    {
        var htResult = new ResourceItemModel { Home = "3", Away = "3" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo();

        var result = validator.Validate(htResult, ftResult, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(3, result.ErrorCode);
        Assert.Contains("HT(3-3) is inconsistent with FT(2-2)", result.ErrorMessage);
    }

    [Fact]
    public void Validate_OnlyHtResult_ConsistentScore_ShouldReturnSuccess()
    {
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        var matchInfo = new MatchInfo { FinalHomeScore = 2, FinalAwayScore = 2 };
        nextValidator.Validate(Arg.Any<ResourceItemModel>(), Arg.Any<ResourceItemModel>(), Arg.Any<MatchInfo>())
            .Returns(new Result());
        var result = validator.Validate(htResult, null, matchInfo);

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Validate_OnlyHtResult_InconsistentScore_ShouldReturnError()
    {
        var htResult = new ResourceItemModel { Home = "3", Away = "3" };
        var matchInfo = new MatchInfo { FinalHomeScore = 2, FinalAwayScore = 2 };

        var result = validator.Validate(htResult, null, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(3, result.ErrorCode);
        Assert.Contains("HT(3-3) is inconsistent with FT(2-2)", result.ErrorMessage);
    }

    [Fact]
    public void Validate_OnlyFtResult_ConsistentScore_ShouldReturnSuccess()
    {
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo { HtHomeScore = 1, HtAwayScore = 1 };
        nextValidator.Validate(Arg.Any<ResourceItemModel>(), Arg.Any<ResourceItemModel>(), Arg.Any<MatchInfo>())
            .Returns(new Result());

        var result = validator.Validate(null, ftResult, matchInfo);

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Validate_OnlyFtResult_InconsistentScore_ShouldReturnError()
    {
        var ftResult = new ResourceItemModel { Home = "1", Away = "1" };
        var matchInfo = new MatchInfo { HtHomeScore = 2, HtAwayScore = 2 };

        var result = validator.Validate(null, ftResult, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(3, result.ErrorCode);
        Assert.Contains("HT(2-2) is inconsistent with FT(1-1)", result.ErrorMessage);
    }

    [Fact]
    public void Validate_NoResults_ShouldCallNextValidator()
    {
        validator.Validate(null, null, new MatchInfo());

        nextValidator.Received(1).Validate(null, null, Arg.Any<MatchInfo>());
    }
}