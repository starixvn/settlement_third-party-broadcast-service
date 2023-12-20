namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.VirtualSports;
using System;

public interface VirtualSportsService
{
    Task<Result> Settle(VirtualSoccerRequest request);
}

public class VirtualSportsServiceImp : VirtualSportsService
{
    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly IDynamicRepository repository;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly TicketSettleService ticketSettleService;

    private readonly AnnouncementService announcementService;

    public VirtualSportsServiceImp(
        ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
        IDynamicRepository repository,
        IMapper mapper,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService,
        AnnouncementService announcementService,
        TicketSettleService ticketSettleService)
    {
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
        this.repository = repository;
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
        this.announcementService = announcementService;
        this.ticketSettleService = ticketSettleService;
    }

    public async Task<Result> Settle(VirtualSoccerRequest request)
    {
        var matchInfo = await GetMatchInfo(request.MatchId);
        var currentTickets = await ticketSettleService.GetTicketsByAction(request.MatchId, request.Action);

        await UpdateMatchInfo(request);

        var message = GenerateMessageLog(request);
        await logDirectlyService.WriteLog(request.MatchId, message);

        var result = await HandleSettle(currentTickets, matchInfo, request);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualSoccerRequest request)
    {
        var criteria = mapper.Map<VirtualSoccerRequest, VirtualSportsCriteria>(request);
        var settleResult = await calculateService.Execute(criteria);

        await SendMail(request.Action, matchInfo);

        await announcementService.Handle(currentTickets, matchInfo, criteria.AdminId);

        return settleResult;
    }

    private async Task SendMail(string action, MatchInfo matchInfo)
    {
        var eventStatusChecking = !string.IsNullOrWhiteSpace(matchInfo.EventStatus) &&
                                  string.Equals(matchInfo.EventStatus.ToLowerInvariant(), EventStatus.Completed);

        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) || eventStatusChecking)
        {
            var criteria = new VirtualSportsTicketCriteria { MatchId = matchInfo.MatchId };
            var (creditTickets, depositTickets) = await repository.FetchMultipleAsync<TicketInfo, TicketInfo>(criteria);

            var actionName = string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
                ? ActionName.Refund
                : ActionName.ReSettle;

            recipientTicketsService.CreditTickets(actionName, creditTickets.ToList(), recipients, matchInfo);
            recipientTicketsService.DepositTickets(actionName, depositTickets.ToList(), recipients, matchInfo);
        }
    }

    private async Task<MatchInfo> GetMatchInfo(int matchId)
    {
        var matchInfo = await repository.GetAsync<MatchInfo>(new VirtualSportsGetMatchInfoCriteria { MatchId = matchId });
        matchInfo.MatchId = matchId;

        return matchInfo;
    }

    private static string GenerateMessageLog(VirtualSoccerRequest request)
        => $"NAP: Calculate {request.Action} for matchId: {request.MatchId} score: {request.Fth}-{request.Fta}";

    private async Task UpdateMatchInfo(VirtualSoccerRequest request)
    {
        var updateInfoRequest = mapper.Map<VirtualSoccerRequest, UpdateMatchInfo3RdRequest>(request);
        var criteria = mapper.Map<VirtualSoccerRequest, VirtualSportsUpdateMatchCriteria>(request);

        if (string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase))
        {
            updateInfoRequest.FTAway = request.Fta;
            updateInfoRequest.FTHome = request.Fth;
            criteria.Command = "C";
            updateInfoRequest.Command = $"{SpName.VirtualSports}-CAL";
        }
        else
        {
            criteria.Command = "F";
            updateInfoRequest.Command = $"{SpName.VirtualSports}-REFUND";
        }

        await scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(updateInfoRequest);
        await repository.ExecuteAsync(criteria);
    }
}