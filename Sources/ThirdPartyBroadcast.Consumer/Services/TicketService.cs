using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface TicketService
{
    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action);

    Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets)
}

public class TicketServiceImp : TicketService
{
    private readonly AppSettingsKey appSettingsKey;
    private readonly IDynamicRepository repository;

    public TicketServiceImp(IOptions<AppSettingsKey> options, IDynamicRepository repository)
    {
        appSettingsKey = options.Value;
        this.repository = repository;
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return await GetTicketsAllSites(new TicketSettleCriteria { MatchId = matchId });
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
        return await GetTicketsAllSites(new TicketSettleCriteria { MatchId = matchId, TransIDs = transIds });
    }

    public async Task<List<TicketSettleInfo>> GetTicketsAllSites(TicketInfoSelBase criteria)
    {
        var tasks = appSettingsKey.SiteDbServer.Select(s => GetTicketsOnSite(s, criteria));

        var result = await Task.WhenAll(tasks);

        return result.SelectMany(x => x).ToList();
    }

    private Task<IEnumerable<TicketSettleInfo>> GetTicketsOnSite(string server, TicketInfoSelBase criteria)
    {
        criteria.DbServer = server;
        return repository.FetchAsync<TicketSettleInfo>(criteria);
    }
}