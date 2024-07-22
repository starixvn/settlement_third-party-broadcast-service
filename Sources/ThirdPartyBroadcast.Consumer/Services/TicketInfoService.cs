using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface TicketInfoService
{
    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, short sportType, string action);

    Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, short sportType, List<TicketSettleInfo> tickets);
}

public class TicketInfoServiceImp : TicketInfoService
{
    private readonly IOptions<AppSettingsKey> appSettingsKey;
    private readonly IDynamicRepository repository;

    public TicketInfoServiceImp(IOptions<AppSettingsKey> appSettingsKey, IDynamicRepository repository)
    {
        this.appSettingsKey = appSettingsKey;
        this.repository = repository;
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, short sportType, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return await GetTicketsAllSites(new TicketSettleCriteria { MatchId = matchId, SportType = sportType });
        }

        return [];
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, short sportType, List<TicketSettleInfo> tickets)
    {
        if (tickets.Count == 0)
        {
            return [];
        }

        var transIds = string.Join(',', tickets.Select(x => x.TransId).ToArray());
        return await GetTicketsAllSites(new TicketSettleCriteria { MatchId = matchId, SportType = sportType, TransIDs = transIds });
    }

    public async Task<List<TicketSettleInfo>> GetTicketsAllSites(TicketSettleCriteria criteria)
    {
        var tasks = appSettingsKey.Value.SiteDbServer.Select(s => GetTicketsOnSite(s, criteria));

        var result = await Task.WhenAll(tasks);

        return result.SelectMany(x => x).ToList();
    }

    private Task<IEnumerable<TicketSettleInfo>> GetTicketsOnSite(string server, TicketSettleCriteria criteria)
    {
        criteria.DbServer = server;
        return repository.FetchAsync<TicketSettleInfo>(criteria);
    }
}