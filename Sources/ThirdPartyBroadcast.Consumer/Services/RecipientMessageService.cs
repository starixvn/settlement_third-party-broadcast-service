namespace ThirdPartyBroadcast.Consumer.Services;

using System;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Models;

public interface RecipientMessageService
{
    Task Message(string action, MatchInfo matchInfo);

    Task SendMessageResettle(VirtualSportFiveRequest request, MatchInfo matchInfo);
}

public class RecipientMessageServiceImp(
    MatchService matchService,
    SendMailService sendMailService,
    IOptions<AppSettingsKey> appSettingsKey) : RecipientMessageService
{
    public async Task SendMessageResettle(VirtualSportFiveRequest request, MatchInfo matchInfo)
    {
        var htResult = request.Resources.Find(resources => resources.Id == ResourceId.HalfTimeResourceId);

        var isMatchReSettle = matchService.IsReSettleOrRefund(request.Action, matchInfo.EventStatus);

        var isHtReSettle = string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
            && htResult is not null
            && matchInfo.HtHomeScore != null && matchInfo.HtAwayScore != null
            && (int.Parse(htResult.Home) != matchInfo.HtHomeScore || int.Parse(htResult.Away) != matchInfo.HtAwayScore);

        if (!isMatchReSettle && !isHtReSettle)
        {
            return;
        }

        var (creditTickets, depositTickets) = await matchService.GetTickets3rd(matchInfo.MatchId, matchInfo.SportType, request.SelectedResources);

        var actionName = GetActionName(request.Action);
        await CreditGroup(actionName, matchInfo, creditTickets.ToList());
        await DepositGroup(actionName, matchInfo, depositTickets.ToList());
    }

    public async Task Message(string action, MatchInfo matchInfo)
    {
        if (matchService.IsReSettleOrRefund(action, matchInfo.EventStatus))
        {
            var (creditTickets, depositTickets) = await matchService.GetAllTicketsVirtualSports(matchInfo.MatchId);

            var actionName = GetActionName(action);
            await CreditGroup(actionName, matchInfo, creditTickets.ToList());
            await DepositGroup(actionName, matchInfo, depositTickets.ToList());
        }
    }

    private static string GetActionName(string action) => string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
        ? ActionName.Refund
        : ActionName.ReSettle;

    private async Task CreditGroup(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var mailId = appSettingsKey.Value.MailFunction.VirtualSport.CreditMailId;
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

    private async Task DepositGroup(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var tickets = matchService.SplitTicketByLicensee(ticketsInfo).ToList();
        var mailId = appSettingsKey.Value.MailFunction.VirtualSport.DepositMailId;

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