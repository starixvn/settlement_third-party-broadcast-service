using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Criteria.BitCoin;
using Models;
using System;

public interface BitCoinService
{
    Task<Result> Settle(BitCoinRequest request);
}

public class BitCoinServiceImp(
    IDynamicRepository repository,
    IMapper mapper,
    CalculateService calculateService,
    LogDirectlyService logDirectlyService)
    : BitCoinService
{
    public async Task<Result> Settle(BitCoinRequest request)
    {
        var matchInfo = await GetMatchInfo(request.MatchId);

        await UpdateMatchInfo(request);

        await WriteLog(request.MatchId);

        var criteria = mapper.Map<BitCoinRequest, BitCoinCriteria>(request);
        var pendingCommand = CreateSiteDbPendingCommand(criteria, matchInfo);
        var result = await calculateService.Execute(criteria, pendingCommand);

        var eventStatus = matchInfo.EventStatus;
        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(eventStatus))
        {
            await Notification(request, eventStatus);
        }

        return result;
    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(BitCoinCriteria criteria, MatchInfo matchInfo)
    {
        var action = criteria.Action;
        if (IsReSettle(matchInfo.EventStatus, action))
        {
            action = ActionName.ReSettle;
        }

        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = ProductType.BitCoin,
            Action = action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }

    private static bool IsReSettle(string eventStatus, string action)
    {
        return !string.IsNullOrWhiteSpace(eventStatus)
               && string.Equals(action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
               && string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed);
    }

    private async Task WriteLog(int matchId)
    {
        var logMessage = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, logMessage);
    }

    private async Task<MatchInfo> GetMatchInfo(int matchId)
    {
        var criteria = new Match3rdSelCriteria { MatchId = matchId };

        return await repository.GetAsync<MatchInfo>(criteria);
    }

    private async Task Notification(BitCoinRequest request, string eventStatus)
    {
        if (string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed) || string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            var criteria = new NotifyCriteria
            {
                MatchId = request.MatchId,
                NotifyAction = string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) ? ActionId.BitCoinRefund : ActionId.BitCoinCalc
            };

            await repository.ExecuteAsync(criteria);
        }
    }

    private async Task UpdateMatchInfo(BitCoinRequest request)
    {
        var criteria = mapper.Map<BitCoinRequest, ResultDetail3rdUpdateCriteria>(request);
        await repository.ExecuteAsync(criteria);

        var updateEventStatus = new UpdateEventStatusCriteria
        {
            MatchId = request.MatchId,
            Action = request.Action
        };
        await repository.ExecuteAsync(updateEventStatus);
    }
}