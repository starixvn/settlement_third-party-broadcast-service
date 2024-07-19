using Fanex.Settlement.Converter.Services;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface AnnouncementService
{
    Task SendAnnounceRefund(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, int adminId);

    Task SendMail(string action, MatchInfo matchInfo);
}

public class AnnouncementServiceImp(
    IDynamicRepository repository,
    XmlConvertService xmlConvertService,
    MatchService matchService,
    MessageAnnouncementService messageAnnouncementService,
    SendMailService sendMailService,
    TicketService ticketService,
    IOptions<AppSettingsKey> appSettingsKey
)
    : AnnouncementService
{
    private MailFunction MailFunction => appSettingsKey.Value.MailFunction;

    public async Task SendAnnounceRefund(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, int adminId)
    {
        var settledTickets = await ticketService.GetTicketsByTransIds(matchInfo.MatchId, matchInfo.SportType, currentTickets);

        if (settledTickets.Count > 0)
        {
            var groupTickets = CreateGroupsStatus(currentTickets, settledTickets).ToList();
            await HandleInsertMessage(matchInfo, groupTickets, adminId);
        }
    }

    public async Task SendMail(string action, MatchInfo matchInfo)
    {
        if (matchService.IsReSettleOrRefund(action, matchInfo.EventStatus))
        {
            var (creditTickets, depositTickets) = await matchService.GetAllTicketsVirtualSports(matchInfo.MatchId);

            var actionName = GetActionName(action);

            await RecipientGroupCredit(actionName, matchInfo, creditTickets.ToList());
            await RecipientGroupDeposit(actionName, matchInfo, depositTickets.ToList());
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

    private async Task RecipientGroupCredit(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var mailId = MailFunction.VirtualSport.CreditMailId;
        var mailModel = new NotificationModel
        {
            MatchInfo = matchInfo,
            Action = actionName,
            Type = EmailType.Credit,
            MailId = mailId,
            TicketsInfo = ticketsInfo,
            TotalTickets = ticketsInfo.Count
        };

        await sendMailService.SendMailByFunctionId(mailModel);
    }

    private async Task RecipientGroupDeposit(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var tickets = matchService.SplitTicketByLicensee(ticketsInfo).ToList();
        var mailId = MailFunction.VirtualSport.DepositMailId;

        var mailModel = new NotificationModel
        {
            MatchInfo = matchInfo,
            Action = actionName,
            Type = EmailType.Deposit,
            MailId = mailId,
            TicketsInfo = tickets,
            TotalTickets = ticketsInfo.Count
        };

        await sendMailService.SendMailByFunctionId(mailModel);
    }

    private static string GetActionName(string action) => string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
        ? ActionName.Refund
        : ActionName.ReSettle;
}