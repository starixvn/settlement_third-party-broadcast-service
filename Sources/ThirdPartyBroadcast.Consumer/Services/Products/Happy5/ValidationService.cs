namespace ThirdPartyBroadcast.Consumer.Services.Products.Happy5;

using Models.Happy5;

public interface ValidationService
{
    Task<Result> IsBlockingMatch(int matchId);

    Task<ResourcesModel> GetResource(int matchId, int sportType);

    Task<int> GetMaxPayoutSetting(int siteGroupId);

    Task<List<WinLimit>> GetMaxWinLimit(string siteGroupId);
}

public class ValidationServiceImp : ValidationService
{
    public async Task<Result> IsBlockingMatch(int matchId)
    {
        return new Result();
    }

    public async Task<ResourcesModel> GetResource(int matchId, int sportType)
    {
        var resource = new ResourcesModel();
        return resource;
    }

    public async Task<int> GetMaxPayoutSetting(int siteGroupId)
    {
        return 1;
    }

    public async Task<List<WinLimit>> GetMaxWinLimit(string siteGroupId)
    {
        var maxWinLimit = new List<WinLimit>();
        return maxWinLimit;
    }
}