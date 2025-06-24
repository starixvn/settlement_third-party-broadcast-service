using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Criteria.LeapGaming;
using Fanex.Data.Repository;
using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using Models;

public interface LeapGamingService
{
    Task<Result> Settle(LeapGamingRequest request, string productType);
}

public class LeapGamingServiceImp(
    IMapper mapper,
    CalculateService calculateService,
    IDynamicRepository repository,
    MatchService matchService,
    TicketInfoService ticketService,
    RecipientMessageService recipientMessageService,
    AnnouncementService announcementService)
    : LeapGamingService
{
    public async Task<Result> Settle(LeapGamingRequest request, string productType)
    {
        var currentMatchInfo = await matchService.GetMatchInfo(request.MatchId);
        var validationResult = ValidateMatchScore(request, currentMatchInfo);
        if (validationResult.ErrorCode > 0)
        {
            return validationResult;
        }

        var currentTickets = await ticketService.GetTicketsByAction(currentMatchInfo.MatchId, currentMatchInfo.SportType, request.Action);

        await matchService.UpdateMatchInfoLeapGaming(request);

        await matchService.WriteLog(request.MatchId);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request, productType);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, LeapGamingRequest request, string productType)
    {
        var criteria = mapper.Map<LeapGamingCriteria>(request);
        var pendingCommand = CreateSiteDbPendingCommand(criteria, matchInfo, productType);
        var settleResult = await calculateService.Execute(criteria, pendingCommand);

        await WriteLogSettled(request, settleResult, matchInfo);

        await recipientMessageService.Message(request.Action, matchInfo);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(LeapGamingCriteria criteria, MatchInfo matchInfo, string productType)
    {
        var action = criteria.Action;
        if (IsReSettle(matchInfo.EventStatus, action))
        {
            action = ActionName.ReSettle;
        }

        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = productType,
            Action = action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }

    private static bool IsReSettle(string eventStatus, string action)
    {
        return !string.IsNullOrWhiteSpace(eventStatus)
               && string.Equals(action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
               && string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed);
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
        var actionId = GetActionId(request, matchInfo);
        var criteria = new WriteLogCriteria(request.MatchId, message, actionId, request.AdminId, request.Ip);
        var htResult = resources.Items.Find(i => i.Id == ResourceId.HalfTimeResourceId) ?? new ResourceItemModel();
        var ftResult = resources.Items.Find(i => i.Id == ResourceId.FullTimeResourceId) ?? new ResourceItemModel();

        criteria.FinalAwayScore = int.TryParse(ftResult.Away, out var away) ? away : 0;
        criteria.FinalHomeScore = int.TryParse(ftResult.Home, out var home) ? home : 0;
        criteria.HtAwayScore = int.TryParse(htResult.Away, out var htAway) ? htAway : 0;
        criteria.HtHomeScore = int.TryParse(htResult.Home, out var htHome) ? htHome : 0;

        return criteria;
    }

    private static int GetActionId(LeapGamingRequest request, MatchInfo matchInfo)
    {
        if (matchInfo.SportType == SportType.VirtualCockFighting)
        {
            return string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? ActionId.CockFightingCalc : ActionId.CockFightingRefund;
        }
        return string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? ActionId.CalculateAll : ActionId.RefundAll;
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
                    ErrorCode = ErrorCode.NotExistValue,
                    ErrorMessage = ErrorMessage.NotExistValue
                };
            }
        }
        catch (Exception)
        {
            return new Result
            {
                ErrorCode = ErrorCode.NotExistValue,
                ErrorMessage = ErrorMessage.NotExistValue
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