using System.Xml.Serialization;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.BitCoin;
using ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;
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
    private readonly TicketService ticketService;
    private readonly AnnouncementService announcementService;
    private readonly IMapper mapper;

    private const int ErrorCodeNotExistValue = 1_000_003;

    private const string ErrorMsgNotExistValue = "The input score values are not valid!";

    public Product3rdServiceImp(IDynamicRepository repository, LogDirectlyService logDirectlyService, IMapper mapper)
    {
        this.repository = repository;
        this.logDirectlyService = logDirectlyService;
        this.mapper = mapper;
    }

    public async Task<Result> Settle(VirtualSportFiveRequest request)
    {
        var currentMatchInfo = await GetMatchInfo(request.MatchId);

        var validationResult = ValidateRequest(request);
        if (validationResult.ErrorCode > 0)
        {
            return validationResult;
        }

        var currentTickets = await ticketService.GetTicketsByAction(currentMatchInfo.MatchId, currentMatchInfo.SportType, request.Action);

        await UpdateMatchInfo(request);

        await WriteLogReSettle(request);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task WriteLogReSettle(VirtualSportFiveRequest request)
    {
        var message = $@"NAP: VS 5 - Calculate {request.Action} for matchId: {request.MatchId} Resource Results: {request.SelectedResources}";

        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, Match3rd matchInfo, VirtualSportFiveRequest request)
    {
        var criteria = mapper.Map<VirtualSportFiveRequest, VirtualSportFiveSettleCriteria>(request);
        var settleResult = await calculateService.Execute(criteria);

        await WriteLogSettled(request, settleResult, matchInfo);

        //await announcementService.SendMail(request.Action, matchInfo);

        //await announcementService.SendAnnounceRefund(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private async Task WriteLogSettled(VirtualSportFiveRequest request, Result resultSettle, Match3rd matchInfo)
    {
        var criteria = CreateWriteLogCriteria(request, resultSettle, matchInfo);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualSportFiveRequest request, Result resultSettle, Match3rd matchInfo)
    {
        var resources = request.SelectedResources.ToResources();
        var message = $"VS 5- Calculate({request.Action}) {(resultSettle.ErrorCode > 0 ? resultSettle.ErrorMessage : string.Empty)}";
        var actionId = GetActionId(request);
        var criteria = new WriteLogCriteria(request.MatchId, message, actionId, request.AdminId, request.Ip);
        var htResult = resources.Items.Find(i => i.Id == ResourceId.HalfTimeResourceId) ?? new ResourceItemModel();
        var ftResult = resources.Items.Find(i => i.Id == ResourceId.FullTimeResourceId) ?? new ResourceItemModel();

        criteria.FinalAwayScore = int.TryParse(ftResult.Away, out var away) ? away : 0;
        criteria.FinalHomeScore = int.TryParse(ftResult.Home, out var home) ? home : 0;
        criteria.HtAwayScore = int.TryParse(htResult.Away, out var htAway) ? htAway : 0;
        criteria.HtHomeScore = int.TryParse(htResult.Home, out var htHome) ? htHome : 0;

        return criteria;
    }

    private Task<Match3rd> GetMatchInfo(int matchId)
    {
        var criteria = new Match3rdSelCriteria { MatchId = matchId };

        return repository.GetAsync<Match3rd>(criteria);
    }

    private async Task Notification(VirtualSportFiveRequest request, string eventStatus)
    {
        if (string.Equals(eventStatus.ToLowerInvariant(), EventStatus.Completed) || string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            var criteria = new NotifyCriteria
            {
                MatchId = request.MatchId,
                NotifyAction = string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase) ? ActionId.BitCoinRefund : ActionId.BitCoinCalc
            };

            await repository.ExecuteAsync(criteria);
        }
    }

    private async Task UpdateMatchInfo(VirtualSportFiveRequest request)
    {
        var criteria = new Match3rdUpdateCriteria
        {
            MatchId = request.MatchId,
            SelectedResources = request.SelectedResources,
            BetStatus = request.Action == "REFUND" ? "Refund" : null,
            EventStatus = request.Action == "REFUND" || request.Action == "CAL" ? "completed" : "running"
        };

        await repository.ExecuteAsync(criteria);
    }

    private static bool ValidateResult(List<ResourceItemModel> resources, string action)
    {
        return resources.TrueForAll(x => IsValidScore(x, action));
    }

    private static bool IsValidScore(ResourceItemModel result, string action)
    {
        if (string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return result is { Home: "", Away: "" };
        }
        return result != null && int.TryParse(result.Home, out _) && int.TryParse(result.Away, out _);
    }

    private static Result ValidateRequest(VirtualSportFiveRequest request)
    {
        try
        {
            var resources = request.SelectedResources.ToResources();

            if (resources == null || resources.Items.Count == 0 || !ValidateResult(resources.Items, request.Action))
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

    private static int GetActionId(VirtualSportFiveRequest request)
    {
        return string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? ActionId.VirtualSportFiveCal : ActionId.VirtualSportFiveRefund;
    }
}