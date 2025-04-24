using Fanex.Settlement.ScaleOut.Constants;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;

public interface PinGoalService
{
    Task<Result> Settle(PinGoalRequest request);
}

public class PinGoalServiceImp(
    ScaleOutSettleService scaleOutSettleService,
    IMapper mapper,
    CalculateService calculateService,
    LogDirectlyService logDirectlyService,
    MatchService matchService,
    Fanex.Settlement.Converter.Services.XmlConvertService xmlConvertService)
    : PinGoalService
{
    private const int LeagueGroupPingoal = 252;
    public async Task<Result> Settle(PinGoalRequest request)
    {
        var criteria = mapper.Map<PinGoalRequest, PinGoalCriteria>(request);
        var matchInfo = await matchService.GetMatchInfo(request.MatchId, true);
        var validateResult = ValidateResult(matchInfo, request.SelectedResources, request.Action);
        var validateBeforePush = await ValidateBeforePush(request);

        if (!validateResult.IsSuccessful || !validateBeforePush.IsSuccessful)
        {
            return validateResult.IsSuccessful ? validateBeforePush : validateResult;
        }

        await UpdateMatchInfo(request);

        var message = GenerateMessageLog(request);
        await logDirectlyService.WriteLog(request.MatchId, message);

        return await calculateService.Execute(criteria);
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

    private Result ValidateResult(MatchInfo matchInfo, string selectedResources, string action)
    {
        if (action != Constants.ActionName.Calculate || matchInfo.LeagueGroupId != LeagueGroupPingoal)
        {
            return new Result();
        }

        var resources = xmlConvertService.Deserialize<ResourcesModel>(selectedResources);
        var finalResult = resources.Items.FirstOrDefault(item => item.Id == ResourceId.FullTimeResourceId);

        if (finalResult == null)
        {
            return new Result();
        }

        if (matchInfo.isHalftimeCal)
        {
            return new Result(1, "Failed: HT has not been settled");
        }

        var homeScore = int.Parse(finalResult.Home);
        var awayScore = int.Parse(finalResult.Away);

        if (matchInfo.HtHomeScore > homeScore || matchInfo.HtAwayScore > awayScore)
        {
            return new Result(1, "Cannot calculate due to FT scores are less than HT Score. Please recheck entered values.");
        }

        return new Result();
    }

    private async Task UpdateMatchInfo(PinGoalRequest request)
    {
        var requestUpdate = mapper.Map<PinGoalRequest, UpdateMatchInfoRequest>(request);
        await scaleOutSettleService.UpdateMatchInfo(requestUpdate);
    }
}