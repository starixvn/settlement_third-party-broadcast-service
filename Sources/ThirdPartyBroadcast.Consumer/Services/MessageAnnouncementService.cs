using Fanex.Logging;
using ThirdPartyBroadcast.Consumer.Constants;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface MessageAnnouncementService
{
    MessageAnnouncementInsertCriteria CreateMessageCriteria(MatchInfo matchInfo, TicketType ticketType, int adminId);

    List<MessageAnnounenceItem> CreateMessageItems(IEnumerable<TicketSettleInfo> settledTickets, int messageId);
}

public class MessageAnnouncementServiceImp : MessageAnnouncementService
{
    private readonly RefDataService refDataService;

    public MessageAnnouncementServiceImp(RefDataService refDataService)
    {
        this.refDataService = refDataService;
    }

    public MessageAnnouncementInsertCriteria CreateMessageCriteria(MatchInfo matchInfo, TicketType ticketType, int adminId)
    {
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

    private string CreateMessageContent(MatchInfo matchInfo, TicketType ticketType, string languageCode)
    {
        var template = GetTemplate(ticketType, languageCode);
        var leagueName = GetLeagueName(matchInfo.LeagueId, languageCode);
        var homeName = GetTeamName(matchInfo.HomeId, languageCode);
        var awayName = GetTeamName(matchInfo.AwayId, languageCode);

        LogService.Warn($"MessageContent {leagueName} {homeName}-vs- {awayName}");

        return template
            .Replace("@home_name", homeName)
            .Replace("@away_name", awayName)
            .Replace("@league_name", leagueName)
            .Replace("@KO_day_month", $"{matchInfo.KickOffTime.Day}/{matchInfo.KickOffTime.Month}");
    }

    private string GetTeamName(int teamId, string languageCode)
    {
        string GetTeamNameByLanguage(string langCode) => refDataService.GetTeamName(teamId, langCode);

        var teamName = GetTeamNameByLanguage(ContentLanguage.English);
        var teamNameCh = GetTeamNameByLanguage(ContentLanguage.Cantonese);
        var teamNameCs = GetTeamNameByLanguage(ContentLanguage.Mandarin);

        return languageCode switch
        {
            ContentLanguage.Cantonese => !string.IsNullOrEmpty(teamNameCh) ? teamNameCh : teamName,
            ContentLanguage.Mandarin => !string.IsNullOrEmpty(teamNameCs) ? teamNameCs : teamName,
            _ => teamName,
        };
    }

    private string GetLeagueName(int leagueId, string languageCode)
    {
        string GetLeagueNameByLanguage(string langCode) => refDataService.GetLeagueName(leagueId, langCode);

        var leagueName = GetLeagueNameByLanguage(ContentLanguage.English);
        var leagueNameCh = GetLeagueNameByLanguage(ContentLanguage.Cantonese);
        var leagueNameCs = GetLeagueNameByLanguage(ContentLanguage.Mandarin);

        LogService.Warn($"GetLeagueName {leagueName} {leagueNameCh}-vs- {leagueNameCs}");
        return languageCode switch
        {
            ContentLanguage.Cantonese => !string.IsNullOrEmpty(leagueNameCh) ? leagueNameCh : leagueName,
            ContentLanguage.Mandarin => !string.IsNullOrEmpty(leagueNameCs) ? leagueNameCs : leagueName,
            _ => leagueName,
        };
    }

    private string GetTemplate(TicketType ticketType, string languageCode) => (ticketType, languageCode) switch
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