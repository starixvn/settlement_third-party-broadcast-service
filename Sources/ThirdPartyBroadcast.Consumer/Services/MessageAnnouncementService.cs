using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface MessageAnnouncementService
{
    MessageAnnouncementInsertCriteria CreateMessageCriteria(MatchInfo matchInfo, string ticketStatus, int adminId);

    List<MessageAnnounenceItem> CreateMessageItems(IEnumerable<TicketSettleInfo> settledTickets, int messageId);
}

public class MessageAnnouncementServiceImp : MessageAnnouncementService
{
    public MessageAnnouncementInsertCriteria CreateMessageCriteria(MatchInfo matchInfo, string ticketStatus, int adminId)
    {
        if (string.Equals(ticketStatus, TicketType.Running, StringComparison.OrdinalIgnoreCase))
        {
            return new MessageAnnouncementInsertCriteria
            {
                ContentEng = CreateMessageContent(matchInfo, MessageAnnounceTemplate.RunningMsgEng),
                ContentCh = CreateMessageContent(matchInfo, MessageAnnounceTemplate.RunningMsgCantonese),
                ContentCs = CreateMessageContent(matchInfo, MessageAnnounceTemplate.RunningMsgMandarin),
                MsgContentsType = MsgContentType.Running,
                MatchId = matchInfo.MatchId,
                Creator = adminId,
                LeagueGroupId = matchInfo.LeagueGroupId,
            };
        }

        return new MessageAnnouncementInsertCriteria
        {
            ContentEng = CreateMessageContent(matchInfo, MessageAnnounceTemplate.SettledMsgEng),
            ContentCh = CreateMessageContent(matchInfo, MessageAnnounceTemplate.SettledMsgCantonese),
            ContentCs = CreateMessageContent(matchInfo, MessageAnnounceTemplate.SettledMsgMandarin),
            MsgContentsType = MsgContentType.Settled,
            MatchId = matchInfo.MatchId,
            Creator = adminId,
            LeagueGroupId = matchInfo.LeagueGroupId,
        };
    }

    private static string CreateMessageContent(MatchInfo matchInfo, string template)
    {
        return template
            .Replace("@league_name", matchInfo.LeagueName)
            .Replace("@KO_day_month", $"{matchInfo.KickOffTime.Day}/{matchInfo.KickOffTime.Month}");
    }

    public List<MessageAnnounenceItem> CreateMessageItems(IEnumerable<TicketSettleInfo> settledTickets, int messageId)
    {
        var ticketSettleInfos = settledTickets.ToList();
        var groupCustomer = GenerateMessageItems(ticketSettleInfos, ticket => ticket.MemberId, ticket => ticket.MemberUserName, messageId);
        var groupAgent = GenerateMessageItems(ticketSettleInfos, ticket => ticket.AgentId, ticket => ticket.AgentUserName, messageId);
        var groupMaster = GenerateMessageItems(ticketSettleInfos, ticket => ticket.MasterId, ticket => ticket.MasterUserName, messageId);
        var groupSuper = GenerateMessageItems(ticketSettleInfos, ticket => ticket.SuperId, ticket => ticket.SuperUserName, messageId);

        return groupCustomer
            .Concat(groupAgent)
            .Concat(groupMaster)
            .Concat(groupSuper).ToList();
    }

    private static IEnumerable<MessageAnnounenceItem> GenerateMessageItems(
        IEnumerable<TicketSettleInfo> settledTickets,
        Func<TicketSettleInfo, int?> idSelector,
        Func<TicketSettleInfo, string> nameSelector,
        int messageId)
    {
        return settledTickets
            .Where(ticket => idSelector(ticket) is not null)
            .GroupBy(idSelector)
            .Select(group => new MessageAnnounenceItem
            {
                TargetId = group.Key ?? 0,
                TargetName = nameSelector(group.FirstOrDefault()),
                BetTransId = string.Join(",", group.Select(ticket => ticket.TransId)),
                MessageId = messageId
            })
            .ToList();
    }
}