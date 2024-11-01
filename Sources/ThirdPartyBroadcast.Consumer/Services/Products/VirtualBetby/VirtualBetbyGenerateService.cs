using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface VirtualBetbyGenerateService
{
    Result ValidateResources(VirtualBetbyRequest request, List<ResourceItemModel> currentResources);

    Task UpdateMatchInfo(VirtualBetbyRequest request, MatchInfo matchInfo);

    Task WriteLogPreSettle(VirtualBetbyRequest request);

    Task WriteLogSettled(VirtualBetbyRequest request, Result result);
}

public class VirtualBetbyGenerateServiceImp(
    IDynamicRepository repository,
    GenerateMatchInfoService generateMatchInfoService,
    LogDirectlyService logDirectlyService,
    ScoreValidatorService scoreValidatorService) : VirtualBetbyGenerateService
{
    public Result ValidateResources(VirtualBetbyRequest request, List<ResourceItemModel> currentResources)
    {
        if (!TryGetSources(request, out var resources))
        {
            return Result.ErrorRequest();
        }

        request.Resources = resources;

        var isExistedFullTime = resources.Exists(r => r.Id == ResourceId.FullTimeResourceId);

        if (string.Equals(request.Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase) && isExistedFullTime)
        {
            return scoreValidatorService.Validate(request, currentResources);
        }

        return new Result();
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

    public async Task UpdateMatchInfo(VirtualBetbyRequest request, MatchInfo matchInfo)
    {
        var criteriaResultDetail = new ResultDetail3rdUpdateCriteria
        {
            MatchId = request.MatchId,
            SportType = request.SportType,
            AdminId = request.AdminId,
            IsAuto = true,
            SelectedResources = request.SelectedResources
        };
        await repository.ExecuteAsync(criteriaResultDetail);

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

    public async Task WriteLogSettled(VirtualBetbyRequest request, Result result)
    {
        var criteria = CreateWriteLogCriteria(request, result);

        await repository.ExecuteAsync(criteria);
    }

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualBetbyRequest request, Result result)
    {
        var resources = request.Resources;
        var message = $"VS6 - Calculate({request.Action}) {(result.ErrorCode > 0 ? result.ErrorMessage : string.Empty)}";
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