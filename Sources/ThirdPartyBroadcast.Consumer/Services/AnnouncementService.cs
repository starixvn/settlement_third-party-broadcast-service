using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.Logging;
using Fanex.Settlement.Converter.Services;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Models;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface AnnouncementService
{
    IEnumerable<IGrouping<string, TicketSettleInfo>> CreateGroupsStatus(IEnumerable<TicketSettleInfo> prevSettleTickets, IEnumerable<TicketSettleInfo> settledTickets);

    Task<Result> Handle(MatchInfo matchInfo, IEnumerable<IGrouping<string, TicketSettleInfo>> groups, int adminId);
}

public class AnnouncementServiceImp : AnnouncementService
{
    private IDynamicRepository repository;
    private readonly XmlConvertService xmlConvertService;

    public AnnouncementServiceImp(IDynamicRepository repository, XmlConvertService xmlConvertService)
    {
        this.repository = repository;
        this.xmlConvertService = xmlConvertService;
    }

    public IEnumerable<IGrouping<string, TicketSettleInfo>> CreateGroupsStatus(
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

    public async Task<Result> Handle(MatchInfo matchInfo, IEnumerable<IGrouping<string, TicketSettleInfo>> groups, int adminId)
    {
        foreach (var group in groups)
        {
            var insertCriteria = CreateAnnounceInsertCriteria(matchInfo, group.Key, adminId);

            var messageId = await Insert(insertCriteria);

            if (messageId <= 0)
            {
                return new Result
                {
                    ErrorCode = -1,
                };
            }

            var items = CreateMessageAnnounenceItems(group, messageId);
            var message = new MessageAnnounence
            {
                Items = items,
            };
            var uploadCriteria = CreateMessageUploadCriteria(message, adminId);

            var result = await Upload(uploadCriteria);

            if (!result.IsSuccessful)
            {
                return result;
            }
        }

        return new Result();
    }

    private MessageAnnouncementInsertCriteria CreateAnnounceInsertCriteria(MatchInfo matchInfo, string ticketStatus, int adminId)
    {
        var isRunningRefund = ticketStatus == TicketType.Running;

        var criteria = new MessageAnnouncementInsertCriteria
        {
            Content_en = CreateMessageContent(matchInfo, isRunningRefund ? MessageAnnounceTemplate.RunningMsgEng : MessageAnnounceTemplate.SettledMsgEng),
            Content_ch = CreateMessageContent(matchInfo, isRunningRefund ? MessageAnnounceTemplate.RunningMsgCantonese : MessageAnnounceTemplate.SettledMsgCantonese),
            Content_cs = CreateMessageContent(matchInfo, isRunningRefund ? MessageAnnounceTemplate.RunningMsgMandarin : MessageAnnounceTemplate.SettledMsgMandarin),
            MsgContentsType = isRunningRefund ? MsgContentType.Running : MsgContentType.Settled,
            MatchId = matchInfo.MatchId,
            Creator = adminId
        };

        return criteria;
    }

    private MessageUploadCriteria CreateMessageUploadCriteria(MessageAnnounence message, int adminId)
    {
        var xmlMessage = xmlConvertService.Serialize(message);

        return new MessageUploadCriteria
        {
            XMLMessage = xmlMessage,
            CreatorId = adminId
        };
    }

    private string CreateMessageContent(MatchInfo matchInfo, string template)
    {
        return template
               .Replace("@league_name", matchInfo.LeagueName)
               .Replace("@KO_day_month", $"{matchInfo.KickOffTime.Day}/{matchInfo.KickOffTime.Month}");
    }

    private List<MessageAnnounenceItem> CreateMessageAnnounenceItems(IEnumerable<TicketSettleInfo> settledTickets, int messageId)
    {
        var groupCust = GenerateMessageAnnounenceItems(settledTickets, ticket => ticket.MemberId, ticket => ticket.MemberUserName, messageId);
        var groupAgent = GenerateMessageAnnounenceItems(settledTickets, ticket => ticket.AgentId, ticket => ticket.AgentUserName, messageId);
        var groupMaster = GenerateMessageAnnounenceItems(settledTickets, ticket => ticket.MasterId, ticket => ticket.MasterUserName, messageId);
        var groupSuper = GenerateMessageAnnounenceItems(settledTickets, ticket => ticket.SuperId, ticket => ticket.SuperUserName, messageId);

        return groupCust
            .Concat(groupAgent)
            .Concat(groupMaster)
            .Concat(groupSuper).ToList();
    }

    private List<MessageAnnounenceItem> GenerateMessageAnnounenceItems(
     IEnumerable<TicketSettleInfo> settledTickets,
     Func<TicketSettleInfo, int?> idSelector,
     Func<TicketSettleInfo, string> nameSelector,
     int messageId)
    {
        return settledTickets
            .Where(ticket => idSelector(ticket) is not null)
            .GroupBy(ticket => idSelector(ticket))
            .Select(group => new MessageAnnounenceItem
            {
                TargetId = group.Key ?? 0,
                TargetName = nameSelector(group.FirstOrDefault()),
                BetTransId = string.Join(",", group.Select(ticket => ticket.TransId)),
                MessageId = messageId
            })
            .ToList();
    }

    private async Task<int> Insert(MessageAnnouncementInsertCriteria criteria)
    {
        try
        {
            var messageId = await repository.GetAsync<int>(criteria);

            return messageId;
        }
        catch (Exception ex)
        {
            LogService.Error($"MessageAnnouncementInsert error: {ex.Message}");
            return -1;
        }
    }

    private async Task<Result> Upload(MessageUploadCriteria criteria)
    {
        try
        {
            await repository.ExecuteAsync(criteria);

            return new Result
            {
                ErrorCode = criteria.ErrorCode,
                ErrorMessage = criteria.ErrorMsg
            };
        }
        catch (Exception ex)
        {
            LogService.Error($"MessageUpload error: {ex.Message}");
            return new Result
            {
                ErrorCode = criteria.ErrorCode,
            };
        }
    }
}