namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;

public interface NumberGameService
{
    Task<Result> Settle(NumberGameRequest request);
}

public class NumberGameServiceImp : NumberGameService
{
    private readonly IMapper mapper;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly IDynamicRepository repository;

    public NumberGameServiceImp(
        IMapper mapper,
        ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService,
        IDynamicRepository repository)
    {
        this.mapper = mapper;
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
        this.repository = repository;
    }

    public async Task<Result> Settle(NumberGameRequest request)
    {
        if (!request.IsLive)
        {
            await UpdateMatchInfo(request);
        }

        await WriteLog(request);

        var criteria = mapper.Map<NumberGameRequest, NumberGameCriteria>(request);

        var settledResult = await calculateService.Execute(criteria);

        await WriteLogSettled(criteria, settledResult);

        return settledResult;
    }

    private async Task WriteLogSettled(NumberGameCriteria criteria, Result resultSettle)
    {
        var logCriteria = CreateWriteLogCriteria(criteria, resultSettle);

        await repository.ExecuteAsync(logCriteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(NumberGameCriteria criteria, Result resultSettle)
    {
        var actionId = criteria.Action.ToUpperInvariant() switch
        {
            ActionName.Calculate => ActionId.NgCal,
            ActionName.CalculateHt => ActionId.NgCalHt,
            ActionName.Refund => ActionId.NgRefund,
            ActionName.RefundFt => ActionId.NgRefundFt,
            _ => 0,
        };

        var message = $"NumberGame - Calculate({criteria.Action}) " +
            $"{(criteria.IsLive ? "- [LIVE] -" : string.Empty)} Sequence number: {criteria.DrawSeq}" +
            $" {(resultSettle.ErrorCode > 0 ? resultSettle.ErrorMessage : string.Empty)}";

        var logCriteria = new WriteLogCriteria(criteria.MatchId, message, actionId, criteria.AdminId, criteria.Ip);

        return logCriteria;
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