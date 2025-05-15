namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.VirtualSports;
using Fanex.Settlement.ThirdPartyBroadcast.Shared;
using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using Models;
using System.Collections.Generic;

public interface VirtualSportsService
{
    Task<Result> Settle(VirtualSoccerRequest request);
}

public class VirtualSportsServiceImp(
    MatchService matchService,
    IMapper mapper,
    CalculateService calculateService,
    AnnouncementService announcementService,
    RecipientMessageService recipientMessageService,
    TicketInfoService ticketService)
    : VirtualSportsService
{
    public async Task<Result> Settle(VirtualSoccerRequest request)
    {
        var matchInfo = await matchService.GetMatchInfo(request.MatchId);
        var currentTickets = await ticketService.GetTicketsByAction(request.MatchId, matchInfo.SportType, request.Action);

        await matchService.UpdateMatchInfoVirtualSports(request);

        await matchService.WriteLogVirtualSports(request);

        var result = await HandleSettle(currentTickets, matchInfo, request);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualSoccerRequest request)
    {
        var criteria = mapper.Map<VirtualSoccerRequest, VirtualSportsCriteria>(request);
        var pendingCommand = CreateSiteDbPendingCommand(criteria, matchInfo);
        var settleResult = await calculateService.Execute(criteria, pendingCommand);

        await recipientMessageService.Message(request.Action, matchInfo);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, criteria.AdminId);

        return settleResult;
    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(VirtualSportsCriteria criteria, MatchInfo matchInfo)
    {
        var action = criteria.Action;
        if (!string.IsNullOrWhiteSpace(matchInfo.EventStatus) && string.Equals(matchInfo.EventStatus.ToLowerInvariant(), EventStatus.Completed))
        {
            action = ActionName.ReSettle;
        }

        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = ProductType.VirtualSports,
            Action = action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }
}