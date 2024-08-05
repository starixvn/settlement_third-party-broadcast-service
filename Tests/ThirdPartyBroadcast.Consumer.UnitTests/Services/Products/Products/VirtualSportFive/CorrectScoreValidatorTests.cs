using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualSportFive;

public class CorrectScoreValidatorTests
{
    private readonly IValidator _nextValidator;
    private readonly CorrectScoreValidator _validator;

    public CorrectScoreValidatorTests()
    {
        _nextValidator = Substitute.For<IValidator>();
        _validator = new CorrectScoreValidator();
        _validator.SetNext(_nextValidator);
    }

    [Fact]
    public void Validate_InvalidHtScore_ShouldReturnError()
    {
        var htResult = new ResourceItemModel { Home = "invalid", Away = "1" };
        var matchInfo = new MatchInfo();

        var result = _validator.Validate(htResult, null, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(1, result.ErrorCode);
        Assert.Contains("Invalid HT score", result.ErrorMessage);
    }

    [Fact]
    public void Validate_InvalidFtScore_ShouldReturnError()
    {
        var ftResult = new ResourceItemModel { Home = "2", Away = "invalid" };
        var matchInfo = new MatchInfo();

        var result = _validator.Validate(null, ftResult, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(2, result.ErrorCode);
        Assert.Contains("Invalid FT score", result.ErrorMessage);
    }

    [Fact]
    public void Validate_ValidScores_ShouldCallNextValidator()
    {
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo();

        _validator.Validate(htResult, ftResult, matchInfo);

        _nextValidator.Received(1).Validate(htResult, ftResult, matchInfo);
    }

    [Fact]
    public void Validate_ValidScores_NoNextValidator_ShouldReturnSuccess()
    {
        var validator = new CorrectScoreValidator(); // No next validator set
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo();

        var result = validator.Validate(htResult, ftResult, matchInfo);

        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.ErrorCode);
    }
}