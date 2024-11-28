using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

public interface Product3rdService
{
    Task<Result> Settle(VirtualSportFiveRequest request);
}

public class Product3rdServiceImp(
    IDynamicRepository repository,
    LogDirectlyService logDirectlyService,
    CalculateService calculateService,
    TicketInfoService ticketService,
    AnnouncementService announcementService,
    MatchService matchService,
    RecipientMessageService recipientMessageService)
    : Product3rdService
{
    public async Task<Result> Settle(VirtualSportFiveRequest request)
    {
        var currentMatchInfo = await matchService.GetMatch3rdInfo(request.MatchId);

        var validationResult = ValidateResources(request, currentMatchInfo);
        if (!validationResult.IsSuccessful)
        {
            return validationResult;
        }

        var currentTickets = await ticketService.GetTicketsByAction(currentMatchInfo.MatchId, currentMatchInfo.SportType, request.Action);

        await UpdateMatchInfo(request, currentMatchInfo);

        await WriteLogPreSettle(request);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task WriteLogPreSettle(VirtualSportFiveRequest request)
    {
        var message = $"NAP: VS5 - Calculate {request.Action} for matchId: {request.MatchId} Resource Results: {request.SelectedResources}";

        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualSportFiveRequest request)
    {
        var criteria = new VirtualSportFiveSettleCriteria
        {
            MatchId = request.MatchId,
            SportType = matchInfo.SportType,
            EventDate = matchInfo.EventDate,
            Action = request.Action,
            ActionId = request.ActionId,
            SelectedResources = request.SelectedResources,
            IsReSettledTickets = request.IsReSettledTickets,
            AdminId = request.AdminId,
            Ip = request.Ip,
        };
        var settleResult = await calculateService.Execute(criteria);

        await WriteLogSettled(request, settleResult);

        await recipientMessageService.SendMessageResettle(request.Action, matchInfo, request.Resources);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private async Task UpdateMatchInfo(VirtualSportFiveRequest request, MatchInfo matchInfo)
    {
        var criteria = new Match3rdUpdateCriteria
        {
            MatchId = request.MatchId,
            SelectedResources = request.SelectedResources,
            BetStatus = GetBetStatus(request),
            EventStatus = GetEventStatus(request, matchInfo),
        };

        await repository.ExecuteAsync(criteria);
    }

    private static string GetBetStatus(VirtualSportFiveRequest request)
    {
        if (string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            var ftResult = request.Resources.Find(resources => resources.Id == ResourceId.FullTimeResourceId);

            var htResult = request.Resources.Find(resources => resources.Id == ResourceId.HalfTimeResourceId);

            return ftResult is not null && htResult is null ? "Refund FT" : "Refund";
        }

        return null;
    }

    private static string GetEventStatus(VirtualSportFiveRequest request, MatchInfo matchInfo)
    {
        if (string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return EventStatus.Completed;
        }

        var ftResult = request.Resources.Find(resources => resources.Id == ResourceId.FullTimeResourceId);
        var isCalc = string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase);

        return isCalc && ftResult is not null
                ? EventStatus.Completed
                : matchInfo.EventStatus;
    }

    private static Result ValidateResources(VirtualSportFiveRequest request, MatchInfo matchInfo)
    {
        if (!TryGetSources(request, out var resources))
        {
            return Result.ErrorRequest();
        }

        var htResult = resources.Find(r => r.Id == ResourceId.HalfTimeResourceId);
        var ftResult = resources.Find(r => r.Id == ResourceId.FullTimeResourceId);
        request.Resources = resources;

        if (string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) && !ValidRefundResources(htResult, ftResult))
        {
            return Result.ErrorRequest();
        }

        if (string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase))
        {
            var validator = ValidatorFactory.CreateValidatorChain();
            return validator.Validate(htResult, ftResult, matchInfo);
        }

        return new Result();
    }

    private static bool ValidRefundResources(ResourceItemModel htResult, ResourceItemModel ftResult)
    {
        if (ftResult == null || (!string.IsNullOrEmpty(ftResult.Away) || !string.IsNullOrEmpty(ftResult.Home)))
        {
            return false;
        }

        return htResult == null || (string.IsNullOrEmpty(htResult.Away) && string.IsNullOrEmpty(htResult.Home));
    }

    private static bool TryGetSources(VirtualSportFiveRequest request, out List<ResourceItemModel> resources)
    {
        resources = [];

        try
        {
            if (request.MatchId <= 0 || string.IsNullOrEmpty(request.SelectedResources))
            {
                return false;
            }

            resources = request.SelectedResources.ToResources().Items;

            return resources.Count != 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task WriteLogSettled(VirtualSportFiveRequest request, Result resultSettle)
    {
        var criteria = CreateWriteLogCriteria(request, resultSettle);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualSportFiveRequest request, Result resultSettle)
    {
        var resources = request.SelectedResources.ToResources().Items;
        var message = $"VS5 - Calculate({request.Action}) {(resultSettle.ErrorCode > 0 ? resultSettle.ErrorMessage : string.Empty)}";
        var criteria = new WriteLogCriteria(request.MatchId, message, request.ActionId, request.AdminId, request.Ip);
        var htResult = resources.Find(i => i.Id == ResourceId.HalfTimeResourceId) ?? new ResourceItemModel();
        var ftResult = resources.Find(i => i.Id == ResourceId.FullTimeResourceId) ?? new ResourceItemModel();

        criteria.FinalAwayScore = int.TryParse(ftResult.Away, out var away) ? away : 0;
        criteria.FinalHomeScore = int.TryParse(ftResult.Home, out var home) ? home : 0;
        criteria.HtAwayScore = int.TryParse(htResult.Away, out var htAway) ? htAway : 0;
        criteria.HtHomeScore = int.TryParse(htResult.Home, out var htHome) ? htHome : 0;

        return criteria;
    }
}