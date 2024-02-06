using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface MatchService
{
    Task<MatchInfo> GetMatchInfo(int matchId);

    Task<MatchInfo> GetMatchInfoVirtualSports(int matchId);

    Task<MatchInfo> GetMatchInfoLeapGaming(int matchId);

    Task UpdateMatchInfoVirtualSports(VirtualSoccerRequest request);

    Task UpdateMatchInfoLeapGaming(LeapGamingRequest request);

    Task UpdateMatchInfoBetRadar(VirtualBetRadarRequest request);

    Task<(IEnumerable<TicketInfo> creditTickets, IEnumerable<TicketInfo> depositTickets)> GetAllTicketsVirtualSports(int matchId);

    Task WriteLogVirtualSports(VirtualSoccerRequest request);

    Task WriteLogLeapGaming(int matchId);

    Task WriteLogBetRadar(VirtualBetRadarRequest request);

    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action);

    Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets);

    bool IsReSettleOrRefund(string action, string eventStatus);

    IEnumerable<TicketInfo> SplitTicketByLicensee(IReadOnlyCollection<TicketInfo> ticketInfos);
}

public class MatchServiceImp(
    IDynamicRepository repository,
    IMapper mapper,
    ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
    LogDirectlyService logDirectlyService,
    RefDataService refDataService,
    IOptions<AppSettingsKey> appSettingsKey)
    : MatchService
{
    public async Task<MatchInfo> GetMatchInfo(int matchId)
    {
        var criteria = new GetMatchCriteria { MatchId = matchId };
        var match = await repository.GetAsync<MatchInfo>(criteria);
        match.RefDataService = refDataService;
        return match;
    }

    public async Task<MatchInfo> GetMatchInfoVirtualSports(int matchId)
    {
        var criteria = new VirtualSportsGetMatchInfoCriteria { MatchId = matchId };
        var match = await repository.GetAsync<MatchInfo>(criteria);
        match.RefDataService = refDataService;
        return match;
    }

    public async Task<MatchInfo> GetMatchInfoLeapGaming(int matchId)
    {
        var criteria = new LeapGamingGetMatchInfoCriteria { MatchId = matchId };
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

    public async Task UpdateMatchInfoLeapGaming(LeapGamingRequest request)
    {
        var requestUpdate = mapper.Map<LeapGamingRequest, UpdateMatchCriteria>(request);
        requestUpdate.Command = EventStatus.Completed;
        await repository.ExecuteAsync(requestUpdate);
    }

    public async Task UpdateMatchInfoBetRadar(VirtualBetRadarRequest request)
    {
        var criteria = mapper.Map<VirtualBetRadarRequest, BetRadarUpdateMatchCriteria>(request);
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

    public async Task WriteLogLeapGaming(int matchId)
    {
        var message = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, message);
    }

    public async Task WriteLogBetRadar(VirtualBetRadarRequest request)
    {
        var message = $@"NAP: VS BetRadar - Calculate {request.Action} for matchId: {request.MatchId}
              Scores: FT=[{request.Fth}-{request.Fta}]; HT=[{request.Hth}-{request.Hta}]
              Resource Results: {request.ResourceResult}";

        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return await GetAllTickets(new TicketSettleCriteria { MatchId = matchId });
        }

        return [];
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets)
    {
        if (tickets.Count == 0)
        {
            return [];
        }

        var transIds = string.Join(',', tickets.Select(x => x.TransId).ToArray());
        return await GetAllTickets(new TicketSettleCriteria { MatchId = matchId, TransIDs = transIds });
    }

    private async Task<List<TicketSettleInfo>> GetAllTickets(TicketSettleCriteria criteria)
    {
        var result = new List<TicketSettleInfo>();
        foreach (var server in appSettingsKey.Value.SiteDbServer)
        {
            criteria.DbServer = server;

            var ticketInfo = await repository.FetchAsync<TicketSettleInfo>(criteria);

            result.AddRange(ticketInfo);
        }

        return result;
    }

    public bool IsReSettleOrRefund(string action, string eventStatus) =>
        (!string.IsNullOrWhiteSpace(eventStatus) && string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed))
        || string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase);

    public IEnumerable<TicketInfo> SplitTicketByLicensee(IReadOnlyCollection<TicketInfo> ticketInfos)
    {
        var distinctLicensees = ticketInfos.Select(ticket => ticket.SiteName).Distinct();

        return distinctLicensees.Select(licensee => new TicketInfo
        {
            TicketsInfo = ticketInfos.Where(ticket => ticket.SiteName == licensee).ToList()
        });
    }
}