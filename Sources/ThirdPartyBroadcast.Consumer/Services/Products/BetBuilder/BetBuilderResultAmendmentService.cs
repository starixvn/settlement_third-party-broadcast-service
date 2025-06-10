using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.MessageBridge.ResultAmendment;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;
using ThirdPartyBroadcast.Consumer.Models.BetBuilder;

using ResourceItem = Fanex.Settlement.MessageBridge.ResultAmendment.ResourceItem;


namespace ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

public interface IBetBuilderResultAmendmentService
{
    Task<bool> PushResultAmendment(BetBuilderRequest request, List<ResourceItem> resourceItems, BetBuilderCriteria criteria);
    Task PushUpdateStatusResult(int matchId, string broadcastSessionId, bool isOrg);
}

public class BetBuilderResultAmendmentService : IBetBuilderResultAmendmentService
{
    private readonly MatchService matchService;
    private readonly XmlConvertService xmlConvertService;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly IDynamicRepository repository;

    public BetBuilderResultAmendmentService(
        MatchService matchService,
        XmlConvertService xmlConvertService,
        IPublishEndpoint publishEndpoint,
        IDynamicRepository repository)
    {
        this.matchService = matchService;
        this.xmlConvertService = xmlConvertService;
        this.publishEndpoint = publishEndpoint;
        this.repository = repository;
    }

    public async Task<bool> PushResultAmendment(
        BetBuilderRequest request,
        List<ResourceItem> resourceItems,
        BetBuilderCriteria criteria)
    {
        var resourceRoot = new Models.FM.Resources
        {
            ResourceItems = resourceItems
        };

        var resourcesXMl = xmlConvertService.Serialize(resourceRoot);

        var matchInfo = await matchService.GetMatchInfo(request.MatchId);

        var resultsChanged = await matchService.GetResultChanged(request.MatchId, request.Action, resourcesXMl);

        if (!resultsChanged.Any())
        {
            return false;
        }

        var (newSelectedResources, newResultChanged) = await GetResultResource(request.MatchId, resourceItems, [.. resultsChanged]);

        await publishEndpoint.Publish(new ResultAmended(
            request.MatchId,
            criteria.BroadcastSessionId,
            matchInfo.SportType,
            request.Action,
            newSelectedResources,
            newResultChanged,
            "BB"), new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

        return true;
    }

    public async Task PushUpdateStatusResult(int matchId, string broadcastSessionId, bool isOrg)
    {
        await publishEndpoint.Publish(
            new ResultAmendSettled(matchId, broadcastSessionId, isOrg),
            new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
    }

    private async Task<List<SelectionMarket>> GetMarkets(int matchId, string selectionTypes)
    {
        var ciriteria = new GetSelectionMarketsCriteria
        {
            MatchId = matchId,
            SelectionTypes = selectionTypes
        };
        var markets = await repository.FetchAsync<SelectionMarket>(ciriteria);

        return [.. markets];
    }

    private async Task<(List<ResourceItem>, List<ResultChanged>)> GetResultResource(
        int matchId,
        List<ResourceItem> resourceItems,
        List<ResultChanged> resultsChanged)
    {
        var selectionTypes = string.Join(',', resourceItems
            .Select(x => x.ResourceId)
            .Concat(resultsChanged.Select(r => r.ResourceId))
            .Distinct());

        var selectionMarkets = await GetMarkets(matchId, selectionTypes);

        var newSelectedResources = resourceItems.Join(
            selectionMarkets,
            resource => resource.ResourceId,
            market => market.SelectionType,
            (resource, market) =>
            {
                resource.OtherInfo = market.MarketId;
                return resource;
            }).ToList();

        var newResultChanged = resultsChanged.Join(
            selectionMarkets,
            changed => changed.ResourceId,
            market => market.SelectionType,
            (changed, market) =>
            {
                changed.OtherInfo = market.MarketId;
                return changed;
            }).ToList();

        return (newSelectedResources, newResultChanged);
    }
}
