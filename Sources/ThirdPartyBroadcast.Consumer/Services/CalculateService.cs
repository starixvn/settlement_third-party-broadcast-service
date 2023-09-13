using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface CalculateService
{
    Task<Result> Execute(CalculateCriteria criteria);

    Task<Result> ExecuteValidationMultiServer(BaseCriteria criteria);

    Task<Result> ValidationDeployFlag(string spName);
}

public class CalculateServiceImp : CalculateService
{
    private readonly IDynamicRepository repository;

    private readonly AppSettingsKey appSettingsKey;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private const int DeployFlag = 99;

    public CalculateServiceImp(
        IDynamicRepository repository,
        IOptions<AppSettingsKey> appSettingsKey,
        ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService)
    {
        this.repository = repository;
        this.appSettingsKey = appSettingsKey.Value;
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
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

        var errorCode = GetErrorCode(results);

        return new Result
        {
            ErrorCode = errorCode,
            ErrorMessage = string.Join(" | ", results.Select(r => r.ErrorMessage).ToArray())
        };
    }

    private static int GetErrorCode(IReadOnlyCollection<Result> listResult)
    {
        var deployFlagError = listResult.Where(result => result.ErrorCode == DeployFlag);
        var totalError = listResult.Count;

        if (deployFlagError.Any())
        {
            return DeployFlag;
        }

        return totalError > 0 ? totalError : 0;
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

    public async Task<Result> ValidationDeployFlag(string spName)
    {
        var getDeploymentInfoRequest = new GetDeploymentInfoRequest
        {
            SpName = spName
        };

        var result = await scaleOutSettleThirdPartyService.GetDeploymentInfo(getDeploymentInfoRequest);

        return new Result
        {
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}