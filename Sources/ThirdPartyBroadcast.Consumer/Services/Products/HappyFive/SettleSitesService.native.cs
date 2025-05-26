using Fanex.SiteDbConnectionManager.Client.Services;
using BusinessModels = Settlement.Business.Core.Models;

namespace ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

public interface SettleSitesService
{
    Task<Result> Settle(HappyFiveRequest request, List<BusinessModels.Resources> resources);
}

public class SettleSitesServiceImp(
    SiteDbClientService siteDbClientService,
    SettleSiteHandler settleSiteHandler) : SettleSitesService
{
    public async Task<Result> Settle(HappyFiveRequest request, List<BusinessModels.Resources> resources)
    {
        var siteDbServer = await siteDbClientService.GetAllSiteDbAsync();
        var tasks = siteDbServer
            .Select(site => settleSiteHandler.SettleByServer(request, resources, site)).ToList();
        var results = await Task.WhenAll(tasks);
        var resultFailed = results.Where(r => r.ErrCode > 0).ToList();
        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrMessage).ToArray())
        };
    }
}