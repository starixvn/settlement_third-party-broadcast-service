namespace ThirdPartyBroadcast.Consumer.Services;

using Microsoft.Extensions.Options;
using Models;
using System;

public interface RecipientMessageService
{
    Task Message(string action, MatchInfo matchInfo);

    Task Message3rd(string action, MatchInfo matchInfo, List<ResourceItemModel> resources);

    Task SendMessageResettle(string action, MatchInfo matchInfo, List<ResourceItemModel> resources);
}

public class RecipientMessageServiceImp(
    MatchService matchService,
    SendMailService sendMailService,
    IOptions<AppSettingsKey> appSettingsKey) : RecipientMessageService
{
    private MailInfo MailVirtualSport => appSettingsKey.Value.MailFunction.VirtualSport;

    private MailInfo MailVirtualBetby => appSettingsKey.Value.MailFunction.VirtualBetby;

    public async Task SendMessageResettle(string action, MatchInfo matchInfo, List<ResourceItemModel> resources)
    {
        var htResult = resources.Find(resource => resource.Id == ResourceId.HalfTimeResourceId);

        var isMatchReSettle = matchService.IsReSettleOrRefund(action, matchInfo.EventStatus);

        var isHtReSettle = IsHtResettle(action, htResult, matchInfo);

        if (!isMatchReSettle && !isHtReSettle)
        {
            return;
        }
        var resourceIds = string.Join(',', resources.Select(r => r.Id));
        var (creditTickets, depositTickets) = await matchService.GetTickets3rd(matchInfo.MatchId, matchInfo.SportType, resourceIds);

        var actionName = GetActionName(action);
        await CreditGroup(actionName, matchInfo, creditTickets.ToList(), MailVirtualSport.CreditMailId);
        await DepositGroup(actionName, matchInfo, depositTickets.ToList(), MailVirtualSport.DepositMailId);
    }

    private static bool IsHtResettle(string action, ResourceItemModel htResult, MatchInfo matchInfo)
    {
        if (!string.Equals(action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) || htResult is null || matchInfo.HtHomeScore is null || matchInfo.HtAwayScore is null)
        {
            return false;
        }

        return int.Parse(htResult.Home) != matchInfo.HtHomeScore || int.Parse(htResult.Away) != matchInfo.HtAwayScore;
    }

    public async Task Message3rd(string action, MatchInfo matchInfo, List<ResourceItemModel> resources)
    {
        if (IsReSettleByResourceOrRefund(action, resources))
        {
            var resourceIds = GetResourcesIds(action, resources);

            var (creditTickets, depositTickets) = await matchService.GetTickets3rd(matchInfo.MatchId, matchInfo.SportType, resourceIds);

            var actionName = GetActionName(action);
            await CreditGroup(actionName, matchInfo, creditTickets.ToList(), MailVirtualBetby.CreditMailId);
            await DepositGroup(actionName, matchInfo, depositTickets.ToList(), MailVirtualBetby.DepositMailId);
        }
    }

    private static string GetResourcesIds(string action, IEnumerable<ResourceItemModel> resources)
    {
        var resettleResource = string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
            ? resources
            : resources.Where(r => r.IsDuplicated).ToList();

        return string.Join(',', resettleResource.Select(r => r.Id));
    }

    private static bool IsReSettleByResourceOrRefund(string action, IEnumerable<ResourceItemModel> resources)
    {
        var resettleResource = resources.Where(r => r.IsDuplicated).ToList();
        return resettleResource.Count > 0 || string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase);
    }

    public async Task Message(string action, MatchInfo matchInfo)
    {
        if (matchService.IsReSettleOrRefund(action, matchInfo.EventStatus))
        {
            var (creditTickets, depositTickets) = await matchService.GetAllTicketsVirtualSports(matchInfo.MatchId);

            var actionName = GetActionName(action);

            await CreditGroup(actionName, matchInfo, creditTickets.ToList(), MailVirtualSport.CreditMailId);
            await DepositGroup(actionName, matchInfo, depositTickets.ToList(), MailVirtualSport.DepositMailId);
        }
    }

    private static string GetActionName(string action) => string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
        ? ActionName.Refund
        : ActionName.ReSettle;

    private async Task CreditGroup(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo, int mailId)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

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

    private async Task DepositGroup(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo, int mailId)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var tickets = matchService.SplitTicketByLicensee(ticketsInfo).ToList();

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
}