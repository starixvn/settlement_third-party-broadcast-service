using Fanex.Caching;
using NSubstitute.ExceptionExtensions;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services;

public class FlagServiceTests
{
    private readonly ICache cache;

    public FlagServiceTests()
    {
        cache = Substitute.For<ICache>();
    }

    [Fact]
    public async Task IsDeployedFromCache_NotExistedKey_ReturnFalse()
    {
        const string key = CacheKeys.BroadcastFlagCacheKey;
        cache.ContainAsync(key).Returns(false);
        var service = new FlagServiceImp(cache);

        var result = await service.IsDeployedFromCache(key);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDeployedFromCache_IsExistedKeyAndTrue_ReturnTrue()
    {
        const string key = CacheKeys.BroadcastFlagCacheKey;
        cache.ContainAsync(key).Returns(true);
        cache.TryGetAsync<bool>(key).Returns((false, true));

        var service = new FlagServiceImp(cache);

        var result = await service.IsDeployedFromCache(key);

        Assert.True(result);
    }

    [Fact]
    public async Task IsDeployedFromCache_ExceptionGetCache_ReturnFalse()
    {
        cache.ContainAsync(Arg.Any<string>()).ThrowsAsync(new Exception());

        var service = new FlagServiceImp(cache);

        var result = await service.IsDeployedFromCache(CacheKeys.BroadcastFlagCacheKey);

        Assert.False(result);
    }
}