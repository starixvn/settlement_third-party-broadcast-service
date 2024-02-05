namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.HappyFive;

public interface HappyFiveService
{
    Task<Result> Settle(HappyFiveRequest request);
}

public class HappyFiveServiceImp(
    IMapper mapper,
    CalculateService calculateService,
    IDynamicRepository repository,
    ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
    LogDirectlyService logDirectlyService)
    : HappyFiveService
{
    public async Task<Result> Settle(HappyFiveRequest request)
    {
        var result = await ValidateBeforeSettle(request);

        if (result.IsSuccessful)
        {
            await UpdateMatchInfo(request);

            await WriteLog(request.MatchId);

            var criteria = mapper.Map<HappyFiveRequest, HappyFiveCriteria>(request);
            return await calculateService.Execute(criteria);
        }

        return result;
    }

    private async Task WriteLog(int matchId)
    {
        var logMessage = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, logMessage);
    }

    private async Task<Result> ValidateBeforeSettle(HappyFiveRequest request)
    {
        var criteriaValidation = new ValidateHappyFive { MatchId = request.MatchId, Action = request.Action };
        await repository.ExecuteAsync(criteriaValidation);
        return new Result
        {
            ErrorCode = criteriaValidation.ErrCode,
            ErrorMessage = criteriaValidation.ErrMsg
        };
    }

    private async Task UpdateMatchInfo(HappyFiveRequest request)
    {
        var updateRequest = new UpdateMatchInfo3RdRequest
        {
            Command = EventStatus.Completed,
            MatchId = request.MatchId,
            Action = request.Action,
            Creator = request.AdminId
        };
        await scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(updateRequest);
    }
}