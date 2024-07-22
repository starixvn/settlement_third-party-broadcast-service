namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;

public interface NumberGameService
{
    Task<Result> Settle(NumberGameRequest request);
}

public class NumberGameServiceImp(
    IMapper mapper,
    ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
    CalculateService calculateService,
    LogDirectlyService logDirectlyService,
    IDynamicRepository repository) : NumberGameService
{
    public async Task<Result> Settle(NumberGameRequest request)
    {
        if (!request.IsLive)
        {
            await UpdateMatchInfo(request);
        }

        await WriteLog(request);

        var criteria = mapper.Map<NumberGameRequest, NumberGameCriteria>(request);

        var startSettle = DateTimeOffset.Now;
        var settledResult = await calculateService.Execute(criteria);
        var endSettle = DateTimeOffset.Now;

        await WriteLogSettled(criteria, settledResult, startSettle, endSettle);

        return settledResult;
    }

    private async Task WriteLogSettled(NumberGameCriteria criteria, Result resultSettle, DateTimeOffset start, DateTimeOffset end)
    {
        var actionId = GetLogActionId(criteria);

        var message = $"NumberGame - Calculate({criteria.Action}) Start at {start} and End at {end}" +
            $"{(criteria.IsLive ? "- [LIVE] -" : string.Empty)} Sequence number: {criteria.DrawSeq}" +
            $" {(resultSettle.ErrorCode > 0 ? resultSettle.ErrorMessage : string.Empty)}";

        var logCriteria = new WriteLogCriteria(criteria.MatchId, message, actionId, criteria.AdminId, criteria.Ip);

        await repository.ExecuteAsync(logCriteria);
    }

    private static int GetLogActionId(NumberGameCriteria criteria)
    {
        if (criteria.IsLive)
        {
            return ActionId.NgIsLive;
        }

        return criteria.Action.ToUpperInvariant() switch
        {
            "CAL" => ActionId.NgCal,
            "CAL-HT" => ActionId.NgCalHt,
            "REFUND" => ActionId.NgRefund,
            "REFUND-FT" => ActionId.NgRefundFt,
            _ => 0,
        };
    }

    private async Task WriteLog(NumberGameRequest request)
    {
        var message = GenerateLogMessage(request);
        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    private static string GenerateLogMessage(NumberGameRequest request)
    {
        if (request.IsLive)
        {
            return $"Calculate Number Game for matchId: {request.MatchId}- [LIVE] - Sequence number: {request.DrawSeq}";
        }

        return string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? $"Calculate Number Game for matchId: {request.MatchId}" : string.Empty;
    }

    private async Task UpdateMatchInfo(NumberGameRequest request)
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