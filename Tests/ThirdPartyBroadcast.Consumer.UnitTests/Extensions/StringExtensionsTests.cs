using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void ToResources_ValidXml_ReturnsResourcesModel()
    {
        // Arrange
        string xml = @"<Resources>
                            <Resource Id=""01"" Home=""1"" Away=""3"" />
                      </Resources>";

        // Act
        var result = xml.ToResources();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("01", result.Items[0].Id);
    }

    [Fact]
    public void ToResources_InValidXml_ThrowsInvalidOperationException()
    {
        // Arrange
        string xml = @"";

        // Assert
        Assert.Throws<InvalidOperationException>(() => xml.ToResources());
    }
}