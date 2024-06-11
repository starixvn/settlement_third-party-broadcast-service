using Microsoft.Extensions.Options;
using BusinessModels = Settlement.Business.Core.Models;

namespace ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

public interface SettleSitesService
{
    Task<Result> Settle(HappyFiveRequest request, List<BusinessModels.Resources> resources);
}

public class SettleSitesServiceImp(
    IOptions<AppSettingsKey> appSettingsKey,
    ProcessService processService) : SettleSitesService
{
    public async Task<Result> Settle(HappyFiveRequest request, List<BusinessModels.Resources> resources)
    {
        var tasks = appSettingsKey.Value
            .SiteDbServer
            .Select(site => processService.Settle(request, site, resources)).ToList();

        var results = await Task.WhenAll(tasks.ToArray());
        var resultFailed = results.Where(r => r.ErrCode > 0).ToList();

        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrMessage).ToArray())
        };
    }
}