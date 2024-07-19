using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Extensions;

public class ResourceItemModelExtensionsTests
{
    [Fact]
    public void TryParseScore_ValidScores_ReturnsTrue()
    {
        // Arrange
        var resourceItem = new ResourceItemModel { Home = "3", Away = "2" };
        var expectedScore = new Score(3, 2);

        // Act
        var result = resourceItem.TryParseScore(out var actualScore);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedScore, actualScore);
    }

    [Fact]
    public void TryParseScore_NullResourceItem_ReturnsFalse()
    {
        // Arrange
        ResourceItemModel resourceItem = null;

        // Act
        var result = resourceItem.TryParseScore(out var actualScore);

        // Assert
        Assert.False(result);
        Assert.Equal(new Score(0, 0), actualScore);
    }

    [Fact]
    public void TryParseScore_InvalidHomeScore_ReturnsFalse()
    {
        // Arrange
        var resourceItem = new ResourceItemModel { Home = "invalid", Away = "2" };

        // Act
        var result = resourceItem.TryParseScore(out var actualScore);

        // Assert
        Assert.False(result);
        Assert.Equal(new Score(0, 0), actualScore);
    }

    [Fact]
    public void TryParseScore_InvalidAwayScore_ReturnsFalse()
    {
        // Arrange
        var resourceItem = new ResourceItemModel { Home = "3", Away = "invalid" };

        // Act
        var result = resourceItem.TryParseScore(out var actualScore);

        // Assert
        Assert.False(result);
        Assert.Equal(new Score(0, 0), actualScore);
    }

    [Fact]
    public void TryParseScore_InvalidBothScores_ReturnsFalse()
    {
        // Arrange
        var resourceItem = new ResourceItemModel { Home = "invalid", Away = "invalid" };

        // Act
        var result = resourceItem.TryParseScore(out var actualScore);

        // Assert
        Assert.False(result);
        Assert.Equal(new Score(0, 0), actualScore);
    }
}