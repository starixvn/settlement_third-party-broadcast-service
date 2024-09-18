using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualSportFive;

public class MissingResultValidatorTests
{
    private readonly IValidator _nextValidator;
    private readonly MissingResultValidator _validator;

    public MissingResultValidatorTests()
    {
        _nextValidator = Substitute.For<IValidator>();
        _validator = new MissingResultValidator();
        _validator.SetNext(_nextValidator);
    }

    [Fact]
    public void Validate_HtResultExistsFtResultNullWithFtScore_ShouldReturnError()
    {
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        ResourceItemModel ftResult = null;
        var matchInfo = new MatchInfo
        {
            HtHomeScore = null,
            HtAwayScore = null,
            FinalHomeScore = 2,
            FinalAwayScore = 2
        };

        var result = _validator.Validate(htResult, ftResult, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(4, result.ErrorCode);
        Assert.Contains("FT not exist", result.ErrorMessage);
    }

    [Fact]
    public void Validate_HtResultNullFtResultExistsWithHtScore_ShouldReturnError()
    {
        ResourceItemModel htResult = null;
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo
        {
            HtHomeScore = null,
            HtAwayScore = null
        };

        var result = _validator.Validate(htResult, ftResult, matchInfo);

        Assert.False(result.IsSuccessful);
        Assert.Equal(5, result.ErrorCode);
        Assert.Contains("HT not exist", result.ErrorMessage);
    }

    [Fact]
    public void Validate_ValidResults_ShouldCallNextValidator()
    {
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo
        {
            HtHomeScore = 1,
            HtAwayScore = 1,
            FinalHomeScore = 2,
            FinalAwayScore = 2
        };

        _validator.Validate(htResult, ftResult, matchInfo);

        _nextValidator.Received(1).Validate(htResult, ftResult, matchInfo);
    }

    [Fact]
    public void Validate_NoNextValidator_ShouldReturnSuccess()
    {
        var validator = new MissingResultValidator(); // No next validator set
        var htResult = new ResourceItemModel { Home = "1", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "2" };
        var matchInfo = new MatchInfo
        {
            HtHomeScore = 1,
            HtAwayScore = 1,
            FinalHomeScore = 2,
            FinalAwayScore = 2
        };

        var result = validator.Validate(htResult, ftResult, matchInfo);

        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.ErrorCode);
    }
}