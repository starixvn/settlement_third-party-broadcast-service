namespace ThirdPartyBroadcast.Consumer.Services;

public interface FlagService
{
    Task<bool> IsDeployedFromCache(string key);
}

public class FlagServiceImp : FlagService
{
    private readonly ICache cache;

    public FlagServiceImp(ICache cache)
    {
        this.cache = cache;
    }

    public async Task<bool> IsDeployedFromCache(string key)
    {
        try
        {
            var isExistedKey = await cache.ContainAsync(key);
            if (isExistedKey)
            {
                var (_, value) = await cache.TryGetAsync<bool>(key);
                return value;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}