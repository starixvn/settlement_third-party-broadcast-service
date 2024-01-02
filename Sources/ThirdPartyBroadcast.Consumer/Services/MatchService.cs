using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface MatchService
{
    Task<MatchInfo> GetMatchInfoVirtualSports(int matchId);

    Task UpdateMatchInfoVirtualSports(VirtualSoccerRequest request);

    Task<(IEnumerable<TicketInfo> creditTickets, IEnumerable<TicketInfo> depositTickets)> GetAllTicketsVirtualSports(int matchId);

    Task WriteLogVirtualSports(VirtualSoccerRequest request);

    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action);

    Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets);
}

public class MatchServiceImp : MatchService
{
    private readonly IDynamicRepository repository;

    private readonly IMapper mapper;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly RefDataService refDataService;

    private readonly AppSettingsKey appSettings;

    public MatchServiceImp(
        IDynamicRepository repository,
        IMapper mapper,
        ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
        LogDirectlyService logDirectlyService,
        RefDataService refDataService,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.repository = repository;
        this.mapper = mapper;
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
        this.logDirectlyService = logDirectlyService;
        this.refDataService = refDataService;
        this.appSettings = appSettingsKey.Value;
    }

    public async Task<MatchInfo> GetMatchInfoVirtualSports(int matchId)
    {
        var criteria = new VirtualSportsGetMatchInfoCriteria { MatchId = matchId };
        var match = await repository.GetAsync<MatchInfo>(criteria);
        match.RefDataService = refDataService;
        return match;
    }

    public async Task UpdateMatchInfoVirtualSports(VirtualSoccerRequest request)
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

    public async Task<(IEnumerable<TicketInfo> creditTickets, IEnumerable<TicketInfo> depositTickets)> GetAllTicketsVirtualSports(int matchId)
    {
        var criteria = new VirtualSportsTicketCriteria { MatchId = matchId };
        return await repository.FetchMultipleAsync<TicketInfo, TicketInfo>(criteria);
    }

    public async Task WriteLogVirtualSports(VirtualSoccerRequest request)
    {
        var message = $"NAP: Calculate {request.Action} for matchId: {request.MatchId} score: {request.Fth}-{request.Fta}";
        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return await GetAllTickets(new TicketSettleCriteria { MatchId = matchId });
        }

        return new List<TicketSettleInfo>();
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets)
    {
        if (tickets.Count == 0)
        {
            return new List<TicketSettleInfo>();
        }

        var transIds = string.Join(',', tickets.Select(x => x.TransId).ToArray());
        return await GetAllTickets(new TicketSettleCriteria { MatchId = matchId, TransIDs = transIds });
    }

    private async Task<List<TicketSettleInfo>> GetAllTickets(TicketSettleCriteria criteria)
    {
        var result = new List<TicketSettleInfo>();
        foreach (var server in appSettings.SiteDbServer)
        {
            criteria.DbServer = server;

            var ticketInfo = await repository.FetchAsync<TicketSettleInfo>(criteria);

            result.AddRange(ticketInfo);
        }

        return result;
    }
}