using Fanex.Settlement.ScaleOut.Constants;
using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products.FastMarket;

public interface FastMarketService
{
    Task<Result> Settle(FastMarketRequest request);
}

public class FastMarketServiceImp(
    ScaleOutSettleService scaleOutSettleService,
    IMapper mapper,
    CalculateService calculateService,
    LogDirectlyService logDirectlyService,
    FastMarketResultAmendmentService fmResultAmendmentService)
    : FastMarketService
{
    public async Task<Result> Settle(FastMarketRequest request)
    {
        var criteria = mapper.Map<FastMarketRequest, FastMarketCriteria>(request);
        var requestValidation = mapper.Map<FastMarketCriteria, EventTicketFastMarketRequest>(criteria);
        var validationResult = await scaleOutSettleService.ValidateEventAndTicketFastMarket(requestValidation);

        if (validationResult.ErrorCode != 0)
        {
            return new Result
            {
                ErrorCode = validationResult.ErrorCode,
                ErrorMessage = validationResult.ErrorMessage
            };
        }

        await UpdateMatchInfo(criteria);

        var message = GenerateLogMessage(criteria);
        await logDirectlyService.WriteLog(criteria.MatchId, message);
        var pendingCommand = CreateSiteDbPendingCommand(criteria);
        var result = await calculateService.Execute(criteria, pendingCommand);
        if (result.IsSuccessful)
        {
            await fmResultAmendmentService.PushResultAmendment(request, criteria);
        }

        return result;
    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(FastMarketCriteria criteria)
    {
        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = ProductType.FastMarket,
            Action = criteria.Action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }

    private static string GenerateLogMessage(FastMarketCriteria criteria)
        => $"NAP: Fast Market {criteria.Action} for matchId: {criteria.MatchId}";

    private async Task UpdateMatchInfo(FastMarketCriteria criteria)
    {
        var requestUpdateInfo = new UpdateMatchInfoRequest
        {
            MatchId = criteria.MatchId,
            Action = criteria.Action,
            AdminId = criteria.AdminId,
            Results = criteria.Results,
            Mode = Mode.FastMarket,
            IsSettleDetermined = criteria.IsSettleDetermined,
            IsAuto = criteria.IsAuto
        };

        await scaleOutSettleService.UpdateMatchInfo(requestUpdateInfo);
    }
}