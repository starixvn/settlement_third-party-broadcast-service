using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Extensions;

public class ScoreValidatorTests
{
    [Fact]
    public void ValidateScore_ReturnsFalse_WhenFtResultScoreIsLessThanHtResultScore()
    {
        // Arrange
        var htResult = new ResourceItemModel { Home = "2", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "1", Away = "0" };
        var matchInfo = new MatchInfo { HtHomeScore = 2, HtAwayScore = 1 };

        // Act
        var result = ScoreValidator.ValidateScore(htResult, ftResult, matchInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateScore_ReturnsTrue_WhenHtResultIsNull()
    {
        // Arrange
        ResourceItemModel htResult = null;
        var ftResult = new ResourceItemModel { Home = "1", Away = "0" };
        var matchInfo = new MatchInfo { HtHomeScore = null, HtAwayScore = null };

        // Act
        var result = ScoreValidator.ValidateScore(htResult, ftResult, matchInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateScore_ReturnsFalse_WhenHtResultScoreIsGreaterThanFtResultScore()
    {
        // Arrange
        var htResult = new ResourceItemModel { Home = "2", Away = "1" };
        var ftResult = new ResourceItemModel { Home = "1", Away = "0" };
        var matchInfo = new MatchInfo { HtHomeScore = 1, HtAwayScore = 0 };

        // Act
        var result = ScoreValidator.ValidateScore(htResult, ftResult, matchInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateScore_ReturnsTrue_WhenAllConditionsAreMet()
    {
        // Arrange
        var htResult = new ResourceItemModel { Home = "1", Away = "0" };
        var ftResult = new ResourceItemModel { Home = "2", Away = "0" };
        var matchInfo = new MatchInfo { HtHomeScore = 1, HtAwayScore = 0 };

        // Act
        var result = ScoreValidator.ValidateScore(htResult, ftResult, matchInfo);

        // Assert
        Assert.True(result);
    }
}