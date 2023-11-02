using System.Security.Permissions;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface TicketSettleService
{
    Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action);

    Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets);
}

public class TicketSettleServiceImp : TicketSettleService
{
    private readonly IDynamicRepository repository;
    private readonly AppSettingsKey appSettings;

    public TicketSettleServiceImp(IDynamicRepository repository, IOptions<AppSettingsKey> appSettingsKey)
    {
        this.repository = repository;
        this.appSettings = appSettingsKey.Value;
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByAction(int matchId, string action)
    {
        if (action.ToLowerInvariant().Equals(ActionType.Refund))
        {
            return await GetAllTickets(new TicketSettleCriteria { MatchId = matchId });
        }

        return new List<TicketSettleInfo>();
    }

    public async Task<List<TicketSettleInfo>> GetTicketsByTransIds(int matchId, List<TicketSettleInfo> tickets)
    {
        if (tickets.Count != 0)
        {
            var transIDs = string.Join(',', tickets.Select(x => x.TransId).ToArray());
            return await GetAllTickets(new TicketSettleCriteria { MatchId = matchId, TransIDs = transIDs });
        }

        return new List<TicketSettleInfo>();
    }

    private async Task<List<TicketSettleInfo>> GetAllTickets(TicketSettleCriteria criteria)
    {
        try
        {
            var result = new List<TicketSettleInfo>();
            foreach (var server in appSettings.SiteDbServer.Where(x => !x.Equals(DbName.DbTrans)))
            {
                criteria.DbServer = server;

                var ticketInfo = await repository.FetchAsync<TicketSettleInfo>(criteria);

                result.AddRange(ticketInfo);
            }

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}