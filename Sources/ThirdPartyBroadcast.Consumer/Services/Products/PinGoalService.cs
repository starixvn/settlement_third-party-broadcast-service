using Fanex.Settlement.ScaleOut.Constants;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;

public interface PinGoalService
{
    Task<Result> Settle(PinGoalRequest request);
}

public class PinGoalServiceImp : PinGoalService
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    public PinGoalServiceImp(
        ScaleOutSettleService scaleOutSettleService,
        IMapper mapper,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService)
    {
        this.scaleOutSettleService = scaleOutSettleService;
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
    }

    public async Task<Result> Settle(PinGoalRequest request)
    {
        var criteria = mapper.Map<PinGoalRequest, PinGoalCriteria>(request);
        var validationResult = await ValidateBeforePush(request);

        if (validationResult.IsSuccessful)
        {
            await UpdateMatchInfo(request);

            var message = GenerateMessageLog(request);
            await logDirectlyService.WriteLog(request.MatchId, message);

            return await calculateService.Execute(criteria);
        }

        return validationResult;
    }

    private static string GenerateMessageLog(PinGoalRequest request)
        => $"NAP: Calculate {request.Action} for matchId: {request.MatchId} Result: {request.SelectedResources} <br/> BetStatus ({request.BetStatus})";

    private async Task<Result> ValidateBeforePush(PinGoalRequest request)
    {
        var validationRequest = new EventTicketRequest
        {
            MatchId = request.MatchId,
            Action = request.Action,
            SpName = SpName.ProfilePush,
            SelectedResources = request.SelectedResources,
            Mode = Mode.PinGoal,
            IsAuto = request.IsAuto
        };

        var validationResult = await scaleOutSettleService.ValidateEventAndTicket(validationRequest);

        return new Result
        {
            ErrorCode = validationResult.ErrorCode,
            ErrorMessage = validationResult.ErrorMessage
        };
    }

    private async Task UpdateMatchInfo(PinGoalRequest request)
    {
        var requestUpdate = mapper.Map<PinGoalRequest, UpdateMatchInfoRequest>(request);
        await scaleOutSettleService.UpdateMatchInfo(requestUpdate);
    }
}