using Microsoft.Extensions.Options;

namespace ThirdPartyBroadcast.Consumer.Services.Products.Happy5;

public interface SettleSitesService
{
    Task<Result> Settle(int matchId, int sportType);
}

public class SettleSitesServiceImp(
    IOptions<AppSettingsKey> appSettingsKey,
    ProcessService processService) : SettleSitesService
{
    public async Task<Result> Settle(int matchId, int sportType)
    {
        var tasks = appSettingsKey.Value
            .SiteDbServer
            .Select(s => processService.Settle(matchId, sportType)).ToList();

        var results = await Task.WhenAll(tasks.ToArray());
        var resultFailed = results.Where(r => r.ErrorCode > 0).ToList();
        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrorMessage).ToArray())
        };
    }
}