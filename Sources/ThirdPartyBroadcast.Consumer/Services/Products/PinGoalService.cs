using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut.Constants;
using ActionName = ThirdPartyBroadcast.Consumer.Constants.ActionName;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Fanex.Settlement.ThirdPartyBroadcast.Shared;
using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
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

        var criteria = mapper.Map<PinGoalRequest, PinGoalCriteria>(request);
        var pendingCommand = CreateSiteDbPendingCommand(criteria);

        return await calculateService.Execute(criteria, pendingCommand);
    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(PinGoalCriteria criteria)
    {
        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = ProductType.PinGoal,
            Action = criteria.Action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
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
        if (!string.Equals(action, ActionName.Calculate, StringComparison.CurrentCultureIgnoreCase) || matchInfo.LeagueGroupId != LeagueGroupPingoal)
        {
            return new Result();
        }

        var (finalResult, halfTimeResult) = GetResourceResults(selectedResources);

        if (IsResultNotSettled(finalResult, halfTimeResult, matchInfo.HtHomeScore, matchInfo.HtAwayScore))
        {
            return new Result(1, "Failed: HT has not been settled");
        }

        return IsResultNotSettled(halfTimeResult, finalResult, matchInfo.FinalHomeScore, matchInfo.FinalAwayScore)
            ? new Result()
            : VerifyHtFTScore(matchInfo, halfTimeResult, finalResult);
    }

    private (ResourceItemModel finalResult, ResourceItemModel halfTimeResult) GetResourceResults(string selectedResources)
    {
        var resources = xmlConvertService.Deserialize<ResourcesModel>(selectedResources);
        var finalResult = resources.Items.Find(item => item.Id == ResourceId.FullTimeResourceId);
        var halfTimeResult = resources.Items.Find(item => item.Id == ResourceId.HalfTimeResourceId);
        return (finalResult, halfTimeResult);
    }

    private static bool IsResultNotSettled(ResourceItemModel presentResult, ResourceItemModel missingResult, int? homeScore, int? awayScore)
         => presentResult != null && missingResult == null && (!homeScore.HasValue || !awayScore.HasValue);

    private static Result VerifyHtFTScore(MatchInfo match, ResourceItemModel htResult, ResourceItemModel ftResult)
    {
        var ftHome = ParseScore(ftResult?.Home, match.FinalHomeScore);
        var ftAway = ParseScore(ftResult?.Away, match.FinalAwayScore);
        var htHome = ParseScore(htResult?.Home, match.HtHomeScore);
        var htAway = ParseScore(htResult?.Away, match.HtAwayScore);

        return IsHtScoreGreaterThanFt(htHome, ftHome, htAway, ftAway)
            ? new Result(1, "Cannot calculate due to FT scores are less than HT Score. Please recheck entered values.")
            : new Result();
    }

    private static int ParseScore(string resourceScore, int? matchScore)
        => int.TryParse(resourceScore, out var value) ? value : matchScore ?? 0;

    private static bool IsHtScoreGreaterThanFt(int htHome, int ftHome, int htAway, int ftAway)
        => htHome > ftHome || htAway > ftAway;

    private async Task UpdateMatchInfo(PinGoalRequest request)
    {
        var requestUpdate = mapper.Map<PinGoalRequest, UpdateMatchInfoRequest>(request);
        await scaleOutSettleService.UpdateMatchInfo(requestUpdate);
    }
}