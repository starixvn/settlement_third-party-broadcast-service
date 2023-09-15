using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface CalculateService
{
    Task<Result> Execute(CalculateCriteria criteria);

    Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria);
}

public class CalculateServiceImp : CalculateService
{
    private readonly IDynamicRepository repository;

    private readonly AppSettingsKey appSettingsKey;

    public CalculateServiceImp(
        IDynamicRepository repository,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.repository = repository;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task<Result> Execute(CalculateCriteria criteria)
    {
        var results = new List<Result>();
        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
            results = ResultMessage(results, server, criteria);
        }

        return new Result
        {
            ErrorCode = results.Count,
            ErrorMessage = string.Join(" | ", results.Select(r => r.ErrorMessage).ToArray())
        };
    }

    private static List<Result> ResultMessage(List<Result> results, string server, CalculateCriteria criteria)
    {
        if (criteria.ErrCode > 0)
        {
            var result = new Result
            {
                ErrorCode = criteria.ErrCode,
                ErrorMessage = $"{server}: {criteria.ErrCode} - {criteria.ErrMsg}"
            };

            results.Add(result);
        }

        return results;
    }

    public async Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria)
    {
        foreach (var server in appSettingsKey.SiteDbServer)
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