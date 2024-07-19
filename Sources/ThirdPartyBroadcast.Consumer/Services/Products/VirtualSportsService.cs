namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.VirtualSports;
using Microsoft.Extensions.Options;
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
    RecipientMessageService recipientMessageService)
    TicketService ticketService,
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
        var settleResult = await calculateService.Execute(criteria);

        await recipientMessageService.Message(request.Action, matchInfo);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, criteria.AdminId);

        return settleResult;
    }
}