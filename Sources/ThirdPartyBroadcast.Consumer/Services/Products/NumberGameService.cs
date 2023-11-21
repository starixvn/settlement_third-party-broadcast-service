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

    public NumberGameServiceImp(
        IMapper mapper,
        ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService)
    {
        this.mapper = mapper;
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
    }

    public async Task<Result> Settle(NumberGameRequest request)
    {
        if (!request.IsLive)
        {
            await UpdateMatchInfo(request);
        }

        await WriteLog(request);

        var criteria = mapper.Map<NumberGameRequest, NumberGameCriteria>(request);
        return await calculateService.Execute(criteria);
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