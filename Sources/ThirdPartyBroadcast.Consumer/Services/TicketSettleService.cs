using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface TicketSettleService
{
    Task<List<TicketSettleInfo>> GetAllTickets(TicketSettleCriteria criteria);
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

    public async Task<List<TicketSettleInfo>> GetAllTickets(TicketSettleCriteria criteria)
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