using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut.Constants;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Models;

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
    XmlConvertService xmlConvertService)
    : PinGoalService
{
    private const int LeagueGroupPingoal = 252;

    public async Task<Result> Settle(PinGoalRequest request)
    {
        var criteria = mapper.Map<PinGoalRequest, PinGoalCriteria>(request);
        var matchInfo = await matchService.GetMatchInfo(request.MatchId);
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
        if (!string.Equals(action, Constants.ActionName.Calculate, StringComparison.CurrentCultureIgnoreCase) || matchInfo.LeagueGroupId != LeagueGroupPingoal)
        {
            return new Result();
        }

        var resources = xmlConvertService.Deserialize<ResourcesModel>(selectedResources);
        var finalResult = resources.Items.Find(item => item.Id == ResourceId.FullTimeResourceId);
        var halfTimeResult = resources.Items.Find(item => item.Id == ResourceId.HalfTimeResourceId);

        var IsHtNotSettled = finalResult != null && halfTimeResult == null && (matchInfo.HtHomeScore is null || matchInfo.HtAwayScore is null);

        if (IsHtNotSettled)
        {
            return new Result(1, "Failed: HT has not been settled");
        }

        var IsFtNotSettled = halfTimeResult != null && finalResult == null && (matchInfo.FinalHomeScore is null || matchInfo.FinalAwayScore is null);

        return IsFtNotSettled ? new Result() : VerifyHtFTScore(matchInfo, halfTimeResult, finalResult);
    }

    private static Result VerifyHtFTScore(MatchInfo matchInfo, ResourceItemModel halfTimeResult, ResourceItemModel finalResult)
    {
        var ftHomeScore = finalResult != null ? int.Parse(finalResult.Home) : matchInfo.FinalHomeScore ?? 0;
        var ftAwayScore = finalResult != null ? int.Parse(finalResult.Away) : matchInfo.FinalAwayScore ?? 0;
        var htHomeScore = halfTimeResult != null ? int.Parse(halfTimeResult.Home) : matchInfo.HtHomeScore ?? 0;
        var htAwayScore = halfTimeResult != null ? int.Parse(halfTimeResult.Away) : matchInfo.HtAwayScore ?? 0;

        var IsHtScoreGreaterThanFt = htHomeScore > ftHomeScore || htAwayScore > ftAwayScore;

        return IsHtScoreGreaterThanFt ? new Result(1, "Cannot calculate due to FT scores are less than HT Score. Please recheck entered values.") : new Result();
    }

    private async Task UpdateMatchInfo(PinGoalRequest request)
    {
        var requestUpdate = mapper.Map<PinGoalRequest, UpdateMatchInfoRequest>(request);
        await scaleOutSettleService.UpdateMatchInfo(requestUpdate);
    }
}