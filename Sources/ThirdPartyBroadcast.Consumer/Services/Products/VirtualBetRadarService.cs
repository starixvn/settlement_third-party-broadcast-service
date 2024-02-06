using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Constants;
using Criteria.VirtualBetRadar;
using Models;
using System;

public interface VirtualBetRadarService
{
    Task<Result> Settle(VirtualBetRadarRequest request);
}

public class VirtualBetRadarServiceImp(
    IDynamicRepository repository,
    SendMailService sendMailService,
    IMapper mapper,
    CalculateService calculateService,
    MatchService matchService,
    AnnouncementService announcementService,
    IOptions<AppSettingsKey> appSettingsKey)
    : VirtualBetRadarService
{
    private MailFunction MailFunction => appSettingsKey.Value.MailFunction;

    public async Task<Result> Settle(VirtualBetRadarRequest request)
    {
        var betRadarValidationCriteria = mapper.Map<VirtualBetRadarRequest, BetRadarValidationCriteria>(request);
        var resultValidation = await calculateService.ExecuteValidationMultiServer(betRadarValidationCriteria);

        if (!resultValidation.IsSuccessful)
        {
            return resultValidation;
        }

        var currentMatchInfo = await matchService.GetMatchInfo(request.MatchId);
        var currentTickets = await matchService.GetTicketsByAction(request.MatchId, request.Action);

        await matchService.UpdateMatchInfoBetRadar(request);

        await matchService.WriteLogBetRadar(request);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualBetRadarRequest request)
    {
        var criteria = mapper.Map<VirtualBetRadarRequest, VirtualBetRadarCriteria>(request);
        var settleResult = await calculateService.Execute(criteria);

        await WriteLogSettled(request, settleResult);

        await SendMail(request.Action, matchInfo);

        await announcementService.Handle(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private async Task WriteLogSettled(VirtualBetRadarRequest request, Result resultSettle)
    {
        var criteria = CreateWriteLogCriteria(request, resultSettle);

        await repository.ExecuteAsync(criteria);
    }

    private async Task SendMail(string action, MatchInfo matchInfo)
    {
        if (matchService.IsReSettleOrRefund(action, matchInfo.EventStatus))
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

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualBetRadarRequest request, Result settleResult)
    {
        var actionId = ActionIdValue(request.Action);

        var criteria = new WriteLogCriteria(request.MatchId, actionId, request.AdminId, request.Ip)
        {
            FinalAwayScore = request.Fta,
            FinalHomeScore = request.Fth,
            HtAwayScore = request.Hta,
            HtHomeScore = request.Hth,
            Remark = $"VS BetRadar - Calculate ({request.Action}) {(settleResult.ErrorCode > 0 ? settleResult.ErrorMessage : string.Empty)}"
        };
        return criteria;
    }

    private static int ActionIdValue(string actionName) => string.Equals(actionName, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? ActionId.VirtualBetRadarCalc
        : ActionId.VirtualBetRadarRefund;
}