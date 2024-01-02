using Fanex.Settlement.Converter.Services;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface AnnouncementService
{
    Task Handle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, int adminId);
}

public class AnnouncementServiceImp : AnnouncementService
{
    private readonly IDynamicRepository repository;

    private readonly XmlConvertService xmlConvertService;

    private readonly MatchService matchService;

    private readonly MessageAnnouncementService messageAnnouncementService;

    public AnnouncementServiceImp(
        IDynamicRepository repository,
        XmlConvertService xmlConvertService,
        MatchService matchService,
        MessageAnnouncementService messageAnnouncementService)
    {
        this.repository = repository;
        this.xmlConvertService = xmlConvertService;
        this.matchService = matchService;
        this.messageAnnouncementService = messageAnnouncementService;
    }

    public async Task Handle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, int adminId)
    {
        var settledTickets = await matchService.GetTicketsByTransIds(matchInfo.MatchId, currentTickets);

        if (settledTickets.Count > 0)
        {
            var groupTickets = CreateGroupsStatus(currentTickets, settledTickets).ToList();
            await HandleInsertMessage(matchInfo, groupTickets, adminId);
        }
    }

    private static IEnumerable<IGrouping<TicketType, TicketSettleInfo>> CreateGroupsStatus(
        IEnumerable<TicketSettleInfo> prevSettleTickets,
        IEnumerable<TicketSettleInfo> settledTickets)
    {
        var result = settledTickets.Join(prevSettleTickets,
            st => st.TransId,
            pst => pst.TransId,
            (st, pst) => new TicketSettleInfo
            {
                TransId = st.TransId,
                MemberId = st.MemberId,
                MemberUserName = st.MemberUserName,
                AgentId = st.AgentId,
                AgentUserName = st.AgentUserName,
                MasterId = st.MasterId,
                MasterUserName = st.MasterUserName,
                SuperId = st.SuperId,
                SuperUserName = st.SuperUserName,
                StatusId = pst.StatusId,
            });

        var groupStatus = result.GroupBy(x => x.Status);

        return groupStatus;
    }

    private async Task HandleInsertMessage(
        MatchInfo matchInfo,
        IEnumerable<IGrouping<TicketType, TicketSettleInfo>> groups,
        int adminId)
    {
        foreach (var group in groups)
        {
            var messageId = await GetMessageId(matchInfo, group.Key, adminId);
            var items = messageAnnouncementService.CreateMessageItems(group, messageId);

            await CreateMessageUpload(items, adminId);
        }
    }

    private async Task<int> GetMessageId(MatchInfo matchInfo, TicketType ticketType, int adminId)
    {
        var criteria = messageAnnouncementService.CreateMessageCriteria(matchInfo, ticketType, adminId);
        return await repository.GetAsync<int>(criteria);
    }

    private async Task CreateMessageUpload(List<MessageAnnounenceItem> items, int adminId)
    {
        var message = new MessageAnnounence
        {
            Items = items,
        };

        var xmlMessage = xmlConvertService.Serialize(message);

        var criteria = new MessageUploadCriteria
        {
            XMLMessage = xmlMessage,
            CreatorId = adminId
        };

        await repository.ExecuteAsync(criteria);
    }
}