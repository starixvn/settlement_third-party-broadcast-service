using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface CalculateService
{
    Task<Result> Execute(CalculateCriteria criteria);

    Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria);
}

public class CalculateServiceImp(
    IDynamicRepository repository,
    IOptions<AppSettingsKey> appSettingsKey)
    : CalculateService
{
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