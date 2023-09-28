using System.Xml.Serialization;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Criteria.LeapGaming;

public interface LeapGamingService
{
    Task<Result> Settle(LeapGamingRequest request);
}

public class LeapGamingServiceImp : LeapGamingService
{
    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly IDynamicRepository repository;

    private readonly RecipientTicketsService recipientTicketsService;

    private const int ErrorCodeNotExistValue = 1_000_003;

    private const string ErrorMsgNotExistValue = "The input score values are not valid!";

    public LeapGamingServiceImp(
        IMapper mapper,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService,
        IDynamicRepository repository,
        RecipientTicketsService recipientTicketsService)
    {
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
        this.repository = repository;
        this.recipientTicketsService = recipientTicketsService;
    }

    public async Task<Result> Settle(LeapGamingRequest request)
    {
        var criteria = mapper.Map<LeapGamingRequest, LeapGamingCriteria>(request);
        var validationResult = ValidateMatchScore(request);
        if (validationResult.ErrorCode > 0)
        {
            return validationResult;
        }

        var currentMatchInfo = await MatchStatus(request.MatchId);

        await UpdateMatchInfo(request);
        var message = logDirectlyService.Message(request.MatchId);
        await logDirectlyService.WriteLog(request.MatchId, message);

        var result = await calculateService.Execute(criteria);

        await HandleAfterSettle(request, currentMatchInfo, result);

        return result;
    }

    private async Task HandleAfterSettle(LeapGamingRequest request, MatchInfo matchInfo, Result resultSettle)
    {
        var criteria = CreateWriteLogCriteria(request, resultSettle);

        await repository.ExecuteAsync(criteria);

        await SendMail(request.Action, matchInfo);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(LeapGamingRequest request, Result resultSettle)
    {
        var resources = DeserializeResources(request.SelectedResources);
        var message = $"VS 3 - Calculate({request.Action}) {(resultSettle.ErrorCode > 0 ? resultSettle.ErrorMessage : string.Empty)}";
        var actionId = request.Action.ToLowerInvariant().Equals(ActionType.Calculate) ? ActionId.LeapGamingCalc : ActionId.LeapGamingRefund;
        var criteria = new WriteLogCriteria(request.MatchId, message, actionId, request.AdminId, request.Ip);
        var htResult = resources.Items.Find(i => i.Id == ResourceId.HalfTimeResourceId);
        var ftResult = resources.Items.Find(i => i.Id == ResourceId.FullTimeResourceId);

        criteria.FinalAwayScore = int.TryParse(ftResult!.Away, out var away) ? away : 0;
        criteria.FinalHomeScore = int.TryParse(ftResult!.Home, out var home) ? home : 0;
        criteria.HtAwayScore = int.TryParse(htResult!.Away, out var htAway) ? htAway : 0;
        criteria.HtHomeScore = int.TryParse(htResult!.Home, out var htHome) ? htHome : 0;

        return criteria;
    }

    private async Task UpdateMatchInfo(LeapGamingRequest request)
    {
        var requestUpdate = mapper.Map<LeapGamingRequest, UpdateMatchCriteria>(request);
        requestUpdate.Command = EventStatus.Completed;
        await repository.ExecuteAsync(requestUpdate);
    }

    private static Result ValidateMatchScore(LeapGamingRequest request)
    {
        try
        {
            var resources = DeserializeResources(request.SelectedResources);

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

    private static ResourcesModel DeserializeResources(string selectedResourced)
    {
        var serializer = new XmlSerializer(typeof(ResourcesModel));
        using var sr = new StringReader(selectedResourced);

        return (ResourcesModel)serializer.Deserialize(sr);
    }

    private async Task<MatchInfo> MatchStatus(int matchId)
    {
        var criteria = new LeapGamingGetMatchInfoCriteria { MatchId = matchId };
        var match = await repository.GetAsync<MatchInfo>(criteria);
        match.MatchId = matchId;

        return match;
    }

    private async Task SendMail(string action, MatchInfo matchInfo)
    {
        var eventStatusChecking = !string.IsNullOrWhiteSpace(matchInfo.EventStatus) &&
                                  string.Equals(matchInfo.EventStatus.ToLowerInvariant(), EventStatus.Completed);

        if (action.ToLowerInvariant().Equals(ActionType.Refund) || eventStatusChecking)
        {
            var criteria = new LeapGamingTicketCriteria { MatchId = matchInfo.MatchId };
            var (creditTickets, depositTickets) = await repository.FetchMultipleAsync<TicketInfo, TicketInfo>(criteria);

            var recipients = await GetRecipients();
            var actionName = string.Equals(action, ActionType.Refund, StringComparison.OrdinalIgnoreCase)
                ? ActionName.Refund
                : ActionName.ReSettle;

            recipientTicketsService.CreditTickets(actionName, creditTickets.ToList(), recipients, matchInfo);
            recipientTicketsService.DepositTickets(actionName, depositTickets.ToList(), recipients, matchInfo);
        }
    }

    private async Task<Recipient> GetRecipients()
    {
        var criteria = new MailRecipientsCriteria();
        return await repository.GetAsync<Recipient>(criteria);
    }

    private static bool ValidateResult(IReadOnlyCollection<ResourceItemModel> items, string action)
    {
        var htResult = items.FirstOrDefault(i => i.Id == ResourceId.HalfTimeResourceId);
        var ftResult = items.FirstOrDefault(i => i.Id == ResourceId.FullTimeResourceId);

        return IsValidResult(htResult, action) && IsValidResult(ftResult, action);
    }

    private static bool IsValidResult(ResourceItemModel result, string action)
    {
        if (action.ToLowerInvariant() == ActionType.Refund)
        {
            return result is { Home: "", Away: "" };
        }
        return result != null && int.TryParse(result.Home, out _) && int.TryParse(result.Away, out _);
    }
}