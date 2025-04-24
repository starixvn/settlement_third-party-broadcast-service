using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Constants;
using Criteria.VirtualBetRadar;
using Models;
using System;

public interface VirtualBetRadarService
{
    Task<Result> Settle(VirtualBetRadarRequest request);
}

public class VirtualBetRadarServiceImp(
    IDynamicRepository repository,
    IMapper mapper,
    CalculateService calculateService,
    MatchService matchService,
    AnnouncementService announcementService,
    RecipientMessageService recipientMessageService,
    TicketInfoService ticketService)
    : VirtualBetRadarService
{
    public async Task<Result> Settle(VirtualBetRadarRequest request)
    {
        var betRadarValidationCriteria = mapper.Map<VirtualBetRadarRequest, BetRadarValidationCriteria>(request);
        var resultValidation = await calculateService.ExecuteValidationMultiServer(betRadarValidationCriteria);

        if (!resultValidation.IsSuccessful)
        {
            return resultValidation;
        }

        var currentMatchInfo = await matchService.GetMatchInfo(request.MatchId);
        var currentTickets = await ticketService.GetTicketsByAction(request.MatchId, currentMatchInfo.SportType, request.Action);

        await matchService.UpdateMatchInfoBetRadar(request);

        await matchService.WriteLogBetRadar(request);

        return await HandleSettle(currentTickets, currentMatchInfo, request);
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualBetRadarRequest request)
    {
        var criteria = mapper.Map<VirtualBetRadarRequest, VirtualBetRadarCriteria>(request);
        var pendingCommand = CreateSiteDbPendingCommand(criteria, matchInfo.SportType);
        var settleResult = await calculateService.Execute(criteria, pendingCommand);

        await WriteLogSettled(request, settleResult);

        await recipientMessageService.Message(request.Action, matchInfo);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(VirtualBetRadarCriteria criteria, short sportType)
    {
        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            SportType = sportType,
            Action = criteria.Action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }

    private async Task WriteLogSettled(VirtualBetRadarRequest request, Result resultSettle)
    {
        var criteria = CreateWriteLogCriteria(request, resultSettle);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualBetRadarRequest request, Result settleResult)
    {
        var actionId = ActionIdValue(request.Action);

        var criteria = new WriteLogCriteria(request.MatchId, actionId, request.AdminId, request.Ip)
        {
            FinalAwayScore = request.Fta,
            FinalHomeScore = request.Fth,
            HtAwayScore = request.Hta,
            HtHomeScore = request.Hth,
            Remark = $"VS BetRadar - Calculate ({request.Action}) {(settleResult.ErrorCode > 0 ? settleResult.ErrorMessage : string.Empty)}"
        };
        return criteria;
    }

    private static int ActionIdValue(string actionName) => string.Equals(actionName, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? ActionId.VirtualBetRadarCalc
        : ActionId.VirtualBetRadarRefund;
}