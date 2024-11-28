using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface TicketInfoService
{
    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, short sportType, string action);

    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, short sportType, string action, List<ResourceItemModel> resources);

    Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, short sportType, List<TicketSettleInfo> tickets);
}

public class TicketInfoServiceImp(IOptions<AppSettingsKey> appSettingsKey, IDynamicRepository repository)
    : TicketInfoService
{
    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, short sportType, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return await GetTicketsAllSites(new TicketSettleCriteria { MatchId = matchId, SportType = sportType });
        }

        return [];
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, short sportType, string action, List<ResourceItemModel> resources)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            var resourceIds = string.Join(',', resources.Select(r => r.Id));
            return await GetTicketsAllSites(new TicketSettleCriteria { MatchId = matchId, SportType = sportType, ResourceIds = resourceIds });
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

    private async Task<List<TicketSettleInfo>> GetTicketsAllSites(TicketSettleCriteria criteria)
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