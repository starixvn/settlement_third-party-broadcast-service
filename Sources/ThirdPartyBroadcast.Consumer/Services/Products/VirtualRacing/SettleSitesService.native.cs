using Microsoft.Extensions.Options;
using Settlement.Business.Core.Models;
using Result = ThirdPartyBroadcast.Consumer.Models.Result;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

public interface SettleSitesService
{
    Task<Result> Settle(VirtualRacingRequest request, IEnumerable<Outright> outright, IEnumerable<Resources> resources);
}

public class SettleSitesServiceImp(
    IOptions<AppSettingsKey> appSettingsKey,
    ProcessService processService) : SettleSitesService
{
    public async Task<Result> Settle(VirtualRacingRequest request, IEnumerable<Outright> outright, IEnumerable<Resources> resources)
    {
        var tasks = appSettingsKey.Value
            .SiteDbServer
            .Select(site => processService.Settle(request, site, outright, resources)).ToList();

        var results = await Task.WhenAll(tasks.ToArray());
        var resultFailed = results.Where(r => r.ErrCode > 0).ToList();

        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrMessage).ToArray())
        };
    }
}