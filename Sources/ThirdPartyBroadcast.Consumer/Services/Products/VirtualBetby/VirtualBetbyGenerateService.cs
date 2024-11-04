using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface VirtualBetbyGenerateService
{
    Result ValidateResources(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources);

    Task UpdateMatch3rdInfo(VirtualBetbyRequest request, MatchInfo matchInfo);

    Task UpdateResultDetail3rdInfo(VirtualBetbyRequest request);

    Task WriteLogPreSettle(VirtualBetbyRequest request);

    Task WriteLogSettled(VirtualBetbyRequest request, Result result, long duration);
}

public class VirtualBetbyGenerateServiceImp(
    IDynamicRepository repository,
    GenerateMatchInfoService generateMatchInfoService,
    LogDirectlyService logDirectlyService,
    ScoreValidatorService scoreValidatorService) : VirtualBetbyGenerateService
{
    public Result ValidateResources(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        if (!TryGetSources(request, out var resources))
        {
            return Result.ErrorRequest();
        }

        request.Resources = GetMarkedList(resources, currentResources);

        var isExistedFullTime = resources.Exists(r => r.Id == ResourceId.FullTimeResourceId);

        if (string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) && isExistedFullTime)
        {
            return scoreValidatorService.Validate(request, currentResources);
        }

        return new Result();
    }

    private static List<ResourceItemModel> GetMarkedList(IEnumerable<ResourceItemModel> newResources, IEnumerable<ResourceItemModel> currentResources)
    {
        var resources = new HashSet<string>(currentResources.Select(p => p.Id));
        return newResources.Select(item => new ResourceItemModel
        {
            Id = item.Id,
            Home = item.Home,
            Away = item.Away,
            IsDuplicated = resources.Contains(item.Id)
        }).ToList();
    }

    private static bool TryGetSources(VirtualBetbyRequest request, out List<ResourceItemModel> resources)
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

    public async Task UpdateResultDetail3rdInfo(VirtualBetbyRequest request)
    {
        var criteriaResultDetail = new ResultDetail3rdUpdateCriteria
        {
            MatchId = request.MatchId,
            SportType = request.SportType,
            AdminId = request.AdminId,
            Action = request.Action,
            IsAuto = true,
            SelectedResources = request.SelectedResources
        };
        await repository.ExecuteAsync(criteriaResultDetail);
    }

    public async Task UpdateMatch3rdInfo(VirtualBetbyRequest request, MatchInfo matchInfo)
    {
        var isExisted = request.Resources.Exists(r => r.Id is ResourceId.FullTimeResourceId or ResourceId.HalfTimeResourceId);

        if (isExisted)
        {
            var criteria = new Match3rdUpdateCriteria
            {
                MatchId = request.MatchId,
                SelectedResources = request.SelectedResources,
                BetStatus = generateMatchInfoService.GetBetStatus(request, matchInfo.BetStatus),
                EventStatus = generateMatchInfoService.GetEventStatus(request, matchInfo.EventStatus),
            };

            await repository.ExecuteAsync(criteria);
        }
    }

    public async Task WriteLogPreSettle(VirtualBetbyRequest request)
    {
        var message = $"NAP: VS6 - {request.Action} for matchId: {request.MatchId} Resource Results: {request.SelectedResources}";

        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    public async Task WriteLogSettled(VirtualBetbyRequest request, Result result, long duration)
    {
        var message = result.ErrorCode > 0 ? $"<br/>{result.ErrorMessage}" : string.Empty;

        var scores = scoreValidatorService.GetInconsistentMessage(request.Resources);

        var remark = $"IsAuto: 1<br/>Results: {scores};<br/>Duration: {duration} ms{message}";

        var criteria = new WriteLogCriteria(request.MatchId, request.ActionId, request.AdminId, request.Ip)
        {
            Remark = remark
        };

        await repository.ExecuteAsync(criteria);
    }
}