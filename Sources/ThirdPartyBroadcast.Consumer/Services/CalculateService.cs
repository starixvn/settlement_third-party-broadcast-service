using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface CalculateService
{
    Task<Result> Execute(CalculateCriteria criteria, SiteDbPendingCommand command);

    Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria);
}

public class CalculateServiceImp(
    IDynamicRepository repository,
    SiteDbClientService siteDbClientService,
    SiteDbPendingCommandService pendingCommandService)
    : CalculateService
{
    public async Task<Result> Execute(CalculateCriteria criteria, SiteDbPendingCommand command)
    {
        var siteDbServer = await siteDbClientService.GetAllSiteDbAsync();

        var tasks = siteDbServer
            .Select(site => CalculateByServer(site, criteria, command))
            .ToList();

        var results = await Task.WhenAll(tasks.ToArray());
        var resultFailed = results.Where(r => r.ErrorCode > 0).ToList();
        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrorMessage).ToArray())
        };
    }

    private async Task<Result> CalculateByServer(SiteDb site, CalculateCriteria criteria, SiteDbPendingCommand command)
    {
        var isActive = await siteDbClientService.IsSiteDbActiveAsync(site.SiteDbGroupId);
        var pendingCommand = await pendingCommandService.CheckingPendingCommand(command.MatchId, site.SiteDbGroupId);

        if (isActive && pendingCommand.ErrorCode == 0)
        {
            return await ProcessActiveSite(site, criteria);
        }

        return await ProcessInactiveSite(site.SiteDbGroupId, command);
    }

    private async Task<Result> ProcessActiveSite(SiteDb site, CalculateCriteria criteria)
    {
        criteria.DbServer = site.SiteName;
        await repository.ExecuteAsync(criteria);

        return new Result
        {
            ErrorCode = criteria.ErrCode,
            ErrorMessage = $"{site.SiteName}: {criteria.ErrCode} - {criteria.ErrMsg}"
        };
    }

    private async Task<Result> ProcessInactiveSite(int siteDbGroupId, SiteDbPendingCommand command)
    {
        command.SiteDBGroupId = siteDbGroupId;
        var result = await pendingCommandService.SavePendingCommand(command);

        return new Result
        {
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria)
    {
        var siteDbList = await siteDbClientService.GetActiveSiteDbAsync();

        var tasks = siteDbList.Select(async site =>
        {
            criteria.DbServer = site.SiteName;
            await repository.ExecuteAsync(criteria);
            return new Result
            {
                ErrorCode = criteria.ErrCode,
                ErrorMessage = criteria.ErrMsg
            };
        });

        var results = await Task.WhenAll(tasks);
        var failedResult = results.ToList().Find(r => r.ErrorCode > 0);

        return failedResult ?? new Result();
    }
}