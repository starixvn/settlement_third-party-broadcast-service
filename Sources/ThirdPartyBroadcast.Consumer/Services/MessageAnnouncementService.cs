using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface MessageAnnouncementService
{
    MessageAnnouncementInsertCriteria CreateMessageCriteria(MatchInfo matchInfo, TicketType ticketType, int adminId);

    List<MessageAnnounenceItem> CreateMessageItems(IEnumerable<TicketSettleInfo> settledTickets, int messageId);
}

public class MessageAnnouncementServiceImp(RefDataService refDataService) : MessageAnnouncementService
{
    public MessageAnnouncementInsertCriteria CreateMessageCriteria(MatchInfo matchInfo, TicketType ticketType, int adminId)
    {
        matchInfo.RefDataService = refDataService;

        return new MessageAnnouncementInsertCriteria
        {
            ContentEng = CreateMessageContent(matchInfo, ticketType, ContentLanguage.English),
            ContentCh = CreateMessageContent(matchInfo, ticketType, ContentLanguage.Cantonese),
            ContentCs = CreateMessageContent(matchInfo, ticketType, ContentLanguage.Mandarin),
            MsgContentsType = ticketType == TicketType.Settled ? MsgContentType.Settled : MsgContentType.Running,
            MatchId = matchInfo.MatchId,
            Creator = adminId,
            LeagueGroupId = matchInfo.LeagueGroupId,
        };
    }

    private static string CreateMessageContent(MatchInfo matchInfo, TicketType ticketType, string languageCode)
    {
        var template = GetTemplate(ticketType, languageCode);
        var leagueName = GetLeagueName(matchInfo, languageCode);
        var teamName = GetTeamName(matchInfo, languageCode);

        return template
            .Replace("@team_name", teamName)
            .Replace("@league_name", leagueName)
            .Replace("@KO_day_month", matchInfo.KickOffDate);
    }

    private static string GetTeamName(MatchInfo matchInfo, string languageCode) =>
        languageCode switch
        {
            ContentLanguage.Cantonese => matchInfo.TeamNameCh,
            ContentLanguage.Mandarin => matchInfo.TeamNameCs,
            _ => matchInfo.TeamName,
        };

    private static string GetLeagueName(MatchInfo matchInfo, string languageCode) =>
        languageCode switch
        {
            ContentLanguage.Cantonese => matchInfo.LeagueNameCh,
            ContentLanguage.Mandarin => matchInfo.LeagueNameCs,
            _ => matchInfo.LeagueName,
        };

    private static string GetTemplate(TicketType ticketType, string languageCode) => (ticketType, languageCode) switch
    {
        (TicketType.Running, ContentLanguage.English) => MessageAnnounceTemplate.RunningMsgEng,
        (TicketType.Running, ContentLanguage.Cantonese) => MessageAnnounceTemplate.RunningMsgCantonese,
        (TicketType.Running, ContentLanguage.Mandarin) => MessageAnnounceTemplate.RunningMsgMandarin,
        (TicketType.Settled, ContentLanguage.English) => MessageAnnounceTemplate.SettledMsgEng,
        (TicketType.Settled, ContentLanguage.Cantonese) => MessageAnnounceTemplate.SettledMsgCantonese,
        (TicketType.Settled, ContentLanguage.Mandarin) => MessageAnnounceTemplate.SettledMsgMandarin,
        _ => string.Empty,
    };

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