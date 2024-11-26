using Microsoft.Extensions.Options;
using Settlement.Business.Core.Models;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualRacing;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

namespace ThirdPartyBroadcast.Consumer.Services;

using Tickets = (IEnumerable<TicketInfo> creditTickets, IEnumerable<TicketInfo> depositTickets);

public interface MatchService
{
    Task<MatchInfo> GetMatchInfo(int matchId);

    Task<MatchInfo> GetMatch3rdInfo(int matchId);

    MatchInfo GetRacingInfo(RaceInfo raceInfo);

    Task UpdateMatchInfoVirtualSports(VirtualSoccerRequest request);

    Task UpdateMatchInfoLeapGaming(LeapGamingRequest request);

    Task UpdateMatchInfoBetRadar(VirtualBetRadarRequest request);

    Task UpdateMatchInfoHappyFive(HappyFiveRequest request);

    Task UpdateMatchInfoRacing(VirtualRacingRequest request);

    Task<Tickets> GetAllTicketsVirtualSports(int matchId);

    Task<Tickets> GetTickets3rd(int matchId, short sportType, string resources);

    Task WriteLogVirtualSports(VirtualSoccerRequest request);

    Task WriteLog(int matchId);

    Task WriteLogBetRadar(VirtualBetRadarRequest request);

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

    public async Task<MatchInfo> GetMatch3rdInfo(int matchId)
    {
        var criteria = new Match3rdSelCriteria { MatchId = matchId };
        var match = await repository.GetAsync<MatchInfo>(criteria);
        match.IsMatch3rd = true;
        match.RefDataService = refDataService;

        return match;
    }

    public MatchInfo GetRacingInfo(RaceInfo raceInfo)
    {
        return new MatchInfo
        {
            MatchId = raceInfo.EventId,
            IsMatch3rd = true,
            EventStatus = raceInfo.EventStatus,
            LeagueId = raceInfo.LeagueId,
            SportType = raceInfo.SportType,
            KickOffTime = raceInfo.EventTime,
            VendorId = raceInfo.VendorMatchId,
            Distance = raceInfo.Distance,
            RefDataService = refDataService
        };
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

    public async Task UpdateMatchInfoHappyFive(HappyFiveRequest request)
    {
        var updateRequest = new UpdateMatchInfo3RdRequest
        {
            Command = EventStatus.Completed,
            MatchId = request.MatchId,
            Action = request.Action,
            Creator = request.AdminId
        };
        await scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(updateRequest);
    }

    public async Task UpdateMatchInfoRacing(VirtualRacingRequest request)
    {
        var betStatus = string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
            ? ActionName.Refund
            : string.Empty;

        var updateResult = new UpdateResultCriteria
        {
            EventId = request.EventId,
            BetStatus = betStatus
        };
        await repository.ExecuteAsync(updateResult);
    }

    public async Task WriteLogVirtualSports(VirtualSoccerRequest request)
    {
        var message = $"NAP: Calculate {request.Action} for matchId: {request.MatchId} score: {request.Fth}-{request.Fta}";
        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    public async Task WriteLog(int matchId)
    {
        var message = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, message);
    }

    public async Task WriteLogBetRadar(VirtualBetRadarRequest request)
    {
        var message = $@"NAP: VS BetRadar - Calculate {request.Action} for matchId: {request.MatchId}
              Scores: FT=[{request.Fth}-{request.Fta}]; HT=[{request.Hth}-{request.Hta}]
              ResourceItem Results: {request.ResourceResult}";

        await logDirectlyService.WriteLog(request.MatchId, message);
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

    public async Task<Tickets> GetAllTicketsVirtualSports(int matchId)
    {
        var criteria = new VirtualSportsTicketCriteria { MatchId = matchId };
        var tasks = appSettingsKey.Value.SiteDbServer.Select(s => GetTicketsVirtualSportsOnSite(s, criteria));

        var result = await Task.WhenAll(tasks);

        var tickets = result.SelectMany(x => x).ToList();
        var creditTickets = tickets.Where(x => !x.IsDeposit);
        var depositTickets = tickets.Where(x => x.IsDeposit);

        return (creditTickets, depositTickets);
    }

    private async Task<IEnumerable<TicketInfo>> GetTicketsVirtualSportsOnSite(string server, VirtualSportsTicketCriteria criteria)
    {
        criteria.DbServer = server;

        return await repository.FetchAsync<TicketInfo>(criteria);
    }

    public async Task<Tickets> GetTickets3rd(int matchId, short sportType, string resources)
    {
        var criteria = new VirtualSportsFiveTicketCriteria { MatchId = matchId, SportType = sportType, ResourceIds = resources };

        var tasks = appSettingsKey.Value.SiteDbServer.Select(s => GetTickets3rdOnSite(s, criteria));

        var result = await Task.WhenAll(tasks);

        var tickets = result.SelectMany(x => x).ToList();
        var creditTickets = tickets.Where(x => !x.IsDeposit);
        var depositTickets = tickets.Where(x => x.IsDeposit);

        return (creditTickets, depositTickets);
    }

    private Task<IEnumerable<TicketInfo>> GetTickets3rdOnSite(string server, VirtualSportsFiveTicketCriteria criteria)
    {
        criteria.DbServer = server;
        return repository.FetchAsync<TicketInfo>(criteria);
    }
}