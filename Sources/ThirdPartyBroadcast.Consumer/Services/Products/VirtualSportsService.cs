namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.VirtualSports;
using Microsoft.Extensions.Options;
using Models;
using System;
using System.Collections.Generic;

public interface VirtualSportsService
{
    Task<Result> Settle(VirtualSoccerRequest request);
}

public class VirtualSportsServiceImp : VirtualSportsService
{
    private readonly MatchService matchService;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly AnnouncementService announcementService;

    private readonly SendMailService sendMailService;

    private readonly AppSettingsKey appSettingsKey;

    private const string Subject = "Virtual Sports";

    public VirtualSportsServiceImp(
        MatchService matchService,
        IMapper mapper,
        CalculateService calculateService,
        AnnouncementService announcementService,
        SendMailService sendMailService,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.matchService = matchService;
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.announcementService = announcementService;
        this.sendMailService = sendMailService;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task<Result> Settle(VirtualSoccerRequest request)
    {
        var matchInfo = await matchService.GetMatchInfoVirtualSports(request.MatchId);
        var currentTickets = await matchService.GetTicketsByAction(request.MatchId, request.Action);

        await matchService.UpdateMatchInfoVirtualSports(request);

        await matchService.WriteLogVirtualSports(request);

        var result = await HandleSettle(currentTickets, matchInfo, request);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualSoccerRequest request)
    {
        var criteria = mapper.Map<VirtualSoccerRequest, VirtualSportsCriteria>(request);
        var settleResult = await calculateService.Execute(criteria);

        await SendMail(request.Action, matchInfo);

        await announcementService.Handle(currentTickets, matchInfo, criteria.AdminId);

        return settleResult;
    }

    private async Task SendMail(string action, MatchInfo matchInfo)
    {
        if (IsReSettleOrRefund(action, matchInfo.EventStatus))
        {
            var (creditTickets, depositTickets) = await matchService.GetAllTicketsVirtualSports(matchInfo.MatchId);

            var actionName = GetActionName(action);
            await RecipientGroupCredit(actionName, matchInfo, creditTickets.ToList());
            await RecipientGroupDeposit(actionName, matchInfo, depositTickets.ToList());
        }
    }

    private async Task RecipientGroupCredit(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var mailId = appSettingsKey.MailFunction.VirtualSport.CreditMailId;
        var mailModel = new NotificationModel
        {
            Subject = Subject,
            MatchInfo = matchInfo,
            Action = actionName,
            Type = EmailType.Credit,
            MailId = mailId,
            TicketsInfo = ticketsInfo,
            TotalTickets = ticketsInfo.Count()
        };

        await sendMailService.SendMailByFunctionId(mailModel);
    }

    private async Task RecipientGroupDeposit(string actionName, MatchInfo matchInfo, IReadOnlyCollection<TicketInfo> ticketsInfo)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var tickets = SplitTicketByLicensee(ticketsInfo).ToList();
        var mailId = appSettingsKey.MailFunction.VirtualSport.DepositMailId;

        var mailModel = new NotificationModel
        {
            Subject = Subject,
            MatchInfo = matchInfo,
            Action = actionName,
            Type = EmailType.Deposit,
            MailId = mailId,
            TicketsInfo = tickets,
            TotalTickets = ticketsInfo.Count()
        };

        await sendMailService.SendMailByFunctionId(mailModel);
    }

    private static IEnumerable<TicketInfo> SplitTicketByLicensee(IReadOnlyCollection<TicketInfo> ticketInfos)
    {
        var distinctLicensees = ticketInfos.Select(ticket => ticket.SiteName).Distinct();

        return distinctLicensees.Select(licensee => new TicketInfo
        {
            TicketsInfo = ticketInfos.Where(ticket => ticket.SiteName == licensee).ToList()
        });
    }

    private static bool IsReSettleOrRefund(string action, string eventStatus) =>
        (!string.IsNullOrWhiteSpace(eventStatus) && string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed))
        || string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase);

    private static string GetActionName(string action) => string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
        ? ActionName.Refund
        : ActionName.ReSettle;
}