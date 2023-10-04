using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Constants;
using Criteria.VirtualBetRadar;

public interface VirtualBetRadarService
{
    Task<Result> Settle(VirtualBetRadarRequest request);
}

public class VirtualBetRadarServiceImp : VirtualBetRadarService
{
    private readonly IDynamicRepository repository;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    public VirtualBetRadarServiceImp(
        IDynamicRepository repository,
        IMapper mapper,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService)
    {
        this.repository = repository;
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
    }

    public async Task<Result> Settle(VirtualBetRadarRequest request)
    {
        var betRadarValidationCriteria = mapper.Map<VirtualBetRadarRequest, BetRadarValidationCriteria>(request);
        var resultValidation = await calculateService.ExecuteValidationMultiServer(betRadarValidationCriteria);
        var criteria = mapper.Map<VirtualBetRadarRequest, VirtualBetRadarCriteria>(request);
        criteria.NotifyFlag = await GetNotifyFlag(request.MatchId, request.Action);

        if (resultValidation.IsSuccessful)
        {
            await UpdateMatchInfo(request);

            var message = GenerateMessageLog(criteria);
            await logDirectlyService.WriteLog(criteria.MatchId, message);

            var result = await calculateService.Execute(criteria);

            await HandleAfterSettle(request, result);
            return result;
        }

        return resultValidation;
    }

    private static string GenerateMessageLog(VirtualBetRadarCriteria criteria)
        => $@"NAP: VS BetRadar - Calculate {criteria.Action} for matchId: {criteria.MatchId}
              Scores: FT=[{criteria.Fth}-{criteria.Fta}]; HT=[{criteria.Hth}-{criteria.Hta}]
              Resource Results: {criteria.ResourceResult}";

    private async Task HandleAfterSettle(VirtualBetRadarRequest request, Result settleResult)
    {
        var actionId = ActionIdValue(request.Action);
        var criteria = new WriteLogCriteria(request.MatchId, actionId, request.AdminId, request.Ip)
        {
            FinalAwayScore = request.Fta,
            FinalHomeScore = request.Fth,
            HtAwayScore = request.Hta,
            HtHomeScore = request.Hth
        };

        if (settleResult.IsSuccessful)
        {
            criteria.Remark = "NAP: VS BetRadar - API inserted into single queue successfully.";
            await repository.ExecuteAsync(criteria);
        }
        else
        {
            var message = $"NAP: VS BetRadar - Error: API inserted into single queue failed. {settleResult.ErrorMessage}";
            criteria.Remark = message;
            await repository.ExecuteAsync(criteria);

            var sendMailCriteria = mapper.Map<WriteLogCriteria, SendMailCriteria>(criteria);
            sendMailCriteria.Message = message;
            await repository.ExecuteAsync(sendMailCriteria);
        }
    }

    private static int ActionIdValue(string actionName) => actionName.ToLowerInvariant().Equals(ActionType.Calculate) ? ActionId.VirtualBetRadarCalc : ActionId.VirtualBetRadarRefund;

    private async Task UpdateMatchInfo(VirtualBetRadarRequest request)
    {
        var criteria = mapper.Map<VirtualBetRadarRequest, BetRadarUpdateMatchCriteria>(request);
        await repository.ExecuteAsync(criteria);
    }

    private async Task<int> GetNotifyFlag(int matchId, string action)
    {
        const int notifyRefund = 10;
        const int notifySingleValue = 2;
        if (!action.ToLowerInvariant().Equals(ActionType.Calculate))
        {
            return notifyRefund;
        }

        var criteria = new GetMatchStatusCriteria { MatchId = matchId };
        var matchStatus = await repository.GetAsync<MatchStatus>(criteria);
        var hasNotify = matchStatus != null && (matchStatus.Calculate || matchStatus.CalRefund);
        return hasNotify ? notifySingleValue + ParlayNotify(matchStatus) : 0;
    }

    private static int ParlayNotify(MatchStatus matchStatus)
    {
        const int notifyParlayValue = 8;
        return matchStatus.Multiple || matchStatus.Refund ? notifyParlayValue : 0;
    }
}