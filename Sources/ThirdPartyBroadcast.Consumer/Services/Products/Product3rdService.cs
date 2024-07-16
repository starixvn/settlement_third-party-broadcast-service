using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AutoMapper;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.BitCoin;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;
using ThirdPartyBroadcast.Consumer.Models;

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
        var match = await GetMatchInfo(request.MatchId);

        var validationResult = ValidateMatchScore(request);
        if (validationResult.ErrorCode > 0)
        {
            return validationResult;
        }

        var currentTickets = await GetTicketsByAction(request.MatchId, request.Action);

        await matchService.UpdateMatchInfoLeapGaming(request);

        await matchService.WriteLogLeapGaming(request.MatchId);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task WriteLog(int matchId)
    {
        var logMessage = logDirectlyService.Message(matchId);
        await logDirectlyService.WriteLog(matchId, logMessage);
    }

    private Task<Match3rd> GetMatchInfo(int matchId)
    {
        var criteria = new Match3rdSelCriteria { MatchId = matchId };

        return repository.GetAsync<Match3rd>(criteria);
    }

    private Task GetTicketsByAction(int matchId, string action)
    {
        var criteria = new TicketInfo3rdSelCriteria
        {
        };
    }

    private async Task<List<TicketSettleInfo>> GetTicketsAllSites(TicketSettleCriteria criteria)
    {
        var result = new List<TicketSettleInfo>();
        foreach (var server in appSettingsKey.Value.SiteDbServer)
        {
            criteria.DbServer = server;

            var ticketInfo = await repository.FetchAsync<TicketSettleInfo>(criteria);

            result.AddRange(ticketInfo);
        }

        return result;
    }

    private Task<IEnumerable<TicketSettleInfo>> GetTicketsOnSite(string server, TicketSettleCriteria criteria)
    {
        criteria.DbServer = server;
        return repository.FetchAsync<TicketSettleInfo>(criteria);
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
            Match3rdId = request.MatchId,
            Action = request.Action,
            SelectedResources = request.SelectedResources
        };

        await repository.ExecuteAsync(criteria);
    }

    private static ResourcesModel DeserializeResources(string selectedResourced)
    {
        var serializer = new XmlSerializer(typeof(ResourcesModel));
        using var sr = new StringReader(selectedResourced);

        return (ResourcesModel)serializer.Deserialize(sr);
    }

    private static bool ValidateResult(IReadOnlyCollection<ResourceItemModel> items, string action)
    {
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

    private static Result ValidateMatchScore(VirtualSportFiveRequest request)
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

    private static int GetActionId(VirtualSportFiveRequest request)
    {
        return string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? ActionId.VirtualSportFiveCal : ActionId.VirtualSportFiveRefund;
    }
}