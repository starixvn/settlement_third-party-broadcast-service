using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Criteria.LeapGaming;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Models;

public interface LeapGamingService
{
    Task<Result> Settle(LeapGamingRequest request);
}

public class LeapGamingServiceImp(
    IMapper mapper,
    CalculateService calculateService,
    IDynamicRepository repository,
    SendMailService sendMailService,
    MatchService matchService,
    AnnouncementService announcementService,
    IOptions<AppSettingsKey> appSettingsKey)
    : LeapGamingService
{
    private MailFunction MailFunction => appSettingsKey.Value.MailFunction;

    private const int ErrorCodeNotExistValue = 1_000_003;

    private const string ErrorMsgNotExistValue = "The input score values are not valid!";

    public async Task<Result> Settle(LeapGamingRequest request)
    {
        var currentMatchInfo = await matchService.GetMatchInfoLeapGaming(request.MatchId);
        var validationResult = ValidateMatchScore(request, currentMatchInfo);
        if (validationResult.ErrorCode > 0)
        {
            return validationResult;
        }

        var currentTickets = await matchService.GetTicketsByAction(request.MatchId, request.Action);

        await matchService.UpdateMatchInfoLeapGaming(request);

        await matchService.WriteLogLeapGaming(request.MatchId);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, LeapGamingRequest request)
    {
        var criteria = mapper.Map<LeapGamingRequest, LeapGamingCriteria>(request);
        var settleResult = await calculateService.Execute(criteria);

        await WriteLogSettled(request, settleResult, matchInfo);

        await SendMail(request.Action, matchInfo);

        await announcementService.Handle(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private async Task WriteLogSettled(LeapGamingRequest request, Result resultSettle, MatchInfo matchInfo)
    {
        var criteria = CreateWriteLogCriteria(request, resultSettle, matchInfo);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(LeapGamingRequest request, Result resultSettle, MatchInfo matchInfo)
    {
        var resources = DeserializeResources(request.SelectedResources);
        var message = $"{matchInfo.SportName}- Calculate({request.Action}) {(resultSettle.ErrorCode > 0 ? resultSettle.ErrorMessage : string.Empty)}";
        var actionId = string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? ActionId.LeapGamingCalc : ActionId.LeapGamingRefund;
        var criteria = new WriteLogCriteria(request.MatchId, message, actionId, request.AdminId, request.Ip);
        var htResult = resources.Items.Find(i => i.Id == ResourceId.HalfTimeResourceId) ?? new ResourceItemModel();
        var ftResult = resources.Items.Find(i => i.Id == ResourceId.FullTimeResourceId) ?? new ResourceItemModel();

        criteria.FinalAwayScore = int.TryParse(ftResult.Away, out var away) ? away : 0;
        criteria.FinalHomeScore = int.TryParse(ftResult.Home, out var home) ? home : 0;
        criteria.HtAwayScore = int.TryParse(htResult.Away, out var htAway) ? htAway : 0;
        criteria.HtHomeScore = int.TryParse(htResult.Home, out var htHome) ? htHome : 0;

        return criteria;
    }

    private static Result ValidateMatchScore(LeapGamingRequest request, MatchInfo matchInfo)
    {
        try
        {
            var resources = DeserializeResources(request.SelectedResources);

            if (resources == null || resources.Items.Count == 0 || !ValidateResult(resources.Items, request.Action, matchInfo))
            {
                return new Result
                {
                    ErrorCode = ErrorCodeNotExistValue,
                    ErrorMessage = ErrorMsgNotExistValue
                };
            }
        }
        catch (Exception)
        {
            return new Result
            {
                ErrorCode = ErrorCodeNotExistValue,
                ErrorMessage = ErrorMsgNotExistValue
            };
        }

        return new Result();
    }

    private static ResourcesModel DeserializeResources(string selectedResourced)
    {
        var serializer = new XmlSerializer(typeof(ResourcesModel));
        using var sr = new StringReader(selectedResourced);

        return (ResourcesModel)serializer.Deserialize(sr);
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

        var tickets = SplitTicketByLicensee(ticketsInfo).ToList();
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

    private static bool ValidateResult(IReadOnlyCollection<ResourceItemModel> items, string action, MatchInfo match)
    {
        if (match?.SportType == SportType.VirtualCockFighting)
        {
            return ValidateFtResult(items, action);
        }

        return ValidateHtResult(items, action) && ValidateFtResult(items, action);
    }

    private static bool ValidateHtResult(IReadOnlyCollection<ResourceItemModel> items, string action)
    {
        var htResult = items.FirstOrDefault(i => i.Id == ResourceId.HalfTimeResourceId);

        return IsValidResult(htResult, action);
    }

    private static bool ValidateFtResult(IReadOnlyCollection<ResourceItemModel> items, string action)
    {
        var ftResult = items.FirstOrDefault(i => i.Id == ResourceId.FullTimeResourceId);

        return IsValidResult(ftResult, action);
    }

    private static bool IsValidResult(ResourceItemModel result, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return result is { Home: "", Away: "" };
        }
        return result != null && int.TryParse(result.Home, out _) && int.TryParse(result.Away, out _);
    }
}