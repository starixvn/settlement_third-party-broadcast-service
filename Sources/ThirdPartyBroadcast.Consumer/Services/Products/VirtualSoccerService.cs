namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.VirtualSoccer;

public interface VirtualSoccerService
{
    Task<Result> Settle(VirtualSoccerRequest request);
}

public class VirtualSoccerServiceImp : VirtualSoccerService
{
    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly IDynamicRepository repository;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly TicketSettleService ticketSettleService;

    private readonly AnnouncementService announcementService;

    public VirtualSoccerServiceImp(
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

        var criteria = mapper.Map<VirtualSoccerRequest, VirtualSoccerCriteria>(request);

        var message = GenerateMessageLog(criteria);
        await logDirectlyService.WriteLog(criteria.MatchId, message);

        var result = await HandleSettle(currentTickets, matchInfo, criteria);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualSoccerCriteria criteria)
    {
        var settleResult = await calculateService.Execute(criteria);

        await announcementService.Handle(currentTickets, matchInfo, criteria.AdminId);

        return settleResult;
    }

    private async Task<MatchInfo> GetMatchInfo(int matchId)
    {
        var matchInfo = await repository.GetAsync<MatchInfo>(new VisualSoccerGetMatchInfoCriteria { MatchId = matchId });
        matchInfo.MatchId = matchId;

        return matchInfo;
    }

    private static string GenerateMessageLog(VirtualSoccerCriteria criteria)
        => $"NAP: Calculate {criteria.Action} for matchId: {criteria.MatchId} score: {criteria.Fth}-{criteria.Fta}";

    private async Task UpdateMatchInfo(VirtualSoccerRequest request)
    {
        var updateInfoRequest = mapper.Map<VirtualSoccerRequest, UpdateMatchInfo3RdRequest>(request);
        var criteria = mapper.Map<VirtualSoccerRequest, VrSoccerUpdateMatchCriteria>(request);

        if (request.Action.ToLowerInvariant().Equals(ActionType.Calculate))
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