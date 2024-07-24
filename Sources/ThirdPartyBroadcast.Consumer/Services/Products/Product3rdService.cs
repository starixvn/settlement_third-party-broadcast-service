using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

public interface Product3rdService
{
    Task<Result> Settle(VirtualSportFiveRequest request);
}

public class Product3rdServiceImp : Product3rdService
{
    private readonly IDynamicRepository repository;
    private readonly LogDirectlyService logDirectlyService;
    private readonly CalculateService calculateService;
    private readonly TicketInfoService ticketService;
    private readonly AnnouncementService announcementService;
    private readonly RecipientMessageService recipientMessageService;
    private readonly RefDataService refDataService;

    public Product3rdServiceImp(
        IDynamicRepository repository,
        LogDirectlyService logDirectlyService,
        CalculateService calculateService,
        TicketInfoService ticketService,
        AnnouncementService announcementService,
        RefDataService refDataService,
        RecipientMessageService recipientMessageService)
    {
        this.repository = repository;
        this.logDirectlyService = logDirectlyService;
        this.calculateService = calculateService;
        this.ticketService = ticketService;
        this.announcementService = announcementService;
        this.refDataService = refDataService;
        this.recipientMessageService = recipientMessageService;
    }

    public async Task<Result> Settle(VirtualSportFiveRequest request)
    {
        var currentMatchInfo = await GetMatchInfo(request.MatchId);

        if (!TryGetSources(request, out var resources) || !ValidateResources(request.Action, resources, currentMatchInfo))
        {
            return Result.ErrorRequest();
        }

        var currentTickets = await ticketService.GetTicketsByAction(currentMatchInfo.MatchId, currentMatchInfo.SportType, request.Action);

        await UpdateMatchInfo(request, resources);

        await WriteLogPreSettle(request);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task WriteLogPreSettle(VirtualSportFiveRequest request)
    {
        var message = $@"NAP: VS5 - Calculate {request.Action} for matchId: {request.MatchId} Resource Results: {request.SelectedResources}";

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

        await recipientMessageService.Message(request.Action, matchInfo);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private async Task<MatchInfo> GetMatchInfo(int matchId)
    {
        var criteria = new Match3rdSelCriteria { MatchId = matchId };
        var match = await repository.GetAsync<MatchInfo>(criteria);
        match.RefDataService = refDataService;

        return match;
    }

    private async Task UpdateMatchInfo(VirtualSportFiveRequest request, List<ResourceItemModel> resources)
    {
        var criteria = new Match3rdUpdateCriteria
        {
            MatchId = request.MatchId,
            SelectedResources = request.SelectedResources,
            BetStatus = GetBetStatus(request.Action),
            EventStatus = GetEventStatus(request.Action, resources),
        };

        await repository.ExecuteAsync(criteria);
    }

    private static string GetBetStatus(string action) => string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) ? "Refund" : null;

    private static string GetEventStatus(string action, List<ResourceItemModel> resources)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return "completed";
        }

        var ftResult = resources.Find(resources => resources.Id == ResourceId.FullTimeResourceId);
        return string.Equals(action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
            && ftResult is not null
                ? "completed"
                : "running";
    }

    private static bool ValidateResources(string action, List<ResourceItemModel> resources, MatchInfo matchInfo)
    {
        var htResult = resources.Find(resources => resources.Id == ResourceId.HalfTimeResourceId);
        var ftResult = resources.Find(resources => resources.Id == ResourceId.FullTimeResourceId);

        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return ValidRefundResources(htResult, ftResult);
        }

        if (string.Equals(action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase))
        {
            return ScoreValidator.ValidateScore(htResult, ftResult, matchInfo);
        }

        return false;
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
        resources = new();

        try
        {
            if (request.MatchId <= 0 || string.IsNullOrEmpty(request.SelectedResources))
            {
                return false;
            }

            resources = request.SelectedResources.ToResources().Items;

            return true;
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