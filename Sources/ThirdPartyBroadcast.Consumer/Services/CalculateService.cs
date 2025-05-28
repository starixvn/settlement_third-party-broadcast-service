using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface CalculateService
{
    Task<Result> Execute(CalculateCriteria criteria);

    Task<Result> Execute(CalculateCriteria criteria, SiteDbPendingCommand command);

    Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria);
}

public class CalculateServiceImp(
    IDynamicRepository repository,
    IOptions<AppSettingsKey> appSettingsKey,
    SiteDbClientService siteDbClientService,
    SiteDbPendingCommandService pendingCommandService)
    : CalculateService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _siteLocks = new();

    private static SemaphoreSlim GetOrCreateLock(int siteDbGroupId)
    {
        return _siteLocks.GetOrAdd(siteDbGroupId, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<Result> Execute(CalculateCriteria criteria)
    {
        var tasks = appSettingsKey.Value
            .SiteDbServer
            .Select(s => CalculateByServer(s, criteria))
            .ToList();
        var results = await Task.WhenAll(tasks.ToArray());
        var resultFailed = results.Where(r => r.ErrorCode > 0).ToList();

        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrorMessage).ToArray())
        };
    }

    public async Task<Result> Execute(CalculateCriteria criteria, SiteDbPendingCommand command)
    {
        var siteDbServer = await siteDbClientService.GetAllSiteDbAsync();

        var tasks = siteDbServer
            .Select(async site => await CalculateByServer(site, criteria, command))
            .ToList();

        var results = await Task.WhenAll(tasks);
        var resultFailed = results.Where(r => r.ErrorCode > 0).ToList();
        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrorMessage).ToArray())
        };
    }

    private async Task<Result> CalculateByServer(string server, CalculateCriteria criteria)
    {
        criteria.DbServer = server;
        await repository.ExecuteAsync(criteria);

        return new Result
        {
            ErrorCode = criteria.ErrCode,
            ErrorMessage = $"{server}: {criteria.ErrCode} - {criteria.ErrMsg}"
        };
    }

    private async Task<Result> CalculateByServer(SiteDb site, CalculateCriteria criteria, SiteDbPendingCommand command)
    {
        var semaphore = GetOrCreateLock(site.SiteDbGroupId);
        try
        {
            await semaphore.WaitAsync();
            var isActive = await siteDbClientService.IsSiteDbActiveAsync(site.SiteDbGroupId);
            var pendingCommand = await pendingCommandService.CheckingPendingCommand(command.MatchId, site.SiteDbGroupId);

            if (isActive && pendingCommand.ErrorCode == 0)
            {
                return await ProcessActiveSite(site, criteria);
            }

            return await ProcessInactiveSite(site.SiteDbGroupId, command);
        }
        finally
        {
            semaphore.Release();
        }
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