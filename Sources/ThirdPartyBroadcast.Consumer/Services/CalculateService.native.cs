using Fanex.SiteDbConnectionManager.Models;
using Fanex.SiteDbConnectionManager.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Helper;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface CalculateService
{
    Task<Result> Execute(CalculateCriteria criteria);

    Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria);
}

public class CalculateServiceImp(
    IDynamicRepository repository,
    IOptions<AppSettingsKey> appSettingsKey,
    SiteDbService siteDbService)
    : CalculateService
{
    public async Task<Result> Execute(CalculateCriteria criteria)
    {
        var siteDbServer = await siteDbService.GetAllSiteDbAsync();

        var tasks = siteDbServer
            .Select(site => CalculateByServer(site, criteria))
            .ToList();

        var results = await Task.WhenAll(tasks.ToArray());
        var resultFailed = results.Where(r => r.ErrorCode > 0).ToList();
        return new Result
        {
            ErrorCode = resultFailed.Count,
            ErrorMessage = string.Join(" | ", resultFailed.Select(r => r.ErrorMessage).ToArray())
        };
    }

    private async Task<Result> CalculateByServer(SiteDb site, CalculateCriteria criteria)
    {
        var isActive = await siteDbService.IsSiteDbActiveAsync(site.SiteDbGroupId);
        if (!isActive)
        {
            var criteriaJson = criteria.ToJson();
            var person = JsonSerializer.Deserialize<NumberGameCriteria>(criteriaJson, JsonSettings.Options);

            return new Result
            {
                ErrorCode = 1,
                ErrorMessage = $"{site.SiteDbGroupId} is not active"
            };
        }

        criteria.DbServer = site.SiteName;
        await repository.ExecuteAsync(criteria);

        return new Result
        {
            ErrorCode = criteria.ErrCode,
            ErrorMessage = $"{site.SiteName}: {criteria.ErrCode} - {criteria.ErrMsg}"
        };
    }

    public async Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria)
    {
        foreach (var server in appSettingsKey.Value.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
            if (criteria.ErrCode > 0)
            {
                return new Result
                {
                    ErrorCode = criteria.ErrCode,
                    ErrorMessage = criteria.ErrMsg
                };
            }
        }

        return new Result();
    }
}